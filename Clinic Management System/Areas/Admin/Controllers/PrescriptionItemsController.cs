using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Clinic_Management_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{CD.ADMIN_ROLE} ,{CD.SUPER_ADMIN_ROLE}")]
    public class PrescriptionItemController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PrescriptionItemController(ApplicationDbContext context)
        {
            _context = context;
        }

        
        public async Task<IActionResult> Index(int? prescriptionId)
        {
            var itemsQuery = _context.PrescriptionItems
                .Include(p => p.Prescription)
                    .ThenInclude(p => p.MedicalRecord)
                        .ThenInclude(m => m.Patient)
                .AsQueryable();

            if (prescriptionId.HasValue)
            {
                itemsQuery = itemsQuery.Where(p => p.PrescriptionId == prescriptionId.Value);
                ViewBag.PrescriptionId = prescriptionId.Value;
            }

            var items = await itemsQuery.ToListAsync();
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? prescriptionId)
        {
            ViewBag.Prescriptions = new SelectList(
                await _context.Prescriptions
                    .Include(p => p.MedicalRecord)
                        .ThenInclude(m => m.Patient)
                    .Select(p => new { Id = p.Id, DisplayName = $"Prescription #{p.Id} - Patient: {p.MedicalRecord.Patient.Name}" })
                    .ToListAsync(),
                "Id",
                "DisplayName",
                prescriptionId
            );

            return View();
        }

        
        [HttpPost]
        
        public async Task<IActionResult> Create(PrescriptionItems item)
        {
            ModelState.Remove(nameof(item.Prescription));

            if (ModelState.IsValid)
            {
                _context.PrescriptionItems.Add(item);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { prescriptionId = item.PrescriptionId });
            }

            ViewBag.Prescriptions = new SelectList(
                await _context.Prescriptions
                    .Include(p => p.MedicalRecord)
                        .ThenInclude(m => m.Patient)
                    .Select(p => new { Id = p.Id, DisplayName = $"Prescription #{p.Id} - Patient: {p.MedicalRecord.Patient.Name}" })
                    .ToListAsync(),
                "Id",
                "DisplayName",
                item.PrescriptionId
            );

            return View(item);
        }

        
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.PrescriptionItems.FindAsync(id);
            if (item == null)
                return NotFound();

            ViewBag.Prescriptions = new SelectList(
                await _context.Prescriptions
                    .Include(p => p.MedicalRecord)
                        .ThenInclude(m => m.Patient)
                    .Select(p => new { Id = p.Id, DisplayName = $"Prescription #{p.Id} - Patient: {p.MedicalRecord.Patient.Name}" })
                    .ToListAsync(),
                "Id",
                "DisplayName",
                item.PrescriptionId
            );

            return View(item);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PrescriptionItems item)
        {
            if (id != item.Id)
                return BadRequest();

            ModelState.Remove(nameof(item.Prescription));

            if (ModelState.IsValid)
            {
                _context.PrescriptionItems.Update(item);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { prescriptionId = item.PrescriptionId });
            }

            ViewBag.Prescriptions = new SelectList(
                await _context.Prescriptions
                    .Include(p => p.MedicalRecord)
                        .ThenInclude(m => m.Patient)
                    .Select(p => new { Id = p.Id, DisplayName = $"Prescription #{p.Id} - Patient: {p.MedicalRecord.Patient.Name}" })
                    .ToListAsync(),
                "Id",
                "DisplayName",
                item.PrescriptionId
            );

            return View(item);
        }

        [HttpPost]
        
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.PrescriptionItems.FindAsync(id);
            if (item != null)
            {
                int prescriptionId = item.PrescriptionId;
                _context.PrescriptionItems.Remove(item);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { prescriptionId });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}