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

        // GET: / - The absolute root entrance node of the PulseCare system application platform
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");

            // If session security data exists, route directly to workspace nodes automatically
            if (role == "Doctor") return RedirectToAction("DoctorDashboard");
            if (role == "Patient") return RedirectToAction("PatientDashboard");
            if (role == "Admin") return RedirectToAction("Dashboard", "Admin");

            // 🔑 FIX: Instead of RedirectToAction("Login"), serve the new public identity landing panel view
            return View();
        }

        public IActionResult PatientDashboard()
        {
            if (HttpContext.Session.GetString("UserRole") != "Patient") return RedirectToAction("Login", "Account");

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

            string userIdStr = HttpContext.Session.GetString("UserId") ?? "0";
            int doctorId = int.Parse(userIdStr);
            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            ViewBag.QueueCount = _context.Appointments.Count(a => a.DoctorId == doctorId && a.Status == "Pending");

            ViewBag.CriticalAlerts = _context.Appointments.Count(a => a.DoctorId == doctorId && a.Status == "Pending" &&
                (a.Symptoms.Contains("fever") || a.Symptoms.Contains("severe") || a.Symptoms.Contains("chest") || a.Symptoms.Contains("pain")));

            ViewBag.ChartsCompiled = _context.Appointments.Count(a => a.DoctorId == doctorId && a.Status == "Completed");

            var rawShiftLogs = (from a in _context.Appointments
                                join u in _context.Users on a.PatientId equals u.Id
                                where a.DoctorId == doctorId
                                orderby a.AppointmentDate ascending
                                select new
                                {
                                    LogTime = a.AppointmentDate.ToString("hh:mm tt"),
                                    PatientLabel = u.FullName,
                                    RawSymptoms = a.Symptoms,
                                    StatusText = a.Status == "Completed" ? "Archived" : "Next Case"
                                }).Take(3).ToList();

            var realTimeShiftLogs = rawShiftLogs.Select(rs => {
                string cleanSymptoms = rs.RawSymptoms;
                if (cleanSymptoms.Contains("]"))
                {
                    cleanSymptoms = cleanSymptoms.Substring(cleanSymptoms.IndexOf("]") + 1).Trim();
                }
                return new
                {
                    rs.LogTime,
                    PatientLabel = $"{rs.PatientLabel} ({cleanSymptoms})",
                    rs.StatusText
                };
            }).ToList();

            ViewBag.ShiftLogs = realTimeShiftLogs;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}