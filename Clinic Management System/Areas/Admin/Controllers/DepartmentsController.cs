using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DepartmentsController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var list = await _context.Departments
                .Include(d => d.Clinic)
                .Select(d => new ClinicManagementSystem.Models.DepartmentVM
                {
                    Id = d.Id,
                    Name = d.Name,
                    ClinicName = d.Clinic != null ? d.Clinic.Name : string.Empty,
                    DoctorsCount = _context.Doctors.Count(x => x.DepartmentId == d.Id)
                })
                .ToListAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Clinics = _context.Clinics.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Department dept)
        {
            ModelState.Remove("Clinic");
            if (string.IsNullOrWhiteSpace(dept.Name)) ModelState.AddModelError("Name", "Name is required");
            if (_context.Departments.Any(d => d.Name == dept.Name)) ModelState.AddModelError("Name", "A department with this name already exists.");

            if (!ModelState.IsValid)
            {
                ViewBag.Clinics = _context.Clinics.ToList();
                return View(dept);
            }

            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Department added successfully";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();
            ViewBag.Clinics = _context.Clinics.ToList();
            return View(dept);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Department model)
        {
            if (id != model.Id) return BadRequest();

            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.Name)) ModelState.AddModelError("Name", "Name is required");
            if (_context.Departments.Any(d => d.Name == model.Name && d.Id != id)) ModelState.AddModelError("Name", "A department with this name already exists.");

            if (!ModelState.IsValid)
            {
                ViewBag.Clinics = _context.Clinics.ToList();
                return View(model);
            }

            try
            {
                dept.Name = model.Name;
                dept.ClinicId = model.ClinicId;
                _context.Update(dept);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Department updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DepartmentExists(id)) return NotFound();
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var dept = await _context.Departments.Include(d => d.Clinic).FirstOrDefaultAsync(d => d.Id == id);
            if (dept == null) return NotFound();
            var hasDoctors = _context.Doctors.Any(d => d.DepartmentId == id);
            ViewBag.HasDoctors = hasDoctors;
            return View(dept);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();
            var hasDoctors = _context.Doctors.Any(d => d.DepartmentId == id);
            if (hasDoctors)
            {
                TempData["Error"] = "Cannot delete department that has assigned doctors.";
                return RedirectToAction(nameof(Index));
            }

            _context.Departments.Remove(dept);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Department deleted successfully";
            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int id) => _context.Departments.Any(e => e.Id == id);
    }
}
