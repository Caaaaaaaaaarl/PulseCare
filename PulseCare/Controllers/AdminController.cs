using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PulseCare.Data;
using PulseCare.Models;
using System;
using System.Linq;

namespace PulseCare.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";

        [HttpGet]
        public IActionResult Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            // 📊 1. Real-time Queue Cadence: Count of all currently unresolved/pending patient slots
            ViewBag.ActivePatientsCount = _context.Appointments.Count(a => a.Status == "Pending");

            // 💰 2. Gross Settled Earnings: Calculate gross earnings based on completed appointments
            decimal revenue = _context.Appointments.Count(a => a.Status == "Completed") * 500m;
            ViewBag.GrossRevenue = revenue >= 1000m ? $"₱{(revenue / 1000m):F1}K" : $"₱{revenue:F0}";

            // 🧠 3. Smart Triage Load: Percentage of appointments processed out of global records
            int totalAppointments = _context.Appointments.Count();
            int completedAppointments = _context.Appointments.Count(a => a.Status == "Completed");
            ViewBag.AnalysisRate = totalAppointments > 0
                ? Math.Round((double)completedAppointments / totalAppointments * 100, 1)
                : 100.0;

            // 🌐 4. Active Portal Bandwidth: Total registered system connection identities across roles
            ViewBag.LiveNodesCount = _context.Users.Count();

            // 📋 5. Dynamic Audit Logs: Pull the 3 most recently added appointment logs to generate the log feed
            var realTimeAuditLogs = (from a in _context.Appointments
                                     join p in _context.Users on a.PatientId equals p.Id
                                     orderby a.Id descending
                                     select new
                                     {
                                         LogTime = a.AppointmentDate.ToString("hh:mm tt"),
                                         Description = $"Patient entry node recorded: {p.FullName} registered an active case profile structure.",
                                         Tag = a.Status == "Completed" ? "Complete" : "System Auth"
                                     }).Take(3).ToList();

            ViewBag.SystemLogs = realTimeAuditLogs;

            return View("AdminDashboard");
        }

        [HttpGet]
        public IActionResult Doctors()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var doctors = _context.Users.Where(u => u.Role == "Doctor").ToList();
            return View(doctors);
        }

        [HttpGet]
        public IActionResult Patients()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var patients = _context.Users.Where(u => u.Role == "Patient").ToList();
            return View(patients);
        }

        [HttpPost]
        public IActionResult CreateAccount(string fullName, string email, string password, string role)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var newUser = new User
            {
                FullName = fullName,
                Email = email,
                Password = password,
                Role = role
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            return RedirectToAction(role == "Doctor" ? "Doctors" : "Patients");
        }

        [HttpPost]
        public IActionResult EditAccount(int id, string fullName, string email, string password)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            if (user != null)
            {
                user.FullName = fullName;
                user.Email = email;
                if (!string.IsNullOrEmpty(password))
                {
                    user.Password = password;
                }
                _context.SaveChanges();
            }

            return RedirectToAction(user?.Role == "Doctor" ? "Doctors" : "Patients");
        }

        [HttpPost]
        public IActionResult RemoveAccount(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            if (user != null)
            {
                string destination = user.Role == "Doctor" ? "Doctors" : "Patients";
                _context.Users.Remove(user);
                _context.SaveChanges();
                return RedirectToAction(destination);
            }

            return RedirectToAction("Dashboard");
        }
    }
}