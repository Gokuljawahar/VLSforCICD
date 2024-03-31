using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scrypt;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VehicleLoanSystem.Data;
using VehicleLoanSystem.Models;

namespace VehicleLoanSystem.Controllers.customer
{
    public class CustomerController : Controller
    {
        private readonly VehicleLoanSystemContext _context;

        public CustomerController(VehicleLoanSystemContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.HomeActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction("Login", "Login");
            }
            int? id = HttpContext.Session.GetInt32("userId");

            var latestLoan = _context.Loans.OrderByDescending(e => e.Id).FirstOrDefault(e => e.UserId == id);
            if (latestLoan != null)
            {
                TempData["loanStatus"] = latestLoan.LoanGrant;
                TempData["monthlyPayment"] = latestLoan.LoanGrant == "ACCEPTED" ? Math.Round(Convert.ToDouble(latestLoan.MonthlyPayableAmount), 2) : 0;
                TempData["nextPayment"] = DateTime.Now.AddMonths(1).ToString("MMMM dddd");
            }
            else
            {
                TempData["monthlyPayment"] = 0;
                TempData["loanStatus"] = null;
                TempData["nextPayment"] = "No Active Loan";
            }

            var latestPayment = _context.Payments.OrderByDescending(e => e.Id).FirstOrDefault(e => e.UserId == id);
            TempData["totalRemainingLoan"] = latestPayment?.RemainingLoanAmount.ToString() ?? null;

            return View();
        }

