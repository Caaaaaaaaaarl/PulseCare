using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PulseCare.Data;
using PulseCare.Models;
using System;
using System.Linq;

namespace PulseCare.Controllers
{
    public class ConsultationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConsultationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Queue()
        {
            // Block access if not a Doctor
            if (HttpContext.Session.GetString("UserRole") != "Doctor")
                return RedirectToAction("Login", "Account");

            int doctorId = int.Parse(HttpContext.Session.GetString("UserId"));

            // Fetch pending appointments for this specific doctor and link patient names
            var pendingQueue = (from a in _context.Appointments
                                join u in _context.Users on a.PatientId equals u.Id
                                where a.DoctorId == doctorId && a.Status == "Pending"
                                select new
                                {
                                    a.Id,
                                    PatientName = u.FullName,
                                    a.AppointmentDate,
                                    a.Symptoms
                                }).ToList();

            ViewBag.Queue = pendingQueue;
            return View();
        }

        [HttpGet]
        public IActionResult Checkout(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Doctor")
                return RedirectToAction("Login", "Account");

            var appointment = _context.Appointments.Find(id);
            if (appointment == null) return NotFound();

            var patient = _context.Users.Find(appointment.PatientId);
            ViewBag.PatientName = patient.FullName;

            return View(appointment);
        }

        [HttpPost]
        public IActionResult Checkout(int appointmentId, int patientId, string notes, string prescription, decimal fee)
        {
            // THIS IS THE PROFESSOR's REQUIREMENT: The Transaction System
            // It ensures that if one step fails (e.g. database crash), the whole thing rolls back safely.
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // Step 1: Update the Appointment Status
                var appointment = _context.Appointments.Find(appointmentId);
                appointment.Status = "Completed";

                // Step 2: Save the Permanent Consultation Record (Ledger)
                var consultation = new Consultation
                {
                    AppointmentId = appointmentId,
                    DoctorNotes = notes,
                    PrescriptionDetails = prescription,
                    ConsultationFee = fee,
                    CreatedAt = DateTime.Now
                };
                _context.Consultations.Add(consultation);

                // Step 3: Automatically Generate the Patient Reminders (SDG 3 Requirement)
                // We create an immediate reminder for them to check their new plan
                var reminder = new Reminder
                {
                    PatientId = patientId,
                    Message = $"New Plan: {prescription}. Please start your assigned diet and medication today.",
                    ReminderDate = DateTime.Now.AddDays(1),
                    IsCompleted = false
                };
                _context.Reminders.Add(reminder);

                // Commit everything to the database as one atomic unit
                _context.SaveChanges();
                transaction.Commit();

                return RedirectToAction("Queue");
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Cancel all changes if any error occurs
                ViewBag.Error = "Transaction failed. No records were saved. Error: " + ex.Message;

                var appointment = _context.Appointments.Find(appointmentId);
                ViewBag.PatientName = _context.Users.Find(appointment.PatientId).FullName;
                return View(appointment);
            }
        }
    }
}