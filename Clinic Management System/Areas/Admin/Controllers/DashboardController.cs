using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Utilities;
using ClinicManagementSystem.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            var today = now.Date;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var chartStart = monthStart.AddMonths(-5); // last 6 months including current

            var user = await _userManager.GetUserAsync(User);

            var vm = new DashboardVM
            {
                FullName = user?.FullName ?? User.Identity?.Name ?? "User",
                UserType = user?.UserType ?? string.Empty,

                TotalPatients = await _context.Users.CountAsync(u => u.UserType == "Patient"),
                TotalDoctors = await _context.Doctors.CountAsync(),
                TotalClinics = await _context.Clinics.CountAsync(),
                TotalDepartments = await _context.Departments.CountAsync(),
                TotalLabTests = await _context.LabTests.CountAsync(),
                TotalAppointments = await _context.Appointments.CountAsync(),
                TodayAppointments = await _context.Appointments
                    .CountAsync(a => a.AppointmentDateTime >= today && a.AppointmentDateTime < today.AddDays(1)),
                PendingAppointments = await _context.Appointments
                    .CountAsync(a => a.Status == AppointmentStatus.Pending),

                TotalRevenue = await _context.Payments
                    .Where(p => p.PaymentStatus == PaymentStatus.Paid)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m,
                PendingPayments = await _context.Payments
                    .Where(p => p.PaymentStatus == PaymentStatus.Pending)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m,

                AverageRating = await _context.Reviews.AverageAsync(r => (double?)r.Rating) ?? 0,
                TotalReviews = await _context.Reviews.CountAsync(),

                MalePatients = await _context.Patients.CountAsync(p => p.Gender == Gender.male),
                FemalePatients = await _context.Patients.CountAsync(p => p.Gender == Gender.female)
            };

            // Revenue + appointments per month (last 6 months)
            var paid = await _context.Payments
                .Where(p => p.PaymentStatus == PaymentStatus.Paid && p.Appointment.AppointmentDateTime >= chartStart)
                .Select(p => new { p.Amount, p.Appointment.AppointmentDateTime })
                .ToListAsync();

            var apptDates = await _context.Appointments
                .Where(a => a.AppointmentDateTime >= chartStart)
                .Select(a => a.AppointmentDateTime)
                .ToListAsync();

            for (int i = 0; i < 6; i++)
            {
                var m = chartStart.AddMonths(i);
                vm.MonthLabels.Add(m.ToString("MMM yyyy"));
                vm.MonthlyRevenue.Add(paid
                    .Where(p => p.AppointmentDateTime.Year == m.Year && p.AppointmentDateTime.Month == m.Month)
                    .Sum(p => p.Amount));
                vm.MonthlyAppointments.Add(apptDates
                    .Count(d => d.Year == m.Year && d.Month == m.Month));
            }
            vm.ThisMonthRevenue = vm.MonthlyRevenue.Last();

            // Appointments by status
            foreach (var status in Enum.GetValues<AppointmentStatus>())
            {
                vm.StatusLabels.Add(status.ToString());
                vm.StatusCounts.Add(await _context.Appointments.CountAsync(a => a.Status == status));
            }

            // Recent appointments
            var recent = await _context.Appointments
                .OrderByDescending(a => a.AppointmentDateTime)
                .Take(8)
                .Select(a => new
                {
                    a.Id,
                    PatientName = a.Patient.Name,
                    DoctorName = a.Doctor.Name,
                    a.AppointmentDateTime,
                    a.Status,
                    a.price
                })
                .ToListAsync();

            vm.RecentAppointments = recent.Select(a => new RecentAppointmentRow
            {
                Id = a.Id,
                PatientName = a.PatientName,
                DoctorName = a.DoctorName,
                DateTime = a.AppointmentDateTime,
                Status = a.Status.ToString(),
                Price = a.price
            }).ToList();

            // Top doctors by (non-cancelled) appointments
            var apptCounts = await _context.Appointments
                .Where(a => a.Status != AppointmentStatus.Cancelled)
                .GroupBy(a => a.DoctorId)
                .Select(g => new { DoctorId = g.Key, Count = g.Count() })
                .ToListAsync();

            var ratings = await _context.Reviews
                .GroupBy(r => r.DoctorId)
                .Select(g => new { DoctorId = g.Key, Avg = g.Average(r => (double)r.Rating) })
                .ToListAsync();

            var doctors = await _context.Doctors
                .Select(d => new { d.Id, d.Name, d.Specialization })
                .ToListAsync();

            vm.TopDoctors = doctors
                .Select(d => new TopDoctorRow
                {
                    Name = d.Name,
                    Specialization = d.Specialization,
                    Appointments = apptCounts.FirstOrDefault(c => c.DoctorId == d.Id)?.Count ?? 0,
                    Rating = ratings.FirstOrDefault(r => r.DoctorId == d.Id)?.Avg ?? 0
                })
                .OrderByDescending(d => d.Appointments)
                .Take(5)
                .ToList();

            return View(vm);
        }
    }
}
