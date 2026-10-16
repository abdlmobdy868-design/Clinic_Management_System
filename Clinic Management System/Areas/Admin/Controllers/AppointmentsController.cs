using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AppointmentsController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index() => View(await _context.Appointments.Include(a => a.Doctor).Include(a => a.Patient).ToListAsync());

        [HttpGet]
        public IActionResult Create()
        {
            PopulateSelects();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            if (appointment.DoctorId == 0 || appointment.PatientId == 0 || appointment.ClinicId == 0)
            {
                ModelState.AddModelError("", "Please select a doctor, patient, and clinic.");
            }

            if (!ModelState.IsValid)
            {
                PopulateSelects();
                return View(appointment);
            }

            try
            {
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error saving: " + ex.InnerException?.Message ?? ex.Message);
                PopulateSelects();
                return View(appointment);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            var app = await _context.Appointments.FindAsync(id);
            if (app != null)
            {
                if (Enum.TryParse<AppointmentStatus>(status, true, out var newStatus))
                {
                    app.Status = newStatus;
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }



        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var app = await _context.Appointments.FindAsync(id);
            if (app == null) return NotFound();

            ViewBag.Doctors = new SelectList(_context.Doctors, "Id", "Name", app.DoctorId);
            ViewBag.Patients = new SelectList(_context.Patients
                .Include(p => p.ApplicationUser)
                .ToList()
                .Select(p => new {
                    Id = p.Id,
                    Name = p.ApplicationUser?.FullName ?? p.ApplicationUser?.UserName ?? p.Name ?? $"Patient {p.Id}"
                }), "Id", "Name");
            ViewBag.Clinics = new SelectList(_context.Clinics, "Id", "Name", app.ClinicId);
            return View(app);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Appointment app)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Doctors = new SelectList(_context.Doctors, "Id", "Name", app.DoctorId);
                ViewBag.Patients = new SelectList(_context.Patients
                    .Include(p => p.ApplicationUser)
                    .ToList()
                    .Select(p => new {
                        Id = p.Id,
                        Name = p.ApplicationUser?.FullName ?? p.ApplicationUser?.UserName ?? p.Name ?? $"Patient {p.Id}"
                    }), "Id", "Name");
                ViewBag.Clinics = new SelectList(_context.Clinics, "Id", "Name", app.ClinicId);
                return View(app);
            }

            _context.Appointments.Update(app);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST delete (must be POST + antiforgery)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var app = await _context.Appointments.FindAsync(id);
            if (app != null)
            {
                _context.Appointments.Remove(app);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private void PopulateSelects()
        {
            ViewBag.Doctors = new SelectList(_context.Doctors.Where(d => d.Name != null).ToList(), "Id", "Name");
            var patients = _context.Patients.Include(p => p.ApplicationUser).ToList()
      .Select(p => new {
          Id = p.Id,
          Name = !string.IsNullOrEmpty(p.Name) ? p.Name : (p.ApplicationUser?.UserName ?? $"Patient {p.Id}")
      }).ToList();

            ViewBag.Patients = new SelectList(patients, "Id", "Name");
            ViewBag.PatientId = ViewBag.Patients; // ده المهم اللي الـ View بيقرأه
            ViewBag.Clinics = new SelectList(_context.Clinics.Where(c => c.Name != null).ToList(), "Id", "Name");
        }
    }
}
