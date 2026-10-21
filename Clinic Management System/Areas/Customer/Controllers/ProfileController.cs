using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.ViewModel; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize] 
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            
            var userId = _userManager.GetUserId(User);

            
            var patient = await _context.Patients
                .Include(p => p.ApplicationUser)
                .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

            
            if (patient == null)
            {
                
                patient = new Patient
                {
                    ApplicationUserId = userId,
                    Name = User.Identity?.Name,
                    Phone = "",
                    Address = ""
                };
            }

            return View(patient);
        }

        
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = _userManager.GetUserId(User);
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Patient model)
        {
            var userId = _userManager.GetUserId(User);
            var patientInDb = await _context.Patients
                .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

            
               
                patientInDb.Name = model.Name;
                patientInDb.Phone = model.Phone;
                patientInDb.Age = model.Age;
                patientInDb.Gender = model.Gender;
                patientInDb.BloodType = model.BloodType;
                patientInDb.Address = model.Address;

                _context.Patients.Update(patientInDb);
            

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}