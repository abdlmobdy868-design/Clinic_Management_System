using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Utilities;
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

            if (record == null)
                return NotFound();

           
            ViewBag.LabResults = await _context.labResults
                .Include(l => l.LabTest)
                .Where(l => l.MedicalRecordId == id)
                .ToListAsync();

            return View(record);
        }

        
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Patients = new SelectList(
                await _context.Users.Where(u => u.UserType == "Patient").Select(p => new { p.Id, FullName = p.FullName ?? "no name" }).ToListAsync(),
                "Id", "FullName");

            ViewBag.Doctors = new SelectList(
                await _context.Doctors.Select(d => new { d.Id, Name = d.Name ?? "no name" }).ToListAsync(),
                "Id", "Name");

            ViewBag.Appointments = new SelectList(
                await _context.Appointments.Include(a => a.Patient)
                   .Where(a => a.Patient != null)
                   .Select(a => new { Id = a.Id, DisplayName = "Appt #" + a.Id + " - " + (a.Patient.Name ?? "Unknown") })
                   .ToListAsync(),
                "Id", "DisplayName");

            return View();
        
        }

        [HttpPost]
        public async Task<IActionResult> Create(MedicalRecord record)
        {
            if (ModelState.IsValid)
            {
                record.CreatedAt = DateTime.UtcNow;
                _context.MedicalRecords.Add(record);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Patients = new SelectList(
     await _context.Users.Where(u => u.UserType == "Patient").Select(p => new { p.Id, FullName = p.FullName ?? "بدون اسم" }).ToListAsync(),
     "Id", "FullName", record.PatientId);

            ViewBag.Doctors = new SelectList(
                await _context.Doctors.Select(d => new { d.Id, Name = d.Name ?? "بدون اسم" }).ToListAsync(),
                "Id", "Name", record.DoctorId);

            ViewBag.Appointments = new SelectList(
                await _context.Appointments.Include(a => a.Patient)
                   .Where(a => a.Patient != null)
                   .Select(a => new { Id = a.Id, DisplayName = "Appt #" + a.Id + " - " + (a.Patient.Name ?? "Unknown") })
                   .ToListAsync(),
                "Id", "DisplayName", record.AppointmentId
            );

            return View(record);
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var record = await _context.MedicalRecords.FindAsync(id);
            if (record == null)
                return NotFound();

            ViewBag.Patients = new SelectList(await _context.Patients.ToListAsync(), "Id", "Name", record.PatientId);
            ViewBag.Doctors = new SelectList(await _context.Doctors.ToListAsync(), "Id", "Name", record.DoctorId);
            ViewBag.Appointments = new SelectList(
                await _context.Appointments
                    .Include(a => a.Patient)
                    .Select(a => new { Id = a.Id, DisplayName = $"Appt #{a.Id} - {a.Patient.Name}" })
                    .ToListAsync(),
                "Id",
                "DisplayName",
                record.AppointmentId
            );

            return View(record);
        }

        
        [HttpPost]
        public async Task<IActionResult> Edit(int id, MedicalRecord record)
        {
            if (id != record.Id)
                return BadRequest();

            if (ModelState.IsValid)
            {
                _context.MedicalRecords.Update(record);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Patients = new SelectList(await _context.Patients.ToListAsync(), "Id", "Name", record.PatientId);
            ViewBag.Doctors = new SelectList(await _context.Doctors.ToListAsync(), "Id", "Name", record.DoctorId);
            ViewBag.Appointments = new SelectList(
                await _context.Appointments
                    .Include(a => a.Patient)
                    .Select(a => new { Id = a.Id, DisplayName = $"Appt #{a.Id} - {a.Patient.Name}" })
                    .ToListAsync(),
                "Id",
                "DisplayName",
                record.AppointmentId
            );

            return View(record);
        }

        
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.MedicalRecords.FindAsync(id);
            if (record != null)
            {
                _context.MedicalRecords.Remove(record);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}