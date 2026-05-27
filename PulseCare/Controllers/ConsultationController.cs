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

        private bool IsDoctor() => HttpContext.Session.GetString("UserRole") == "Doctor";

        // GET: Consultation/Queue - Main active patient intake registry grid layout panel
        [HttpGet]
        public IActionResult Queue()
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");

            string userIdStr = HttpContext.Session.GetString("UserId") ?? "0";
            int doctorId = int.Parse(userIdStr);
            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            var assignedQueue = (from a in _context.Appointments
                                 join u in _context.Users on a.PatientId equals u.Id
                                 where a.DoctorId == doctorId && a.Status == "Pending"
                                 orderby a.AppointmentDate ascending
                                 select new
                                 {
                                     a.Id,
                                     PatientName = u.FullName,
                                     a.AppointmentDate,
                                     a.Symptoms,
                                     a.Status
                                 }).ToList();

            ViewBag.AssignedQueue = assignedQueue;
            return View();
        }

        // 🔑 NEW: GET: Consultation/CurrentPatients - Fetches unique patients with active/upcoming slots assigned here
        [HttpGet]
        public IActionResult CurrentPatients()
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");

            string userIdStr = HttpContext.Session.GetString("UserId") ?? "0";
            int doctorId = int.Parse(userIdStr);
            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            var activePatients = (from a in _context.Appointments
                                  join u in _context.Users on a.PatientId equals u.Id
                                  where a.DoctorId == doctorId && a.Status == "Pending"
                                  select new
                                  {
                                      PatientId = u.Id,
                                      PatientName = u.FullName,
                                      PatientEmail = u.Email,
                                      NextConsultationDate = a.AppointmentDate,
                                      CaseIssue = a.Symptoms
                                  })
                                  .ToList()
                                  .GroupBy(p => p.PatientId)
                                  .Select(g => g.OrderBy(p => p.NextConsultationDate).First())
                                  .ToList();

            ViewBag.ActivePatients = activePatients;
            return View();
        }

        // 🔑 NEW: GET: Consultation/PatientHistory - Compiles a clear list of historically concluded check-ins 
        [HttpGet]
        public IActionResult PatientHistory()
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");

            string userIdStr = HttpContext.Session.GetString("UserId") ?? "0";
            int doctorId = int.Parse(userIdStr);
            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            var pastConsultations = (from a in _context.Appointments
                                     join u in _context.Users on a.PatientId equals u.Id
                                     where a.DoctorId == doctorId && a.Status == "Completed"
                                     orderby a.AppointmentDate descending
                                     select new
                                     {
                                         PatientName = u.FullName,
                                         ConcludedDate = a.AppointmentDate,
                                         SymptomsReported = a.Symptoms,
                                         StatusValue = a.Status
                                     }).ToList();

            ViewBag.PastConsultations = pastConsultations;
            return View();
        }

        // GET: Consultation/Checkout/{id} - Fetches appointment model and serves the checkout board screen view
        [HttpGet]
        public IActionResult Checkout(int id)
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");

            var appointmentRecord = _context.Appointments.Find(id);
            if (appointmentRecord == null)
            {
                return NotFound();
            }

            string userIdStr = HttpContext.Session.GetString("UserId") ?? "0";
            int currentDoctorId = int.Parse(userIdStr);
            if (appointmentRecord.DoctorId != currentDoctorId)
            {
                return Unauthorized();
            }

            return View(appointmentRecord);
        }

        // POST: Consultation/CompleteConsultation - Processes transactional medical metrics and creates updates
        [HttpPost]
        public IActionResult CompleteConsultation(int consultationId, int patientId, string clinicalNotes, string prescriptions, decimal fee, DateTime? nextAppointmentDate)
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");

            var currentAppointment = _context.Appointments.Find(consultationId);
            if (currentAppointment == null)
            {
                return NotFound();
            }

            string userIdStr = HttpContext.Session.GetString("UserId") ?? "0";
            int doctorId = int.Parse(userIdStr);

            currentAppointment.Status = "Completed";

            if (!string.IsNullOrEmpty(prescriptions))
            {
                var careReminderNode = new Reminder
                {
                    PatientId = patientId,
                    Message = $"Prescription Plan Logged: {prescriptions}. Notes: {clinicalNotes}",
                    ReminderDate = DateTime.Now,
                    IsCompleted = false
                };
                _context.Reminders.Add(careReminderNode);
            }

            if (nextAppointmentDate.HasValue)
            {
                var followUpAppointment = new Appointment
                {
                    PatientId = patientId,
                    DoctorId = doctorId,
                    AppointmentDate = nextAppointmentDate.Value,
                    Symptoms = "Follow-up session scheduled directly by attending physician during previous checkout review.",
                    Status = "Pending"
                };

                _context.Appointments.Add(followUpAppointment);
            }

            _context.SaveChanges();

            return RedirectToAction("Queue");
        }
    }
}