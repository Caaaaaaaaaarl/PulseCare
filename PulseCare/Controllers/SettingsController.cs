using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PulseCare.Data;
using PulseCare.Models;
using System;
using System.Linq;

namespace PulseCare.Controllers
{
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole"));

        // GET: Settings/Index - Renders the personal credentials management dashboard board
        [HttpGet]
        public IActionResult Index()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            string userIdStr = HttpContext.Session.GetString("UserId") ?? "0";
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(userId);
            if (user == null) return RedirectToAction("Login", "Account");

            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

            return View("Settings", user);
        }

        // POST: Settings/UpdateProfile - Background endpoint to alter name and contact email variables cleanly
        [HttpPost]
        public IActionResult UpdateProfile(string fullName, string email)
        {
            if (!IsLoggedIn())
                return Json(new { success = false, message = "Unauthorized session context security parameters." });

            string userIdStr = HttpContext.Session.GetString("UserId") ?? "0";
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = _context.Users.Find(userId);
                if (user != null)
                {
                    user.FullName = fullName;
                    user.Email = email;
                    _context.SaveChanges();

                    // Instantly sync layout configuration session elements
                    HttpContext.Session.SetString("UserName", fullName);
                    return Json(new { success = true, message = "Your personal profile account parameters have been successfully saved.", updatedName = fullName });
                }
            }
            return Json(new { success = false, message = "Failed to locate target user account record resource." });
        }

        // POST: Settings/UpdateSecurity - Background endpoint to cleanly execute encrypted access password rotations
        [HttpPost]
        public IActionResult UpdateSecurity(string currentPassword, string newPassword)
        {
            if (!IsLoggedIn())
                return Json(new { success = false, message = "Unauthorized session context security parameters." });

            string userIdStr = HttpContext.Session.GetString("UserId") ?? "0";
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = _context.Users.Find(userId);
                if (user != null)
                {
                    // Evaluate current security signature match bounds
                    if (user.Password == currentPassword)
                    {
                        user.Password = newPassword;
                        _context.SaveChanges();
                        return Json(new { success = true, message = "Your secure portal access authorization key has been successfully rotated." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Authentication failure: The provided current password signature does not match our records." });
                    }
                }
            }
            return Json(new { success = false, message = "Failed to locate target user security record resource." });
        }
    }
}