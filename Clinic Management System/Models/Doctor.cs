
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class Doctor
    {
        public int Id { get; set; }
        public string? ApplicationUserId { get; set; }
        [ForeignKey(nameof(ApplicationUserId))]
        [ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; }

        public int? DepartmentId { get; set; }
        [ForeignKey(nameof(DepartmentId))]
        [ValidateNever]
        public Department? Department { get; set; }

        public string? Name { get; set; } = "";

        public string? Specialization { get; set; }="";

        public int ExperienceYears { get; set; }

    }
}
