using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scrypt;
using System;
using System.Linq;
using System.Threading.Tasks;
using VehicleLoanSystem.Data;
using VehicleLoanSystem.Helpers;
using VehicleLoanSystem.Models;

namespace VehicleLoanSystem.Controllers.administrator
{
    public class AdministratorController : Controller
    {
        private readonly VehicleLoanSystemContext _context;

        public AdministratorController(VehicleLoanSystemContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(nameof(LoginController.Login), "Login");
            }

            TempData["todaysLoanRequest"] = _context.Loans
                .Where(e => e.LoanDate.Day == DateTime.Now.Day && e.LoanDate.Year == DateTime.Now.Year && e.LoanGrant == "PENDING")
                .Count();

            TempData["CoveredLoan"] = _context.Payments
                .Where(e => e.PayedDate.Month == DateTime.Now.Month && e.LoanCovered)
                .Count();

            TempData["registedUsers"] = _context.Accounts.Where(e => !e.IsAdmin).Count();

            TempData["activeLoans"] = _context.Payments
                .Where(e => e.PayedDate.Year == DateTime.Now.Year && !e.LoanCovered)
                .Count();

            return View();
        }

        public async Task<IActionResult> Loan()
        {
            ViewBag.LoanActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(nameof(LoginController.Login), "Login");
            }
            return View(await _context.Loans.ToListAsync());
        }

        public async Task<IActionResult> DetailLoan(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(nameof(LoginController.Login), "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans.FirstOrDefaultAsync(m => m.Id == id);
            if (loan == null)
            {
                return NotFound();
            }

            return View(loan);
        }

        public async Task<IActionResult> AcceptLoan(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans.FirstOrDefaultAsync(m => m.Id == id);
            if (loan == null)
            {
                return NotFound();
            }

            loan.LoanGrant = "ACCEPTED";
            loan.RejectionReason = "NONE";
            _context.Update(loan);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Loan));
        }

        public async Task<IActionResult> RejectLoan(int? id)
        {
            ViewBag.LoanActive = "active";
            var loan = await _context.Loans.FirstOrDefaultAsync(m => m.Id == id);
            return View(loan);
        }

        [HttpPost]
        public async Task<IActionResult> RejectLoan(Loan loan)
        {
            var loanUpdated = await _context.Loans.FirstOrDefaultAsync(m => m.Id == loan.Id);
            if (loanUpdated == null)
            {
                return NotFound();
            }

            loanUpdated.LoanGrant = "REJECTED";
            loanUpdated.RejectionReason = loan.RejectionReason;
            _context.Update(loanUpdated);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Loan));
        }

        public IActionResult Payment()
        {
            ViewBag.AdminPaymentActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(nameof(LoginController.Login), "Login");
            }
            return View();
        }

        public async Task<IActionResult> PayeeCustomers()
        {
            ViewBag.AdminPaymentActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(nameof(LoginController.Login), "Login");
            }
            return View(await _context.Accounts.ToListAsync());
        }

        public async Task<IActionResult> MakePayment(int id)
        {
            ViewBag.AdminPaymentActive = "active";

            var userLoan = _context.Loans.FirstOrDefault(e => e.UserId == id && e.LoanGrant == "ACCEPTED");
            if (userLoan == null)
            {
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "No loan has been requested by this customer yet";
                return RedirectToAction(nameof(PayeeCustomers));
            }

            var payment = new Payment
            {
                PayedDate = DateTime.Now,
                PayedMonth = DateTime.Now,
                PayedAmount = 0,
                RemainingLoanAmount = userLoan.TotalPayableAmount,
                RemainingMonthPayment = userLoan.MonthlyPayableAmount,
                PenaltyPaymentAmount = userLoan.MonthlyPenalty,
                NextPaymentDate = DateTime.Today.AddMonths(1),
                LoanStatus = "ACTIVE",
                LoanCovered = false,
                UserId = id
            };

            var lastPayment = _context.Payments.OrderBy(e => e.Id).LastOrDefault(e => e.UserId == id);
            if (lastPayment != null)
            {
                payment.RemainingLoanAmount = lastPayment.RemainingLoanAmount;
                payment.RemainingMonthPayment = lastPayment.RemainingMonthPayment;
                payment.PenaltyPaymentAmount = lastPayment.PenaltyPaymentAmount;
                payment.NextPaymentDate = lastPayment.NextPaymentDate;
                payment.LoanStatus = lastPayment.LoanStatus;
                payment.LoanCovered = lastPayment.LoanCovered;
            }

            return View(payment);
        }

        [HttpPost]
        public async Task<IActionResult> MakeFinalPayment(int id, Payment payment)
        {
            payment.RemainingLoanAmount -= payment.PayedAmount;
            payment.RemainingMonthPayment -= payment.PayedAmount;

            if (DateTime.Now > payment.NextPaymentDate)
            {
                var userLoan = await _context.Loans.FirstOrDefaultAsync(e => e.UserId == payment.UserId && e.LoanGrant == "ACCEPTED");
                if (userLoan != null)
                {
                    var loanPlan = await _context.LoanPlans.FirstOrDefaultAsync(e => e.Id == userLoan.LoanPlanId);
                    if (loanPlan != null)
                    {
                        double penaltyValue = loanPlan.MonthlyOverDuePenalty;
                        payment.PenaltyPaymentAmount = Math.Round(Math.Ceiling(penaltyValue), 1);
                    }
                    else
                    {
                        payment.PenaltyPaymentAmount = 0;
                    }
                }
                else
                {
                    payment.PenaltyPaymentAmount = 0;
                }
            }
            else
            {
                payment.PenaltyPaymentAmount = 0;
            }

            if (payment.RemainingLoanAmount <= 0)
            {
                payment.LoanStatus = "DEACTIVE";
                payment.LoanCovered = true;
            }
            else
            {
                payment.LoanStatus = "ACTIVE";
                payment.LoanCovered = false;
            }

            _context.Add(payment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(PayeeCustomers));
        }



        public async Task<IActionResult> PaymentDetail(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FirstOrDefaultAsync(m => m.Id == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }
        [HttpGet]
        public async Task<IActionResult> ListPayments()
        {
            ViewBag.AdminPaymentActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            var payment = await _context.Payments.OrderByDescending(e => e.Id).ToListAsync();
            if (payment == null)
            {
                return NotFound();
            }
            return View(payment);
        }

        public IActionResult LoanPlan()
        {
            ViewBag.LoanPlanActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            return View();
        }

        public async Task<IActionResult> ViewLoanPlan()
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            var loanPlans = await _context.LoanPlans.ToListAsync();
            return View(loanPlans);
        }



        public async Task<IActionResult> DetailsLoanPlan(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var loanPlan = await _context.LoanPlans
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loanPlan == null)
            {
                return NotFound();
            }

            return View(loanPlan);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLoanPlan([Bind("Id,Month,Interest,MonthlyOverDuePenalty")] LoanPlan loanPlan)
        {
            if (ModelState.IsValid)
            {
                _context.Add(loanPlan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(loanPlan);
        }


        public async Task<IActionResult> EditLoanPlan(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var loanPlan = await _context.LoanPlans.FindAsync(id);
            if (loanPlan == null)
            {
                return NotFound();
            }
            return View(loanPlan);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLoanPlan(int id, [Bind("Id,Month,Interest,MonthlyOverDuePenalty")] LoanPlan loanPlan)
        {
            if (id != loanPlan.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loanPlan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoanPlanExists(loanPlan.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ViewLoanPlan));
            }
            return View(loanPlan);
        }

        // GET: LoanPlan/Delete/5
        public async Task<IActionResult> DeleteLoanPlan(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var loanPlan = await _context.LoanPlans
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loanPlan == null)
            {
                return NotFound();
            }

            return View(loanPlan);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLoanPlanConfirmed(int id)
        {
            var loanPlan = await _context.LoanPlans.FindAsync(id);
            _context.LoanPlans.Remove(loanPlan);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ViewLoanPlan));
        }

        private bool LoanPlanExists(int id)
        {
            return _context.LoanPlans.Any(e => e.Id == id);
        }

        public IActionResult LoanType()
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            return View();
        }



        // GET: LoanType
        public async Task<IActionResult> ViewLoanType()
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            return View(await _context.LoanTypes.ToListAsync());
        }

        // GET: LoanType/Details/5
        public async Task<IActionResult> DetailsLoanType(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var loanType = await _context.LoanTypes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loanType == null)
            {
                return NotFound();
            }

            return View(loanType);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLoanType([Bind("Id,LoanTypeName,LoanDescription")] LoanType loanType)
        {
            if (ModelState.IsValid)
            {
                _context.Add(loanType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ViewLoanType));
            }
            return View(loanType);
        }

        // GET: LoanType/Edit/5
        public async Task<IActionResult> EditLoanType(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var loanType = await _context.LoanTypes.FindAsync(id);
            if (loanType == null)
            {
                return NotFound();
            }
            return View(loanType);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLoanType(int id, [Bind("Id,LoanTypeName,LoanDescription")] LoanType loanType)
        {
            if (id != loanType.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loanType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoanTypeExists(loanType.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ViewLoanType));
            }
            return View(loanType);
        }


        public async Task<IActionResult> DeleteLoanType(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var loanType = await _context.LoanTypes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loanType == null)
            {
                return NotFound();
            }

            return View(loanType);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLoanTypeConfirmed(int id)
        {
            var loanType = await _context.LoanTypes.FindAsync(id);
            _context.LoanTypes.Remove(loanType);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ViewLoanType));
        }

        private bool LoanTypeExists(int id)
        {
            return _context.LoanTypes.Any(e => e.Id == id);
        }


        public async Task<IActionResult> Users()
        {
            ViewBag.UsersActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            return View(await _context.Accounts.ToListAsync());
        }


        // GET: Users/Details/5
        public async Task<IActionResult> DetailsUsers(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var userAccount = await _context.Accounts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userAccount == null)
            {
                return NotFound();
            }

            return View(userAccount);
        }

        // GET: Users/Create
        public IActionResult CreateUsers()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUsers([Bind("Id,User_Name,User_Password,IsAdmin")] UserAccount userAccount)
        {
            if (ModelState.IsValid)
            {
                ScryptEncoder encoder = new ScryptEncoder();
                userAccount.User_Password = encoder.Encode(userAccount.User_Password);
                _context.Add(userAccount);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Users));
            }
            return View(userAccount);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> EditUsers(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var userAccount = await _context.Accounts.FindAsync(id);
            if (userAccount == null)
            {
                return NotFound();
            }
            return View(userAccount);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUsers(int id, UserAccount userAccount)
        {
            if (id != userAccount.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    ScryptEncoder encoder = new ScryptEncoder();
                    userAccount.User_Password = encoder.Encode(userAccount.User_Password);
                    _context.Update(userAccount);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserAccountExists(userAccount.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Users));
            }
            return View(userAccount);
        }


        public async Task<IActionResult> DeleteUsers(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var userAccount = await _context.Accounts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userAccount == null)
            {
                return NotFound();
            }

            return View(userAccount);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUsersConfirmed(int id)
        {
            var userAccount = await _context.Accounts.FindAsync(id);
            _context.Accounts.Remove(userAccount);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Users));
        }

        private bool UserAccountExists(int id)
        {
            return _context.Accounts.Any(e => e.Id == id);
        }



        public async Task<IActionResult> Report()
        {
            ViewBag.AdminReportActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }



            TempData["MonthlyGivenOut"] = _context.Loans.Where(e => e.LoanDate.Month == DateTime.Now.Month && e.LoanDate.Year == DateTime.Now.Year).Select(t => t.LoanAmount).Sum();

            TempData["MonthlyAciveLoan"] = _context.Loans.Where(e => e.LoanDate.Month == DateTime.Now.Month && e.LoanDate.Year == DateTime.Now.Year).Where(e => e.LoanGrant == "ACCEPTED").Count();

            TempData["MonthlyClosedLoan"] = _context.Payments.Where(e => e.PayedDate.Month == DateTime.Now.Month).Where(e => e.LoanCovered == true).Count();

            TempData["MonthlyProfit"] = _context.Loans.Where(e => e.LoanDate.Month == DateTime.Now.Month && e.LoanDate.Year == DateTime.Now.Year).Select(e => e.TotalPayableAmount).Sum() - _context.Loans.Where(e => e.LoanDate.Month == DateTime.Now.Month && e.LoanDate.Year == DateTime.Now.Year).Select(e => e.LoanAmount).Sum();

            TempData["MonthlyActiveUsers"] = _context.Accounts.Where(e => e.IsAdmin == false).Count();

            TempData["MonthlyExpectedPayment"] = _context.Loans.Where(e => e.LoanDate.Month == DateTime.Now.Month && e.LoanDate.Year == DateTime.Now.Year).Where(e => e.LoanGrant == "ACCEPTED").Select(e => e.MonthlyPayableAmount).Sum();

            TempData["MonthlyPayedAmount"] = _context.Payments.Where(e => e.PayedDate.Month == DateTime.Now.Month && e.PayedDate.Year == DateTime.Now.Year).Select(e => e.PayedAmount).Sum();

            TempData["MonthlyPayedPenalty"] = _context.Payments.Where(e => e.PayedDate.Month == DateTime.Now.Month && e.PayedDate.Year == DateTime.Now.Year).Select(e => e.PenaltyPaymentAmount).Sum();

            TempData["YearlyGivenOut"] = _context.Loans.Where(e => e.LoanDate.Year == DateTime.Now.Year).Select(t => t.LoanAmount).Sum();

            TempData["YearlyActiveLoan"] = _context.Loans.Where(e => e.LoanGrant == "ACCEPTED" && e.LoanDate.Year == DateTime.Now.Year).Count();

            TempData["YearlyClosedLoan"] = _context.Payments.Where(e => e.LoanCovered == true && e.PayedDate.Year == DateTime.Now.Year).Count();

            TempData["YearlyProfit"] = _context.Loans.Where(e => e.LoanDate.Year == DateTime.Now.Year).Select(e => e.TotalPayableAmount).Sum() - _context.Loans.Where(e => e.LoanDate.Year == DateTime.Now.Year).Select(e => e.LoanAmount).Sum();

            TempData["YearlyUsers"] = _context.Accounts.Where(e => e.IsAdmin == false).Count();

            TempData["YearlyPayment"] = _context.Payments.Where(e => e.PayedDate.Year == DateTime.Now.Year).Select(e => e.PayedAmount).Sum();


            return View(await _context.Accounts.ToListAsync());
        }

        public RedirectToActionResult Logout()
        {
            HttpContext.Session.SetInt32("userId", -1);
            HttpContext.Session.SetString("userEmail", "");
            return RedirectToAction(nameof(LoginController.Login), "Login");
        }
    }
}





