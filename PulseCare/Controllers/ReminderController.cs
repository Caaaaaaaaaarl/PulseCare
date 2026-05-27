using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PulseCare.Data;
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

        [HttpGet]
        public IActionResult Index()
        {
            // Block access if not a Patient
            if (HttpContext.Session.GetString("UserRole") != "Patient")
                return RedirectToAction("Login", "Account");

            int patientId = int.Parse(HttpContext.Session.GetString("UserId"));

            // Fetch all reminders for this patient, sort uncompleted ones first
            var reminders = _context.Reminders
                                    .Where(r => r.PatientId == patientId)
                                    .OrderBy(r => r.IsCompleted)
                                    .ThenBy(r => r.ReminderDate)
                                    .ToList();

            return View(reminders);
        }

        [HttpPost]
        public IActionResult MarkComplete(int id)
        {
            // Find the reminder and mark it as done
            var reminder = _context.Reminders.Find(id);
            if (reminder != null)
            {
                reminder.IsCompleted = true;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}