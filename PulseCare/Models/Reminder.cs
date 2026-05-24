using System;
using System.ComponentModel.DataAnnotations;

namespace PulseCare.Models
{
    public class Reminder
    {
        [Key]
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string Message { get; set; } // e.g., "Take Metformin 500mg" or "Log Blood Pressure"
        public DateTime ReminderDate { get; set; }
        public bool IsCompleted { get; set; } = false;
    }
}