using System.ComponentModel.DataAnnotations.Schema;

namespace Clinic_System.Models
{
    public class Department
    {
        public int Id { get; set; }
        public int ClinicId { get; set; }
        [ForeignKey(nameof(ClinicId))]
        public Clinic Clinic { get; set; }

        public string Name { get; set; }
    }
}
