using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VehicleLoanSystem.Models;
using Scrypt;
using Microsoft.AspNetCore.Http;
using VehicleLoanSystem.Helpers;
using VehicleLoanSystem.Data;

namespace VehicleLoanSystem.Controllers
{
    public class SignUpController : Controller
    {
        private readonly VehicleLoanSystemContext _context;

        public SignUpController(VehicleLoanSystemContext context)
        {
            _context = context;
        }

        // GET: SignUp/Index
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("userId") != null && HttpContext.Session.GetInt32("isAdmin") == 1)
            {
                return RedirectToAction(nameof(Index), "Administrator");
            }
            if (HttpContext.Session.GetInt32("userId") != null && HttpContext.Session.GetInt32("isAdmin") == 0)
            {
                return RedirectToAction(nameof(Index), "Customer");
            }
            return View();
        }

        // POST: SignUp/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,User_Name,User_Password")] UserAccount userAccount)
        {
            ScryptEncoder encoder = new ScryptEncoder();
            if (ModelState.IsValid)
            {
                // Check if it exists
                if (UserAccountExists(userAccount.User_Name))
                {
                    TempData["AlertMessage"] = "User Already Exists";
                    TempData["AlertType"] = "danger";
                    return RedirectToAction(nameof(Index));
                }

                // Hash Password Before Storing
                userAccount.User_Password = encoder.Encode(userAccount.User_Password);
                _context.Add(userAccount);
                await _context.SaveChangesAsync();

                // Retrieve the user after saving changes
                UserAccount user = await _context.Accounts.FirstOrDefaultAsync(e => e.User_Name == userAccount.User_Name);

                // Handle case when User is null
                if (user == null)
                {
                    // Log or handle the situation where user isn't found
                    return RedirectToAction(nameof(Index));
                }

                HttpContext.Session.SetInt32("userId", user.Id);
                HttpContext.Session.SetInt32("isAdmin", 0);
                HttpContext.Session.SetString("username", user.User_Name);

                // Save the user in the session
                SessionHelper.SetObjectAsJson(HttpContext.Session, "user", user);
                return RedirectToAction(nameof(Index), "Customer");
            }
            return View(userAccount);
        }


        private bool UserAccountExists(string User_Name)
        {
            return _context.Accounts.Any(e => e.User_Name == User_Name);
        }
    }
}


