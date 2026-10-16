using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class MedicalRecordController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MedicalRecordController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var records = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(records);
        }

        public async Task<IActionResult> Details(int id)
        {
            var record = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .Include(m => m.Appointment)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (record == null) return NotFound();

            ViewBag.LabResults = await _context.labResults
                .Include(l => l.LabTest)
                .Where(l => l.MedicalRecordId == id)
                .ToListAsync();

            return View(record);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateSelects(null, null, null);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MedicalRecord record)
        {
             ModelState.Clear();
            record.CreatedAt = DateTime.UtcNow;
                _context.MedicalRecords.Add(record);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            

        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var record = await _context.MedicalRecords.FindAsync(id);
            if (record == null) return NotFound();

            await PopulateSelects(record.PatientId, record.DoctorId, record.AppointmentId);
            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MedicalRecord record)
        {
            if (id != record.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                _context.MedicalRecords.Update(record);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateSelects(record.PatientId, record.DoctorId, record.AppointmentId);
            return View(record);
        }

        // POST delete (ensure labResults removed first)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.MedicalRecords.FindAsync(id);
            if (record == null) return RedirectToAction(nameof(Index));

            // delete lab results that reference this medical record first
            var labResults = await _context.labResults.Where(l => l.MedicalRecordId == id).ToListAsync();
            if (labResults.Any())
            {
                _context.labResults.RemoveRange(labResults);
                await _context.SaveChangesAsync();
            }

            _context.MedicalRecords.Remove(record);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSelects(int? selectedPatientId, int? selectedDoctorId, int? selectedAppointmentId)
        {
            ViewBag.Patients = new SelectList(
                await _context.Patients.Select(p => new { p.Id, Name = p.Name ?? "no name" }).ToListAsync(),
                "Id", "Name", selectedPatientId);

            ViewBag.Doctors = new SelectList(
                await _context.Doctors.Select(d => new { d.Id, Name = d.Name ?? "no name" }).ToListAsync(),
                "Id", "Name", selectedDoctorId);

            ViewBag.Appointments = new SelectList(
                await _context.Appointments.Include(a => a.Patient)
                   .Where(a => a.Patient != null)
                   .Select(a => new { Id = a.Id, DisplayName = "Appt #" + a.Id + " - " + (a.Patient!.Name ?? "Unknown") })
                   .ToListAsync(),
                "Id", "DisplayName", selectedAppointmentId);
        }
    }
}
