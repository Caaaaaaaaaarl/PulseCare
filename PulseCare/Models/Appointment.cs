using System;
using System.ComponentModel.DataAnnotations;

namespace PulseCare.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Symptoms { get; set; }
        public string Status { get; set; } = "Pending"; // "Pending" or "Completed"
    }
}