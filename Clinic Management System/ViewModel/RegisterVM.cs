using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModel
{
    

        public class RegisterVM
        {

        [Required(ErrorMessage = "UserName is required")]
        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        public string? Address { get; set; }

        [Required]
        [Display(Name = "Role / User Type")]
        public string? UserType { get; set; } = "Doctor";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password), Compare("Password")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }

    }

}

