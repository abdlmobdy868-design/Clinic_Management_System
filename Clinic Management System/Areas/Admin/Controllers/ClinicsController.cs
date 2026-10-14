using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic_Management_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ClinicsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ClinicsController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index() => View(await _context.Clinics.Include(c => c.Department).ToListAsync());

        public IActionResult Create() { ViewBag.Departments = _context.Departments.ToList(); return View(); }

        [HttpPost]
        public async Task<IActionResult> Create(Clinic clinic)
        {
            if (ModelState.IsValid) { _context.Add(clinic); await _context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
            ViewBag.Departments = _context.Departments.ToList(); return View(clinic);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var clinic = await _context.Clinics.FindAsync(id);
            if (clinic == null) return NotFound();
            ViewBag.Departments = _context.Departments.ToList();
            return View(clinic);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Clinic clinic)
        {
            if (id != clinic.Id) return NotFound();
            if (ModelState.IsValid) { _context.Update(clinic); await _context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
            ViewBag.Departments = _context.Departments.ToList(); return View(clinic);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var clinic = await _context.Clinics.Include(c => c.Department).FirstOrDefaultAsync(c => c.Id == id);
            if (clinic == null) return NotFound();
            return View(clinic);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var clinic = await _context.Clinics.FindAsync(id);
            if (clinic != null) { _context.Clinics.Remove(clinic); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }
    }
}