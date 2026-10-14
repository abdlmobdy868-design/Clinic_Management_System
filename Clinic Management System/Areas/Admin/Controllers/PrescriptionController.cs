using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Clinic_Management_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class PrescriptionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PrescriptionController(ApplicationDbContext context)
        {
            _context = context;
        }

     
        public async Task<IActionResult> Index()
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.MedicalRecord)
                    .ThenInclude(m => m.Patient)
                .Include(p => p.MedicalRecord)
                    .ThenInclude(m => m.Doctor)
                .OrderByDescending(p => p.Date)
                .ToListAsync();

            return View(prescriptions);
        }

        
        public async Task<IActionResult> Details(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.MedicalRecord)
                    .ThenInclude(m => m.Patient)
                .Include(p => p.MedicalRecord)
                    .ThenInclude(m => m.Doctor)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescription == null)
                return NotFound();

            ViewBag.PrescriptionItems = await _context.PrescriptionItems
                .Where(e => e.PrescriptionId == id)
                .ToListAsync();

            return View(prescription);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? medicalRecordId)
        {
            ViewBag.MedicalRecords = new SelectList(
                await _context.MedicalRecords
                    .Include(m => m.Patient)
                    .Select(m => new { Id = m.Id, DisplayName = $"Record #{m.Id} - Patient: {m.Patient.Name}" })
                    .ToListAsync(),
                "Id",
                "DisplayName",
                medicalRecordId
            );

            return View();
        }

       
        [HttpPost]
        
        public async Task<IActionResult> Create(Prescription prescription)
        {
            ModelState.Remove(nameof(prescription.MedicalRecord));

            if (ModelState.IsValid)
            {
                prescription.Date = DateTime.UtcNow;

                _context.Prescriptions.Add(prescription);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = prescription.Id });
            }

            ViewBag.MedicalRecords = new SelectList(
                await _context.MedicalRecords
                    .Include(m => m.Patient)
                    .Select(m => new { Id = m.Id, DisplayName = $"Record #{m.Id} - Patient: {m.Patient.Name}" })
                    .ToListAsync(),
                "Id",
                "DisplayName",
                prescription.MedicalRecordId
            );

            return View(prescription);
        }

       
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null)
                return NotFound();

            ViewBag.MedicalRecords = new SelectList(
                await _context.MedicalRecords
                    .Include(m => m.Patient)
                    .Select(m => new { Id = m.Id, DisplayName = $"Record #{m.Id} - Patient: {m.Patient.Name}" })
                    .ToListAsync(),
                "Id",
                "DisplayName",
                prescription.MedicalRecordId
            );

            return View(prescription);
        }

      
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Prescription prescription)
        {
            if (id != prescription.Id)
                return BadRequest();

            ModelState.Remove(nameof(prescription.MedicalRecord));

            if (ModelState.IsValid)
            {
                _context.Prescriptions.Update(prescription);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.MedicalRecords = new SelectList(
                await _context.MedicalRecords
                    .Include(m => m.Patient)
                    .Select(m => new { Id = m.Id, DisplayName = $"Record #{m.Id} - Patient: {m.Patient.Name}" })
                    .ToListAsync(),
                "Id",
                "DisplayName",
                prescription.MedicalRecordId
            );

            return View(prescription);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription != null)
            {
                _context.Prescriptions.Remove(prescription);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}