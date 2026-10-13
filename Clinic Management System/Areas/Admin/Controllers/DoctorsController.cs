using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public DoctorsController(ApplicationDbContext c, UserManager<ApplicationUser> um) { _context = c; _userManager = um; }

        public async Task<IActionResult> Index() => View(await _context.Doctors.ToListAsync());
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Doctor doctor, string email, string password)
        {
            if (doctor.DepartmentId == null || doctor.DepartmentId == 0)
            {
                ModelState.AddModelError("DepartmentId", "Please select a department.");
            }
            else if (!_context.Departments.Any(d => d.Id == doctor.DepartmentId))
            {
                ModelState.AddModelError("DepartmentId", "The selected department does not exist.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name");
                return View(doctor);
            }

            try
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = doctor.Name,
                    EmailConfirmed = true,
                    UserType = "Doctor"
                };

                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                    ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name");
                    return View(doctor);
                }

                await _userManager.AddToRoleAsync(user, "Doctor");
                doctor.ApplicationUserId = user.Id;
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Doctor added successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while saving the doctor.");
                ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name");
                return View(doctor);
            }
        }

        // UPDATE
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            return View(doctor);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Doctor d)
        {
            _context.Doctors.Update(d);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // DELETE
        public async Task<IActionResult> Delete(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}