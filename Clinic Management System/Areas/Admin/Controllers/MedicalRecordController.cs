using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{CD.ADMIN_ROLE} ,{CD.SUPER_ADMIN_ROLE}")]
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

            ViewBag.Prescriptions = await _context.Prescriptions.AsNoTracking()
                .Where(p => p.MedicalRecordId == id)
                .OrderByDescending(p => p.Date)
                .ToListAsync();

            return View(record);
        }

        // appointmentId (optional): coming from the Appointments page pre-fills patient + doctor
        [HttpGet]
        public async Task<IActionResult> Create(int? appointmentId)
        {
            var record = new MedicalRecord();

            if (appointmentId.HasValue)
            {
                var appt = await _context.Appointments.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId != null);
                if (appt == null) return NotFound();

                record.AppointmentId = appt.Id;
                record.PatientId = appt.PatientId;
                record.DoctorId = appt.DoctorId;
            }

            await PopulateSelects(record.PatientId, record.DoctorId, record.AppointmentId);
            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MedicalRecord record)
        {
            ModelState.Remove(nameof(record.Patient));
            ModelState.Remove(nameof(record.Doctor));
            ModelState.Remove(nameof(record.Appointment));

            Appointment? appt = null;
            if (record.AppointmentId.HasValue)
            {
                appt = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == record.AppointmentId && a.PatientId != null);

                if (appt == null)
                {
                    ModelState.AddModelError(nameof(record.AppointmentId), "Appointment not found or it has no patient.");
                }
                else
                {
                    // the appointment decides who the record belongs to
                    record.PatientId = appt.PatientId;
                    record.DoctorId = appt.DoctorId;
                    ModelState.Remove(nameof(record.PatientId));
                    ModelState.Remove(nameof(record.DoctorId));
                }
            }
            else
            {
                if (!record.PatientId.HasValue)
                    ModelState.AddModelError(nameof(record.PatientId), "Select a patient.");
                if (!record.DoctorId.HasValue)
                    ModelState.AddModelError(nameof(record.DoctorId), "Select a doctor.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateSelects(record.PatientId, record.DoctorId, record.AppointmentId);
                return View(record);
            }

            record.Id = 0;
            record.CreatedAt = DateTime.UtcNow;
            _context.MedicalRecords.Add(record);

            // a record means the visit happened: close the appointment so the patient can review the doctor
            if (appt != null && (appt.Status == AppointmentStatus.Pending || appt.Status == AppointmentStatus.Confirmed))
                appt.Status = AppointmentStatus.Completed;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Medical record saved. You can now add lab results or a prescription.";
            return RedirectToAction(nameof(Details), new { id = record.Id });
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

            ModelState.Remove(nameof(record.Patient));
            ModelState.Remove(nameof(record.Doctor));
            ModelState.Remove(nameof(record.Appointment));

            var existing = await _context.MedicalRecords.FirstOrDefaultAsync(m => m.Id == id);
            if (existing == null) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateSelects(record.PatientId, record.DoctorId, record.AppointmentId);
                return View(record);
            }

            // copy only the editable fields; CreatedAt stays as it was
            existing.PatientId = record.PatientId;
            existing.DoctorId = record.DoctorId;
            existing.AppointmentId = record.AppointmentId;
            existing.Diagnosis = record.Diagnosis;
            existing.Symptoms = record.Symptoms;
            existing.MedicalHistory = record.MedicalHistory;
            existing.VitalSigns = record.VitalSigns;
            existing.Notes = record.Notes;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Medical record updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST delete (ensure labResults removed first)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.MedicalRecords.FindAsync(id);
            if (record == null) return RedirectToAction(nameof(Index));

            // prescriptions (and their items) hang on the record: remove them first
            var prescriptionIds = await _context.Prescriptions
                .Where(p => p.MedicalRecordId == id).Select(p => p.Id).ToListAsync();
            if (prescriptionIds.Any())
            {
                var items = await _context.PrescriptionItems
                    .Where(i => prescriptionIds.Contains(i.PrescriptionId)).ToListAsync();
                _context.PrescriptionItems.RemoveRange(items);
                var prescriptions = await _context.Prescriptions
                    .Where(p => p.MedicalRecordId == id).ToListAsync();
                _context.Prescriptions.RemoveRange(prescriptions);
                await _context.SaveChangesAsync();
            }

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

            var booked = await _context.Appointments.AsNoTracking()
                .Include(a => a.Patient)
                .Where(a => a.PatientId != null && a.Status != AppointmentStatus.Cancelled)
                .OrderByDescending(a => a.StartTime)
                .Select(a => new { a.Id, PatientName = a.Patient!.Name, a.StartTime })
                .Take(200)
                .ToListAsync();

            ViewBag.Appointments = new SelectList(
                booked.Select(a => new
                {
                    a.Id,
                    DisplayName = $"Appt #{a.Id} - {a.PatientName ?? "Unknown"} - {a.StartTime:dd MMM HH:mm}"
                }),
                "Id", "DisplayName", selectedAppointmentId);
        }
    }
}
