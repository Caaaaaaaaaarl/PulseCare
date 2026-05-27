using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PulseCare.Data;
using PulseCare.Models;
using System;
using System.Linq;

namespace PulseCare.Controllers
{
    public class ReminderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReminderController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsPatient() => HttpContext.Session.GetString("UserRole") == "Patient";

        // GET: Reminder/Index - Streams active treatment tasks onto the user view panel
        [HttpGet]
        public IActionResult Index()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            string userIdStr = HttpContext.Session.GetString("UserId") ?? "0";
            int patientId = int.Parse(userIdStr);

            // Pull active, incomplete prescription tasks ordered by date sequence
            var reminders = _context.Reminders
                                    .Where(r => r.PatientId == patientId && !r.IsCompleted)
                                    .OrderBy(r => r.ReminderDate)
                                    .ToList();

            return View(reminders);
        }

        // POST: Reminder/MarkComplete/{id} - Implements a background endpoint to process completed care items
        [HttpPost]
        public IActionResult MarkComplete(int id)
        {
            // Return JSON validation blocks to satisfy background asynchronous requests cleanly
            if (!IsPatient())
                return Json(new { success = false, message = "Session context unauthorized." });

            var reminderRecord = _context.Reminders.Find(id);
            if (reminderRecord == null)
                return Json(new { success = false, message = "Target tracking row resource not found." });

            // Update state parameter and commit to database storage
            reminderRecord.IsCompleted = true;
            _context.SaveChanges();

            // Return a clear, lightweight completion payload signature back to the browser script
            return Json(new { success = true });
        }
    }
}