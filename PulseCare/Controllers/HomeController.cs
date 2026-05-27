using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PulseCare.Data;
using System;
using System.Linq;

namespace PulseCare.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Doctor") return RedirectToAction("DoctorDashboard");
            if (role == "Patient") return RedirectToAction("PatientDashboard");
            if (role == "Admin") return RedirectToAction("Dashboard", "Admin");

            return RedirectToAction("Login", "Account");
        }

        public IActionResult PatientDashboard()
        {
            if (HttpContext.Session.GetString("UserRole") != "Patient") return RedirectToAction("Login", "Account");

            // Safe parsing fallback to resolve CS8604 nullability warnings
            string userIdStr = HttpContext.Session.GetString("UserId") ?? "0";
            int patientId = int.Parse(userIdStr);
            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            ViewBag.TotalAppointments = _context.Appointments.Count(a => a.PatientId == patientId);
            ViewBag.PendingRequests = _context.Appointments.Count(a => a.PatientId == patientId && a.Status == "Pending");
            ViewBag.ActiveRemindersCount = _context.Reminders.Count(r => r.PatientId == patientId && !r.IsCompleted);

            ViewBag.RecentAppointmentsList = (from a in _context.Appointments
                                              join u in _context.Users on a.DoctorId equals u.Id
                                              where a.PatientId == patientId
                                              orderby a.AppointmentDate descending
                                              select new
                                              {
                                                  DoctorName = u.FullName,
                                                  a.AppointmentDate,
                                                  a.Status
                                              }).Take(3).ToList();

            ViewBag.ActiveRemindersList = _context.Reminders
                                                  .Where(r => r.PatientId == patientId && !r.IsCompleted)
                                                  .OrderBy(r => r.ReminderDate)
                                                  .Take(3)
                                                  .ToList();

            return View();
        }

        public IActionResult DoctorDashboard()
        {
            if (HttpContext.Session.GetString("UserRole") != "Doctor") return RedirectToAction("Login", "Account");

            // Safe parsing fallback to resolve CS8604 nullability warnings
            string userIdStr = HttpContext.Session.GetString("UserId") ?? "0";
            int doctorId = int.Parse(userIdStr);
            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            // 📊 1. Patients in Queue: Count of pending appointments assigned to this specific doctor
            ViewBag.QueueCount = _context.Appointments.Count(a => a.DoctorId == doctorId && a.Status == "Pending");

            // ⚠️ 2. Critical Alerts: High-risk screening flags derived from patient case notes
            ViewBag.CriticalAlerts = _context.Appointments.Count(a => a.DoctorId == doctorId && a.Status == "Pending" &&
                (a.Symptoms.Contains("fever") || a.Symptoms.Contains("severe") || a.Symptoms.Contains("chest") || a.Symptoms.Contains("pain")));

            // 📋 3. Charts Compiled Today: Total processed records completed by this doctor context node
            ViewBag.ChartsCompiled = _context.Appointments.Count(a => a.DoctorId == doctorId && a.Status == "Completed");

            // ⏳ 4. Shift Logs Feed: Stream top 3 appointments for this doctor joined with patient full name metadata fields
            // 🔑 FIX: Changed 'asc' to C#'s valid 'ascending' keyword to fully clear the query expression compiler layout state
            var realTimeShiftLogs = (from a in _context.Appointments
                                     join u in _context.Users on a.PatientId equals u.Id
                                     where a.DoctorId == doctorId
                                     orderby a.AppointmentDate ascending
                                     select new
                                     {
                                         LogTime = a.AppointmentDate.ToString("hh:mm tt"),
                                         PatientLabel = u.FullName,
                                         StatusText = a.Status == "Completed" ? "Archived" : "Next Case"
                                     }).Take(3).ToList();

            ViewBag.ShiftLogs = realTimeShiftLogs;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}