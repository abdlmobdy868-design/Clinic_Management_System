using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Utilities;
using ClinicManagementSystem.ViewModel; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize] 
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            
            var userId = _userManager.GetUserId(User);

            
            var patient = await _context.Patients
                .Include(p => p.ApplicationUser)
                .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

            
            if (patient == null)
            {
                
                patient = new Patient
                {
                    ApplicationUserId = userId,
                    Name = User.Identity?.Name,
                    Phone = "",
                    Address = ""
                };
            }

            return View(patient);
        }
        [HttpGet]
        public async Task<IActionResult> UserAppointment(string? status)
        {
            var userId = _userManager.GetUserId(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

            var query = _context.Appointments
                .IgnoreQueryFilters()
                .Include(a => a.Doctor)
                .Include(a => a.Clinic)
                .Include(a => a.Patient)
                .OrderByDescending(a => a.StartTime);

            List<Appointment> appointments;
            if (User.IsInRole("Admin"))
            {
                appointments = await query.ToListAsync();
            }
            else
            {
                if (patient == null) appointments = new List<Appointment>();
                else appointments = await query.Where(a => a.PatientId == patient.Id).ToListAsync();
            }

            if (Enum.TryParse<AppointmentStatus>(status, true, out var statusFilter))
                appointments = appointments.Where(a => a.Status == statusFilter).ToList();

            ViewBag.Status = status;
            ViewBag.Count = appointments.Count;
            return View(appointments);
        }




        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var patient = await PatientHelper.GetOrCreateAsync(_context, _userManager, User);
            if (patient == null) return Challenge();
            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Patient model)
        {
            // Always edit the logged-in user's own row; Id / ApplicationUserId from the form are ignored.
            var patientInDb = await PatientHelper.GetOrCreateAsync(_context, _userManager, User);
            if (patientInDb == null) return Challenge();

            if (string.IsNullOrWhiteSpace(model.Name) || model.Age < 0 || model.Age > 130)
            {
                ModelState.AddModelError(nameof(model.Name), "Please enter a valid name and age.");
                model.Id = patientInDb.Id;
                model.ApplicationUserId = patientInDb.ApplicationUserId;
                return View(model);
            }

            patientInDb.Name = model.Name.Trim();
            patientInDb.Phone = model.Phone;
            patientInDb.Age = model.Age;
            patientInDb.Gender = model.Gender;
            patientInDb.BloodType = model.BloodType;
            patientInDb.Address = model.Address;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Simulated payment: records a Payment and confirms the appointment.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int id)
        {
            var patient = await PatientHelper.FindAsync(_context, User);
            if (patient == null) return Forbid();

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.PatientId == patient.Id);
            if (appointment == null) return NotFound();

            if (appointment.Status != AppointmentStatus.Pending)
            {
                TempData["Error"] = "This appointment can't be paid.";
                return RedirectToAction(nameof(UserAppointment));
            }

            _context.Payments.Add(new Payment
            {
                AppointmentId = appointment.Id,
                PatientId = patient.Id,
                Amount = appointment.price,
                PaymentMethod = PaymentMethod.CreditCard,
                PaymentStatus = PaymentStatus.Paid
            });
            appointment.Status = AppointmentStatus.Confirmed;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment received. Your appointment is confirmed.";
            return RedirectToAction(nameof(UserAppointment));
        }

        // Cancelling frees the slot again so someone else can book it.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var patient = await PatientHelper.FindAsync(_context, User);
            if (patient == null) return Forbid();

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.PatientId == patient.Id);
            if (appointment == null) return NotFound();

            if (appointment.Status == AppointmentStatus.Pending || appointment.Status == AppointmentStatus.Confirmed)
            {
                var payments = await _context.Payments
                    .Where(p => p.AppointmentId == appointment.Id && p.PaymentStatus == PaymentStatus.Paid)
                    .ToListAsync();
                foreach (var pay in payments) pay.PaymentStatus = PaymentStatus.Refunded;

                appointment.PatientId = null;
                appointment.IsAvailable = true;
                appointment.Status = AppointmentStatus.Pending;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Appointment cancelled.";
            }

            return RedirectToAction(nameof(UserAppointment));
        }
    }
}
