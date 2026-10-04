using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class MedicalRecord
    {
        
        public int Id { get; set; }

        public int PatientId { get; set; }

        [ForeignKey(nameof(PatientId))]
        public virtual Patient Patient { get; set; } = null!;
        public int DoctorId { get; set; }

        [ForeignKey(nameof(DoctorId))]
        public virtual Doctor Doctor { get; set; } = null!;

        public int? AppointmentId { get; set; }

        [ForeignKey(nameof(AppointmentId))]
        public virtual Appointment? Appointment { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Diagnosis { get; set; } = string.Empty;

        public string? Symptoms { get; set; }

        public string? MedicalHistory { get; set; }

        public string? VitalSigns { get; set; } // e.g. "BP: 120/80, Temp: 37C, Pulse: 72"

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
