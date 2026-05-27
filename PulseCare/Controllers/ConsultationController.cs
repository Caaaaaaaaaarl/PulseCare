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

            var rawQueue = (from a in _context.Appointments
                            join u in _context.Users on a.PatientId equals u.Id
                            where a.DoctorId == doctorId && a.Status == "Pending"
                            orderby a.AppointmentDate ascending
                            select new
                            {
                                a.Id,
                                PatientName = u.FullName,
                                a.AppointmentDate,
                                RawSymptoms = a.Symptoms,
                                a.Status
                            }).ToList();

            var assignedQueue = rawQueue.Select(rq => {
                string cleanSymptoms = rq.RawSymptoms;
                if (cleanSymptoms.Contains("]"))
                {
                    cleanSymptoms = cleanSymptoms.Substring(cleanSymptoms.IndexOf("]") + 1).Trim();
                }
                return new
                {
                    rq.Id,
                    rq.PatientName,
                    rq.AppointmentDate,
                    Symptoms = cleanSymptoms,
                    rq.Status
                };
            }).ToList();

            ViewBag.AssignedQueue = assignedQueue;
            return View();
        }

        // GET: Consultation/CurrentPatients - Directory of active assigned patients
        [HttpGet]
        public IActionResult CurrentPatients()
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");

            string userIdStr = HttpContext.Session.GetString("UserId") ?? "0";
            int doctorId = int.Parse(userIdStr);
            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            var rawPatients = (from a in _context.Appointments
                               join u in _context.Users on a.PatientId equals u.Id
                               where a.DoctorId == doctorId && a.Status == "Pending"
                               select new
                               {
                                   PatientId = u.Id,
                                   PatientName = u.FullName,
                                   PatientEmail = u.Email,
                                   NextConsultationDate = a.AppointmentDate,
                                   RawSymptoms = a.Symptoms
                               }).ToList();

            var activePatients = rawPatients.Select(rp => {
                string cleanSymptoms = rp.RawSymptoms;
                if (cleanSymptoms.Contains("]"))
                {
                    cleanSymptoms = cleanSymptoms.Substring(cleanSymptoms.IndexOf("]") + 1).Trim();
                }
                return new
                {
                    rp.PatientId,
                    rp.PatientName,
                    rp.PatientEmail,
                    rp.NextConsultationDate,
                    CaseIssue = cleanSymptoms
                };
            })
            .GroupBy(p => p.PatientId)
            .Select(g => g.OrderBy(p => p.NextConsultationDate).First())
            .ToList();

            ViewBag.ActivePatients = activePatients;
            return View();
        }

        // GET: Consultation/PatientHistory - Compiles closed consultation charts
        [HttpGet]
        public IActionResult PatientHistory()
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");

            string userIdStr = HttpContext.Session.GetString("UserId") ?? "0";
            int doctorId = int.Parse(userIdStr);
            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            var rawHistory = (from a in _context.Appointments
                              join u in _context.Users on a.PatientId equals u.Id
                              where a.DoctorId == doctorId && a.Status == "Completed"
                              orderby a.AppointmentDate descending
                              select new
                              {
                                  a.Id, // Included to bind background AJAX tracking requests
                                  PatientName = u.FullName,
                                  ConcludedDate = a.AppointmentDate,
                                  RawSymptoms = a.Symptoms,
                                  StatusValue = a.Status
                              }).ToList();

            var pastConsultations = rawHistory.Select(rh => {
                string cleanSymptoms = rh.RawSymptoms;
                if (cleanSymptoms.Contains("]"))
                {
                    cleanSymptoms = cleanSymptoms.Substring(cleanSymptoms.IndexOf("]") + 1).Trim();
                }
                return new
                {
                    rh.Id,
                    rh.PatientName,
                    rh.ConcludedDate,
                    SymptomsReported = cleanSymptoms,
                    rh.StatusValue
                };
            }).ToList();

            ViewBag.PastConsultations = pastConsultations;
            return View();
        }

        // 🔑 NEW: GET: Consultation/GetConsultationDetails/{id} - Secure background context node validation endpoint
        [HttpGet]
        public IActionResult GetConsultationDetails(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")))
                return Json(new { success = false, message = "Session context unauthorized." });

            var appointment = _context.Appointments.Find(id);
            if (appointment == null)
                return Json(new { success = false, message = "Target chart entry record not found." });

            // Pull matching historical care plans logged in the tracking grid safely
            var reminder = _context.Reminders
                                   .Where(r => r.PatientId == appointment.PatientId && r.ReminderDate.Date == appointment.AppointmentDate.Date)
                                   .FirstOrDefault();

            string clinicalNotes = "No clinical summary notes filed for this session.";
            string prescriptions = "No active medical tracks assigned.";

            if (reminder != null && !string.IsNullOrEmpty(reminder.Message))
            {
                string rawMsg = reminder.Message;
                if (rawMsg.Contains("Notes:"))
                {
                    int notesIndex = rawMsg.IndexOf("Notes:");
                    prescriptions = rawMsg.Substring(0, notesIndex).Replace("Prescription Plan Logged:", "").Trim();
                    clinicalNotes = rawMsg.Substring(notesIndex + 6).Trim();
                }
                else
                {
                    prescriptions = rawMsg;
                }
            }

            // Clean symptom strings of triage references before pushing payload to frontend
            string diagnosticSymptoms = appointment.Symptoms ?? "";
            if (diagnosticSymptoms.Contains("]"))
            {
                diagnosticSymptoms = diagnosticSymptoms.Substring(diagnosticSymptoms.IndexOf("]") + 1).Trim();
            }

            return Json(new
            {
                success = true,
                symptoms = diagnosticSymptoms,
                date = appointment.AppointmentDate.ToString("MMMM dd, yyyy hh:mm tt"),
                notes = clinicalNotes,
                rx = prescriptions
            });
        }

        // GET: Consultation/Checkout/{id} - Serves checkout board screen view
        [HttpGet]
        public IActionResult Checkout(int id)
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");

            var appointmentRecord = _context.Appointments.Find(id);
            if (appointmentRecord == null) return NotFound();

            string userIdStr = HttpContext.Session.GetString("UserId") ?? "0";
            int currentDoctorId = int.Parse(userIdStr);
            if (appointmentRecord.DoctorId != currentDoctorId) return Unauthorized();

            if (appointmentRecord.Symptoms != null && appointmentRecord.Symptoms.Contains("]"))
            {
                appointmentRecord.Symptoms = appointmentRecord.Symptoms.Substring(appointmentRecord.Symptoms.IndexOf("]") + 1).Trim();
            }

            return View(appointmentRecord);
        }

        // POST: Consultation/CompleteConsultation - Processes transactional medical metrics
        [HttpPost]
        public IActionResult CompleteConsultation(int consultationId, int patientId, string clinicalNotes, string prescriptions, decimal fee, DateTime? nextAppointmentDate)
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");

            var currentAppointment = _context.Appointments.Find(consultationId);
            if (currentAppointment == null) return NotFound();

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