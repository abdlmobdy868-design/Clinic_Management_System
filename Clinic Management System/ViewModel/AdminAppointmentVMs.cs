using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ClinicManagementSystem.Utilities;

namespace ClinicManagementSystem.ViewModel
{
    // ---------- Admin: appointments list ----------
    public class AppointmentRowVM
    {
        public int Id { get; set; }
        public int? DoctorId { get; set; }
        public string DoctorName { get; set; } = "-";
        public string? PatientName { get; set; }
        public int? PatientId { get; set; }
        public string? ClinicName { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public decimal Price { get; set; }
        public AppointmentStatus Status { get; set; }
        public bool IsFree { get; set; }
        public bool IsPaid { get; set; }
        public bool HasRecord { get; set; }
    }

    public class AppointmentsIndexVM
    {
        public string? View { get; set; }            // all | free | booked
        public AppointmentStatus? Status { get; set; }
        public int? DoctorId { get; set; }
        public DateTime? Date { get; set; }

        public int FreeCount { get; set; }
        public int PendingCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }

        public List<SelectListItem> Doctors { get; set; } = new();
        public List<AppointmentRowVM> Rows { get; set; } = new();
    }

    // ---------- Admin: create many free slots at once ----------
    public class SlotGeneratorVM
    {
        [Required(ErrorMessage = "Please select a doctor")]
        public int? DoctorId { get; set; }

        public int? ClinicId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(1);

        [Range(1, 14, ErrorMessage = "Days must be between 1 and 14")]
        public int Days { get; set; } = 5;

        [Required]
        public string FromTime { get; set; } = "10:00";

        [Required]
        public string ToTime { get; set; } = "14:00";

        [Range(10, 240, ErrorMessage = "Duration must be between 10 and 240 minutes")]
        public int DurationMinutes { get; set; } = 45;

        [Range(0, 100000, ErrorMessage = "Price must be 0 or more")]
        public decimal Price { get; set; } = 250;

        public List<SelectListItem> Doctors { get; set; } = new();
        public List<SelectListItem> Clinics { get; set; } = new();
    }

    // ---------- Admin: edit one appointment (only these fields can be changed) ----------
    public class AppointmentEditVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a doctor")]
        public int? DoctorId { get; set; }

        [Required(ErrorMessage = "Please select a clinic")]
        public int? ClinicId { get; set; }

        public int? PatientId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Range(0, 100000)]
        public decimal Price { get; set; }

        public AppointmentStatus Status { get; set; }

        public List<SelectListItem> Doctors { get; set; } = new();
        public List<SelectListItem> Clinics { get; set; } = new();
        public List<SelectListItem> Patients { get; set; } = new();
    }
}
