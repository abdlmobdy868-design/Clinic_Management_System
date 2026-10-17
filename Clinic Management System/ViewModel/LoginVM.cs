using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClinicManagementSystem.Models;
namespace ClinicManagementSystem.ViewModel
{
 

        public class LoginVM
        {
            [Required]
            public string UserNameOrEmail { get; set; }

            [Required, DataType(DataType.Password)]
            public string Password { get; set; }

            public bool RememberMe { get; set; }
        }
    
}

