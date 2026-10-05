using Microsoft.AspNetCore.Identity;

namespace ClinicManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {

        public string FullName { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? UserType { get; set; }
    }

}
