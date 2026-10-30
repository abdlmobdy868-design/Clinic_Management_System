using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic_Management_System.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = $"{CD.DOCTOR_ROLE}, {CD.ADMIN_ROLE}")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> MyAppointments()
        {
            var user = await _userManager.GetUserAsync(User);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == user.Id); // أضفنا await للأمان

            if (doctor == null)
            {
                TempData["Error"] = "Doctor profile Can't be found";
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            var appointments = await _context.Appointments
                .Include(e => e.Patient)
                .Where(e => e.DoctorId == doctor.Id && e.PatientId != null) 
                .OrderBy(e => e.AppointmentDateTime)
                .ToListAsync();

            
            var existingRecordAppointmentIds = await _context.MedicalRecords
                .Where(m => m.DoctorId == doctor.Id && m.AppointmentId != null)
                .Select(m => m.AppointmentId.Value)
                .ToListAsync();

            
            ViewBag.ExistingRecordIds = existingRecordAppointmentIds;

            return View(appointments);
        }
        [HttpGet]
        public async Task<IActionResult> CreateMedicalRecord(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null || appointment.PatientId == null)
            {
                return NotFound();
            }

            return View(appointment);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMedicalRecord(
            int appointmentId,
            string diagnosis,
            string? symptoms,
            string? medicalHistory,
            string? vitalSigns,
            string? recordNotes,
            string[] medicineNames,
            int[] dosages,
            string[] durations,
            string? prescriptionNotes)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null || appointment.PatientId == null)
            {
                return NotFound();
            }

            
            var medicalRecord = new MedicalRecord
            {
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                AppointmentId = appointment.Id,
                Diagnosis = diagnosis,
                Symptoms = symptoms,
                MedicalHistory = medicalHistory,
                VitalSigns = vitalSigns,
                Notes = recordNotes,
                CreatedAt = DateTime.UtcNow
            };

            _context.MedicalRecords.Add(medicalRecord);
            await _context.SaveChangesAsync(); 

            
            var prescription = new Prescription
            {
                MedicalRecordId = medicalRecord.Id,
                Date = DateTime.UtcNow,
                Notes = prescriptionNotes ?? string.Empty
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync(); 

            
            if (medicineNames != null && medicineNames.Length > 0)
            {
                for (int i = 0; i < medicineNames.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(medicineNames[i]))
                    {
                        var item = new PrescriptionItems
                        {
                            PrescriptionId = prescription.Id,
                            MedicineName = medicineNames[i],
                            Dosage = dosages != null && dosages.Length > i ? dosages[i] : 1,
                            Duration = durations != null && durations.Length > i ? durations[i] : "1 Week"
                        };
                        _context.PrescriptionItems.Add(item);
                    }
                }
                await _context.SaveChangesAsync();
            }

            
            appointment.Status = AppointmentStatus.Completed; 
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Medical record and prescription saved successfully!";
            return RedirectToAction(nameof(MyAppointments));
        }

        [HttpGet]
        public async Task<IActionResult> EditMedicalRecord(int appointmentId)
        {
            
            var medicalRecord = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .Include(m => m.Appointment)
                .FirstOrDefaultAsync(m => m.AppointmentId == appointmentId);

            
            if (medicalRecord == null)
            {
                TempData["Error"] = "No medical record found for this appointment to edit.";
                return RedirectToAction("MyAppointments");
            }

            
            var prescription = await _context.Prescriptions
                .FirstOrDefaultAsync(p => p.MedicalRecordId == medicalRecord.Id);

            List<PrescriptionItems> prescriptionItems = new List<PrescriptionItems>();

            if (prescription != null)
            {
                prescriptionItems = await _context.PrescriptionItems
                    .Where(pi => pi.PrescriptionId == prescription.Id)
                    .ToListAsync();
            }

            ViewBag.Prescription = prescription;
            ViewBag.PrescriptionItems = prescriptionItems;

            return View(medicalRecord);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMedicalRecord(
            int appointmentId, 
            string diagnosis,
            string? symptoms,
            string? medicalHistory,
            string? vitalSigns,
            string? recordNotes,
            string[]? medicineNames,
            int[]? dosages,
            string[]? durations,
            string? prescriptionNotes)
        {
            
            var medicalRecord = await _context.MedicalRecords
                .FirstOrDefaultAsync(m => m.AppointmentId == appointmentId);

            if (medicalRecord == null)
            {
                return NotFound();
            }

           
            medicalRecord.Diagnosis = diagnosis;
            medicalRecord.Symptoms = symptoms;
            medicalRecord.MedicalHistory = medicalHistory;
            medicalRecord.VitalSigns = vitalSigns;
            medicalRecord.Notes = recordNotes;

            _context.MedicalRecords.Update(medicalRecord);

            
            var prescription = await _context.Prescriptions
                .FirstOrDefaultAsync(p => p.MedicalRecordId == medicalRecord.Id);

            if (prescription == null)
            {
                prescription = new Prescription
                {
                    MedicalRecordId = medicalRecord.Id,
                    Date = DateTime.UtcNow,
                    Notes = prescriptionNotes ?? string.Empty
                };
                _context.Prescriptions.Add(prescription);
                await _context.SaveChangesAsync();
            }
            else
            {
                prescription.Notes = prescriptionNotes ?? string.Empty;
                _context.Prescriptions.Update(prescription);
            }

            
            var existingItems = await _context.PrescriptionItems
                .Where(pi => pi.PrescriptionId == prescription.Id)
                .ToListAsync();

            if (existingItems.Any())
            {
                _context.PrescriptionItems.RemoveRange(existingItems);
            }

            if (medicineNames != null && medicineNames.Length > 0)
            {
                for (int i = 0; i < medicineNames.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(medicineNames[i]))
                    {
                        var item = new PrescriptionItems
                        {
                            PrescriptionId = prescription.Id,
                            MedicineName = medicineNames[i],
                            Dosage = dosages != null && dosages.Length > i ? dosages[i] : 1,
                            Duration = durations != null && durations.Length > i ? durations[i] : "1 Week"
                        };
                        _context.PrescriptionItems.Add(item);
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Medical record and prescription updated successfully!";
            return RedirectToAction("MyAppointments");
        }
    }
}
