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
            ViewBag.Specializations = GetSpecializations();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Doctor doctor, string email, string password)
        {
            if (doctor.DepartmentId == 0)
            {
                ModelState.AddModelError("DepartmentId", "Please select a department.");
            }
            else if (!_context.Departments.Any(d => d.Id == doctor.DepartmentId))
            {
                ModelState.AddModelError("DepartmentId", "The selected department does not exist.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", doctor.DepartmentId);
                ViewBag.Specializations = GetSpecializations();
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
                    ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", doctor.DepartmentId);
                    ViewBag.Specializations = GetSpecializations();
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
                ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", doctor.DepartmentId);
                ViewBag.Specializations = GetSpecializations();
                return View(doctor);
            }
        }

        // UPDATE
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();

            ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", doctor.DepartmentId);
            ViewBag.Specializations = GetSpecializations();
            return View(doctor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Doctor d)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", d.DepartmentId);
                ViewBag.Specializations = GetSpecializations();
                return View(d);
            }

            var existing = await _context.Doctors.FindAsync(d.Id);
            if (existing == null) return NotFound();

            existing.Name = d.Name;
            existing.Specialization = d.Specialization;
            existing.ExperienceYears = d.ExperienceYears;
            existing.DepartmentId = d.DepartmentId;

            _context.Doctors.Update(existing);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            // id may be Doctor.Id or ApplicationUserId
            var doctor = await _context.Doctors
               .FirstOrDefaultAsync(d => d.ApplicationUserId == id || d.Id.ToString() == id);

            if (doctor == null) return RedirectToAction(nameof(Index));

            // 1- Delete lab results that belong to medical records of this doctor
            var medicalRecords = await _context.MedicalRecords
                .Where(m => m.DoctorId == doctor.Id)
                .ToListAsync();

            var labResultsToRemove = await _context.labResults
                .Where(l => medicalRecords.Select(m => m.Id).Contains(l.MedicalRecordId))
                .ToListAsync();

            if (labResultsToRemove.Any())
            {
                _context.labResults.RemoveRange(labResultsToRemove);
                await _context.SaveChangesAsync();
            }

            // 2- Delete medical records for this doctor
            if (medicalRecords.Any())
            {
                _context.MedicalRecords.RemoveRange(medicalRecords);
                await _context.SaveChangesAsync();
            }

            // 3- Delete appointments for this doctor
            var appointments = await _context.Appointments
               .Where(a => a.DoctorId == doctor.Id)
               .ToListAsync();
            if (appointments.Any())
            {
                _context.Appointments.RemoveRange(appointments);
                await _context.SaveChangesAsync();
            }

            // 4- Remove roles (AspNetUserRoles) and delete AspNetUser if present
            if (!string.IsNullOrEmpty(doctor.ApplicationUserId))
            {
                var userRoles = await _context.UserRoles
                   .Where(r => r.UserId == doctor.ApplicationUserId)
                   .ToListAsync();
                if (userRoles.Any())
                {
                    _context.UserRoles.RemoveRange(userRoles);
                    await _context.SaveChangesAsync();
                }

                var user = await _userManager.FindByIdAsync(doctor.ApplicationUserId);
                if (user != null)
                {
                    await _userManager.DeleteAsync(user);
                }
            }

            // 5- Finally remove the doctor
            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private List<string> GetSpecializations()
        {
            // return existing specializations from DB or a default list
            var list = _context.Doctors
                .Where(d => !string.IsNullOrEmpty(d.Specialization))
                .Select(d => d.Specialization)
                .Distinct()
                .ToList();

            // fallback default specializations
            if (!list.Any())
            {
                list = new List<string> { "General", "Pediatrics", "Cardiology", "Dermatology" };
            }
            return list;
        }
    }
}
