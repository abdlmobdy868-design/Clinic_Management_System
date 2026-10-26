using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Areas.Customer.ViewModels
{
    // What the "Leave a review" form is allowed to send (nothing else can be over-posted)
    public class ReviewInputVM
    {
        public int AppointmentId { get; set; }
        public string DoctorName { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; } = 5;

        [Required, StringLength(1000)]
        public string Comment { get; set; } = string.Empty;
    }
}
