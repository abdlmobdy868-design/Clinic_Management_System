using System.ComponentModel.DataAnnotations.Schema;
using ClinicManagementSystem.Utilities;
using ClinicManagementSystem.Data;
namespace ClinicManagementSystem.Models
{
    public class Patient
    {
        public int Id { get; set; }

        public string ApplicationUserId { get; set; }
        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUser ApplicationUser { get; set; }

        public string Name { get; set; }

        public string Phone { get; set; }
        public int Age { get; set; }
        public Gender Gender { get; set; } // male = 0 , female = 1

        public string? BloodType { get; set; }

        public string? Address { get; set; }

        

    }
}
