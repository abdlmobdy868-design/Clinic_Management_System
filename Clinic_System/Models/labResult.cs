using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clinic_System.Models
{
    public class labResult
    {
        public int Id { get; set; }
        public int MedicalRecordId { get; set; }
        [ForeignKey(nameof(MedicalRecordId))]
        public MedicalRecord MedicalRecord { get; set; }

        public int LabTestId { get; set; }
        [ForeignKey(nameof(LabTestId))]
        public LabTest LabTest { get; set; }

        public decimal ResultValue { get; set; }
        public string ResultFileUrl { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
