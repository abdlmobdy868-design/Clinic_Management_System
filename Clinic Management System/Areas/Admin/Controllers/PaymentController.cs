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
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .Include(p => p.Patient)
                .Include(p => p.Appointment)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View(payments);
        }

        
        public async Task<IActionResult> Details(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Patient)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound();

            return View(payment);
        }

        
        [HttpGet]
        public async Task<IActionResult> Create(int? appointmentId, int? patientId)
        {
            ViewBag.Patients = new SelectList(await _context.Patients.ToListAsync(), "Id", "Name", patientId);
            ViewBag.Appointments = new SelectList(
                await _context.Appointments
                    .Include(a => a.Patient)
                    .Select(a => new { Id = a.Id, DisplayName = $"Appt #{a.Id} - Patient: {a.Patient.Name}" })
                    .ToListAsync(),
                "Id",
                "DisplayName",
                appointmentId
            );

            return View();
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment payment)
        {
            ModelState.Remove(nameof(payment.Appointment));
            ModelState.Remove(nameof(payment.Patient));

            if (ModelState.IsValid)
            {
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Patients = new SelectList(await _context.Patients.ToListAsync(), "Id", "Name", payment.PatientId);
            ViewBag.Appointments = new SelectList(
                await _context.Appointments
                    .Include(a => a.Patient)
                    .Select(a => new { Id = a.Id, DisplayName = $"Appt #{a.Id} - Patient: {a.Patient.Name}" })
                    .ToListAsync(),
                "Id",
                "DisplayName",
                payment.AppointmentId
            );

            return View(payment);
        }

        
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound();

            ViewBag.Patients = new SelectList(await _context.Patients.ToListAsync(), "Id", "Name", payment.PatientId);
            ViewBag.Appointments = new SelectList(
                await _context.Appointments
                    .Include(a => a.Patient)
                    .Select(a => new { Id = a.Id, DisplayName = $"Appt #{a.Id} - Patient: {a.Patient.Name}" })
                    .ToListAsync(),
                "Id",
                "DisplayName",
                payment.AppointmentId
            );

            return View(payment);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Payment payment)
        {
            if (id != payment.Id)
                return BadRequest();

            ModelState.Remove(nameof(payment.Appointment));
            ModelState.Remove(nameof(payment.Patient));

            if (ModelState.IsValid)
            {
                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Patients = new SelectList(await _context.Patients.ToListAsync(), "Id", "Name", payment.PatientId);
            ViewBag.Appointments = new SelectList(
                await _context.Appointments
                    .Include(a => a.Patient)
                    .Select(a => new { Id = a.Id, DisplayName = $"Appt #{a.Id} - Patient: {a.Patient.Name}" })
                    .ToListAsync(),
                "Id",
                "DisplayName",
                payment.AppointmentId
            );

            return View(payment);
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}