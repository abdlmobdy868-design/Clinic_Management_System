using System.ComponentModel.DataAnnotations.Schema;

namespace Clinic_System.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        [ForeignKey(nameof(PatientId))]
        public Patient Patient { get; set; }
        public int DoctorId { get; set; }
        [ForeignKey(nameof(DoctorId))]
        public Doctor Doctor { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
