using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Clinic_Management_System.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet] 
        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var patient = _context.Patients.FirstOrDefault(p => p.ApplicationUserId == userId);
            if (patient == null) return View(new List<Review>());

            var myReviews = _context.Reviews
                .Include(r => r.Doctor)
                .Where(r => r.PatientId == patient.Id)
                .ToList();

            var myDoctors = _context.Appointments
                .Where(a => a.PatientId == patient.Id)
                .Include(a => a.Doctor)
                .Select(a => a.Doctor)
                .Distinct()
                .ToList();

            ViewBag.MyDoctors = myDoctors;
            return View(myReviews);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int id)
        {
            // id ده هو Appointment Id
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return NotFound();

            var review = new Review
            {
                DoctorId = appointment.DoctorId.Value,
            };

            ViewBag.DoctorName = appointment.Doctor.Name;
            return View(review);
        }

        [HttpPost]
        public IActionResult Create(Review review)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var patient = _context.Patients.FirstOrDefault(p => p.ApplicationUserId == userId);

            if (patient == null) return BadRequest("Patient not found");

            var hadVisit = _context.Appointments.Any(a => a.PatientId == patient.Id && a.DoctorId == review.DoctorId);
            if (!hadVisit) return BadRequest("You can only review after a visit");

            review.PatientId = patient.Id;
            review.Date = DateTime.Now;

            _context.Reviews.Add(review);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}