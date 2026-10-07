using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClinicManagementSystem.Models;
namespace ClinicManagementSystem.ViewModel
{
 

        public class LoginVM
        {
            [Required, EmailAddress]
            public string Email { get; set; }

            [Required, DataType(DataType.Password)]
            public string Password { get; set; }

            public bool RememberMe { get; set; }
        }
    
}

