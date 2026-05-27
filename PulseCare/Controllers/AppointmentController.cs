using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PulseCare.Data;
using PulseCare.Models;
using System;
using System.Linq;

namespace PulseCare.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Book()
        {
            // Ensure only patients can access this page
            if (HttpContext.Session.GetString("UserRole") != "Patient")
                return RedirectToAction("Login", "Account");

            // Fetch all doctors to populate the dropdown menu
            ViewBag.Doctors = _context.Users.Where(u => u.Role == "Doctor").ToList();
            return View();
        }
        // Inside Controllers/AppointmentController.cs

        [HttpGet]
        public IActionResult MyAppointments()
        {
            // Ensure only authorized patients can access this data ledger route
            if (HttpContext.Session.GetString("UserRole") != "Patient")
                return RedirectToAction("Login", "Account");

            int patientId = int.Parse(HttpContext.Session.GetString("UserId"));

            // 🔑 Query personal appointments database history and join with doctor names cleanly
            var appointmentsList = (from a in _context.Appointments
                                    join u in _context.Users on a.DoctorId equals u.Id
                                    where a.PatientId == patientId
                                    orderby a.AppointmentDate descending
                                    select new
                                    {
                                        a.Id,
                                        DoctorName = u.FullName,
                                        a.AppointmentDate,
                                        a.Symptoms,
                                        a.Status
                                    }).ToList();

            ViewBag.Appointments = appointmentsList;
            return View();
        }
        [HttpPost]
        public IActionResult Book(int doctorId, DateTime appointmentDate, string symptoms)
        {
            int patientId = int.Parse(HttpContext.Session.GetString("UserId"));

            // --- AI TRIAGE ENGINE ---
            // Scans symptoms for keywords to assign priority automatically
            string triageLevel = "Routine";
            string lowerSymptoms = symptoms.ToLower();

            if (lowerSymptoms.Contains("chest pain") || lowerSymptoms.Contains("short breath") || lowerSymptoms.Contains("severe"))
            {
                triageLevel = "URGENT";
            }
            else if (lowerSymptoms.Contains("fever") || lowerSymptoms.Contains("dizzy") || lowerSymptoms.Contains("pain"))
            {
                triageLevel = "Elevated";
            }

            // Prepend the triage result so the Doctor sees the system's evaluation
            string finalSymptoms = $"[Triage: {triageLevel}] {symptoms}";

            var appointment = new Appointment
            {
                PatientId = patientId,
                DoctorId = doctorId,
                AppointmentDate = appointmentDate,
                Symptoms = finalSymptoms,
                Status = "Pending"
            };

            _context.Appointments.Add(appointment);
            _context.SaveChanges();

            // Redirect back to dashboard after booking
            return RedirectToAction("PatientDashboard", "Home");
        }
    }
}