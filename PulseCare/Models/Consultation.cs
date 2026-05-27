using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore; 

namespace PulseCare.Models
{
    public class Consultation
    {
        [Key]
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        public string DoctorNotes { get; set; }

        public string PrescriptionDetails { get; set; }

        [Precision(18, 2)] // <-- ADD THIS LINE
        public decimal ConsultationFee { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}