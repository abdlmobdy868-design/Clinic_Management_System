using System.Security.Claims;
using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Areas.Customer
{
    // One place to find "the Patient row of the logged-in user".
    // Never take a PatientId from the URL or a form: always derive it from the login.
    public static class PatientHelper
    {
        public static Task<Patient?> FindAsync(ApplicationDbContext db, ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Task.FromResult<Patient?>(null);
            return db.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
        }

        // Old accounts created before Register started creating a Patient row get one here.
        public static async Task<Patient?> GetOrCreateAsync(
            ApplicationDbContext db, UserManager<ApplicationUser> userManager, ClaimsPrincipal principal)
        {
            var patient = await FindAsync(db, principal);
            if (patient is not null) return patient;

            var appUser = await userManager.GetUserAsync(principal);
            if (appUser is null) return null;

            patient = new Patient
            {
                ApplicationUserId = appUser.Id,
                Name = string.IsNullOrWhiteSpace(appUser.FullName) ? appUser.UserName : appUser.FullName,
                Phone = appUser.PhoneNumber ?? string.Empty,
                Address = appUser.Address,
                Age = 0,
                Gender = Gender.male
            };
            db.Patients.Add(patient);
            await db.SaveChangesAsync();
            return patient;
        }
    }
}
