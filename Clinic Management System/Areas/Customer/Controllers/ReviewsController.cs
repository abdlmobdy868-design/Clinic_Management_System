using ClinicManagementSystem.Areas.Customer.ViewModels;
using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // My reviews
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var patient = await PatientHelper.FindAsync(_context, User);
            if (patient == null) return View(new List<Review>());

            var myReviews = await _context.Reviews.AsNoTracking()
                .Include(r => r.Doctor)
                .Where(r => r.PatientId == patient.Id)
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            return View(myReviews);
        }

        // id = Appointment id (comes from the "Leave Review" button)
        [HttpGet]
        public async Task<IActionResult> Create(int id)
        {
            var (appointment, error) = await GetReviewableAppointmentAsync(id);
            if (appointment == null) return error!;

            return View(new ReviewInputVM
            {
                AppointmentId = appointment.Id,
                DoctorName = appointment.Doctor?.Name ?? "Doctor"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReviewInputVM vm)
        {
            var (appointment, error) = await GetReviewableAppointmentAsync(vm.AppointmentId);
            if (appointment == null) return error!;

            vm.DoctorName = appointment.Doctor?.Name ?? "Doctor";
            if (!ModelState.IsValid) return View(vm);

            // DoctorId and PatientId are taken from the appointment / login, never from the form
            _context.Reviews.Add(new Review
            {
                DoctorId = appointment.DoctorId!.Value,
                PatientId = appointment.PatientId!.Value,
                Rating = vm.Rating,
                Comment = vm.Comment.Trim(),
                Date = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thanks for your review!";
            return RedirectToAction(nameof(Index));
        }

        // The appointment must be mine, completed, and I must not have reviewed this doctor yet.
        private async Task<(Appointment? appointment, IActionResult? error)> GetReviewableAppointmentAsync(int appointmentId)
        {
            var patient = await PatientHelper.FindAsync(_context, User);
            if (patient == null) return (null, Forbid());

            var appointment = await _context.Appointments.AsNoTracking()
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId == patient.Id);

            if (appointment == null || appointment.DoctorId == null) return (null, NotFound());

            if (appointment.Status != AppointmentStatus.Completed)
            {
                TempData["Error"] = "You can review a doctor only after the visit is completed.";
                return (null, RedirectToAction("UserAppointment", "Profile", new { area = "Customer" }));
            }

            var already = await _context.Reviews.AnyAsync(r => r.PatientId == patient.Id && r.DoctorId == appointment.DoctorId);
            if (already)
            {
                TempData["Error"] = "You already reviewed this doctor.";
                return (null, RedirectToAction(nameof(Index)));
            }

            return (appointment, null);
        }
    }
}
