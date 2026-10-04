using System.ComponentModel.DataAnnotations.Schema;

namespace Clinic_System.Models
{
    public class Prescription
    {
        public int Id { get; set; }

        public int MedicalRecordId { get; set; }
        [ForeignKey(nameof(MedicalRecordId))]
        public MedicalRecord MedicalRecord { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public string Notes { get; set; }
    }
}
