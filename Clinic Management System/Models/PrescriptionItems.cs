using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class PrescriptionItems
    {
        public int Id { get; set; }
        public int PrescriptionId { get; set; }
        [ForeignKey(nameof(PrescriptionId))]
        public Prescription Prescription { get; set; }

        public string MedicineName { get; set; }

        public int Dosage { get; set; }
        public string Duration { get; set; }
    }
}
