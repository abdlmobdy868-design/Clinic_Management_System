using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(string fullName, string email, string password, string role)
        {
            var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, EmailConfirmed = true, UserType = role };
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                return RedirectToAction(nameof(Index));
            }
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View();
        }

       
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            return View(user);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(string id, string fullName, string email)
        {
            var user = await _userManager.FindByIdAsync(id);
            user.FullName = fullName;
            user.Email = email;
            user.UserName = email;
          
            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }

        
        [HttpGet]
        public async Task<IActionResult> Roles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            var allRoles = _roleManager.Roles.ToList();
            ViewBag.UserId = id;
            ViewBag.UserRoles = await _userManager.GetRolesAsync(user);
            return View(allRoles);
        }
        [HttpPost]
        public async Task<IActionResult> Roles(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            var oldRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, oldRoles);
            await _userManager.AddToRoleAsync(user, role);
            return RedirectToAction(nameof(Index));
        }

        
        public async Task<IActionResult> LockUnlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
                user.LockoutEnd = DateTime.Now;
            else
                user.LockoutEnd = DateTime.Now.AddYears(100); 
            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return RedirectToAction(nameof(Index));

            // متسمحش تمسح نفسك
            if (id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "Can't delete yourself!";
                return RedirectToAction(nameof(Index));
            }

            // 1- اهم سطر هيحل الايرور اللي في الصورة: امسح الرولات الاول
            var userRoles = await _context.UserRoles.Where(r => r.UserId == id).ToListAsync();
            _context.UserRoles.RemoveRange(userRoles);

            // 2- امسح اي حاجة مربوطة باليوزر ده
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == id);
            if (patient != null)
            {
                _context.Appointments.RemoveRange(_context.Appointments.Where(a => a.PatientId == patient.Id));
                _context.MedicalRecords.RemoveRange(_context.MedicalRecords.Where(r => r.PatientId == patient.Id));
                _context.Patients.Remove(patient);
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == id);
            if (doctor != null)
            {
                _context.Appointments.RemoveRange(_context.Appointments.Where(a => a.DoctorId == doctor.Id));
                _context.Doctors.Remove(doctor);
            }

            await _context.SaveChangesAsync();

            // 3- دلوقتي امسح اليوزر نفسه وهيتمسح بدون اي ايرور
            await _userManager.DeleteAsync(user);



            return RedirectToAction(nameof(Index));
        }

         
        
    }
}