        public IActionResult Account()
        {
            ViewBag.AccountActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction("Login", "Login");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAccount(UserAccount userAccount)
        {
            ViewBag.AccountActive = "active";
            ScryptEncoder encoder = new ScryptEncoder();
            userAccount.User_Password = encoder.Encode(userAccount.User_Password);

            _context.Update(userAccount);
            await _context.SaveChangesAsync();

            TempData["AlertMessage"] = "Account Updated Successfully.";
            TempData["AlertType"] = "success";

            return RedirectToAction("Account");
        }

        public IActionResult Loan()
        {
            ViewBag.LoanActive = "active";
            var LoanPlan = _context.LoanPlans.ToList();
            var LoanType = _context.LoanTypes.ToList();

            ViewData["plan"] = LoanPlan;
            ViewData["type"] = LoanType;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestLoan(Loan loan, IFormFile identityImageFile, IFormFile incomeImageFile, IFormFile cibilImageFile)
        {
            ViewBag.LoanActive = "active";
            var loanPlans = _context.LoanPlans.ToList();
            var loanTypes = _context.LoanTypes.ToList();

            ViewData["plan"] = loanPlans;
            ViewData["type"] = loanTypes;

            if (ModelState.IsValid)
            {
                var loanPlan = LoanPlanExists(loan.LoanPlanId);
                if (loanPlan == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid Loan plan.");
                    return View("Loan", loan);
                }

                if (identityImageFile != null && identityImageFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await identityImageFile.CopyToAsync(memoryStream);
                        byte[] imageData = memoryStream.ToArray();
                        loan.IdentityImage = imageData; // Assign byte array directly
                    }
                }

                // Handle income image
                if (incomeImageFile != null && incomeImageFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await incomeImageFile.CopyToAsync(memoryStream);
                        byte[] imageData = memoryStream.ToArray();
                        loan.IncomeImage = imageData; // Assign byte array directly
                    }
                }

                if (cibilImageFile != null && cibilImageFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await cibilImageFile.CopyToAsync(memoryStream);
                        byte[] imageData = memoryStream.ToArray();
                        loan.CibilImage = imageData; // Assign byte array directly
                    }
                }

                double interestRate = loanPlan.Interest;
                double monthlyOverduePenaltyRate = loanPlan.MonthlyOverDuePenalty;
                double principal = loan.LoanAmount;
                int totalMonths = loanPlan.Month;

                double monthlyInterestRate = interestRate / 100 / 12;
                double monthlyPayment = principal * (monthlyInterestRate / (1 - Math.Pow(1 + monthlyInterestRate, -totalMonths)));
                double totalPayableAmount = monthlyPayment * totalMonths;
                double monthlyPenalty = principal * (monthlyOverduePenaltyRate / 100);
                monthlyPayment = Math.Round(Math.Ceiling(monthlyPayment), 1);
                totalPayableAmount = Math.Round(Math.Ceiling(totalPayableAmount), 1);
                monthlyPenalty = Math.Round(Math.Ceiling(monthlyPenalty), 1);

                Console.WriteLine("Loan parameters calculated successfully.");
                loan.UserId = (int)HttpContext.Session.GetInt32("userId");

                loan.LoanDate = DateTime.Now;
                loan.TotalPayableAmount = totalPayableAmount;
                loan.MonthlyPayableAmount = monthlyPayment;
                loan.MonthlyPenalty = monthlyPenalty;

                loan.LoanGrant = "PENDING";
                loan.RejectionReason = "NONE";

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        loan.UserId = (int)HttpContext.Session.GetInt32("userId");
                        loan.LoanDate = DateTime.Now;
                        loan.TotalPayableAmount = totalPayableAmount;
                        loan.MonthlyPayableAmount = monthlyPayment;
                        loan.MonthlyPenalty = monthlyPenalty;
                        loan.LoanGrant = "PENDING";
                        loan.RejectionReason = "NONE";

                        _context.Loans.Add(loan);
                        await _context.SaveChangesAsync();

                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateException)
                    {
                        ModelState.AddModelError(string.Empty, "An error occurred while saving to the database.");
                        return View("Loan", loan);
                    }
                }
            }
            else
            {
                // ModelState is invalid, print errors to console
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        Console.WriteLine($"ModelState error: {error.ErrorMessage}");
                    }
                }
            }

            return View("Loan", loan);
        }

        public async Task<IActionResult> ViewLoan()
        {
            ViewBag.LoanActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction("Login", "Login");
            }
            int? id = HttpContext.Session.GetInt32("userId");
            return View(await _context.Loans.Where(e => e.UserId == id).ToListAsync());
        }

        public async Task<IActionResult> DetailLoan(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction("Login", "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var Loan = await _context.Loans.FirstOrDefaultAsync(m => m.Id == id);
            if (Loan == null)
            {
                return NotFound();
            }

            return View(Loan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLoan(int id)
        {
            TempData["AlertType"] = "success";
            TempData["AlertMessage"] = "Loan Request Has Been Deleted Successfully";

            var Loan = await _context.Loans.FindAsync(id);
            _context.Loans.Remove(Loan);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Payment()
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction("Login", "Login");
            }
            int? id = HttpContext.Session.GetInt32("userId");
            return View(await _context.Payments.Where(e => e.UserId == id).ToListAsync());
        }

        public IActionResult Report()
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction("Login", "Login");
            }
            int? id = HttpContext.Session.GetInt32("userId");

            TempData["AcceptedLoan"] = _context.Loans.Count(e => e.LoanDate.Year == DateTime.Now.Year && e.LoanGrant == "ACCEPTED" && e.UserId == id);
            TempData["RejectedLoan"] = _context.Loans.Count(e => e.LoanDate.Year == DateTime.Now.Year && e.LoanGrant == "REJECTED" && e.UserId == id);
            TempData["CoveredLoan"] = _context.Payments.Count(e => e.LoanCovered && e.UserId == id);

            TempData["TotalMoneyPaid"] = _context.Payments.Where(e => e.UserId == id).Select(e => e.PayedAmount).Sum();
            TempData["TotalPenaltyMoney"] = _context.Payments.Where(e => e.UserId == id).Select(e => e.PenaltyPaymentAmount).Sum();

            return View();
        }

        public IActionResult Logout()
        {
            return RedirectToAction("Logout", "Login");
        }

        public LoanPlan LoanPlanExists(int id)
        {
            return _context.LoanPlans.FirstOrDefault(e => e.Id == id);
        }
    }
}
