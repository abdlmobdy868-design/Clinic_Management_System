using ClinicManagementSystem.Utilities;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata.Ecma335;

namespace ClinicManagementSystem.Models
{
    public class Appointment
    {
        
        public int Id { get; set; }
        [Required]
        public string PatientId { get; set; }
        [ValidateNever]
        public Patient? Patient { get; set; }
        [Required]
        public int DoctorId { get; set; }
        [ValidateNever]
        public Doctor? Doctor { get; set; }
        [Required]
        public int ClinicId { get; set; }
        [ValidateNever]
        public Clinic? Clinic { get; set; }
        [Required]
        public DateTime AppointmentDateTime { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public bool IsAvailable { get; set; }
        public AppointmentStatus Status { get; set; }

        public decimal price { get; set; }

    }
}
