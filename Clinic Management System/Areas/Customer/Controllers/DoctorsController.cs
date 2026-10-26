using ClinicManagementSystem.Areas.Customer.ViewModels;
using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Areas.Customer.Controllers
{
    [Area("Customer")]
    [AllowAnonymous]
    public class DoctorsController : Controller
    {
        private const int PageSize = 9;
        private const int SlotHorizonDays = 14;

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET /Customer/Doctors?q=&clinicId=&departmentId=&specialization=&sort=&availableOnly=&page=
        public async Task<IActionResult> Index(
            string? q, int? clinicId, int? departmentId, string? specialization,
            string? sort, bool availableOnly = false, int page = 1)
        {
            sort = string.IsNullOrWhiteSpace(sort) ? "rating" : sort;
            var now = DateTime.Now;

            // ---- 1) filter doctors ----
            var query = _context.Doctors.AsNoTracking()
                .Include(d => d.Department).ThenInclude(dep => dep!.Clinic)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(d =>
                    (d.Name != null && d.Name.Contains(term)) ||
                    (d.Specialization != null && d.Specialization.Contains(term)));
            }
            if (departmentId.HasValue)
                query = query.Where(d => d.DepartmentId == departmentId);
            if (clinicId.HasValue)
                query = query.Where(d => d.Department != null && d.Department.ClinicId == clinicId);
            if (!string.IsNullOrWhiteSpace(specialization))
                query = query.Where(d => d.Specialization == specialization);

            var doctors = await query.ToListAsync();
            var ids = doctors.Select(d => d.Id).ToList();

            // ---- 2) ratings ----
            var ratingMap = (await _context.Reviews.AsNoTracking()
                    .Where(r => ids.Contains(r.DoctorId))
                    .GroupBy(r => r.DoctorId)
                    .Select(g => new { DoctorId = g.Key, Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
                    .ToListAsync())
                .ToDictionary(x => x.DoctorId);

            // ---- 3) available appointments (unbooked & available in future) ----
            var appointmentMap = (await _context.Appointments.AsNoTracking()
                    .Where(a => a.DoctorId != null && ids.Contains(a.DoctorId.Value)
                                && a.IsAvailable && a.PatientId == null
                                && a.StartTime >= now && a.Status != AppointmentStatus.Cancelled)
                    .GroupBy(a => a.DoctorId)
                    .Select(g => new { DoctorId = g.Key, Next = g.Min(a => a.StartTime), Price = g.Min(a => a.price), Count = g.Count() })
                    .ToListAsync())
                .Where(x => x.DoctorId.HasValue)
                .ToDictionary(x => x.DoctorId!.Value);

            var cards = doctors.Select(d =>
            {
                ratingMap.TryGetValue(d.Id, out var r);
                appointmentMap.TryGetValue(d.Id, out var a);
                return new DoctorCardVM
                {
                    Id = d.Id,
                    Name = d.Name ?? "Doctor",
                    Specialization = d.Specialization ?? string.Empty,
                    ExperienceYears = d.ExperienceYears,
                    DepartmentName = d.Department?.Name,
                    ClinicName = d.Department?.Clinic?.Name,
                    AverageRating = r?.Avg ?? 0,
                    ReviewCount = r?.Count ?? 0,
                    OpenSlots = a?.Count ?? 0,
                    NextSlot = a?.Next,
                    FromPrice = a?.Price
                };
            }).ToList();

            if (availableOnly)
                cards = cards.Where(c => c.OpenSlots > 0).ToList();

            // ---- 4) sort ----
            IEnumerable<DoctorCardVM> sorted = sort switch
            {
                "experience" => cards.OrderByDescending(c => c.ExperienceYears).ThenBy(c => c.Name),
                "name" => cards.OrderBy(c => c.Name),
                "soonest" => cards.OrderBy(c => c.NextSlot ?? DateTime.MaxValue).ThenBy(c => c.Name),
                _ => cards.OrderByDescending(c => c.AverageRating).ThenByDescending(c => c.ReviewCount).ThenBy(c => c.Name)
            };

            // ---- 5) paging ----
            var total = cards.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            page = Math.Min(Math.Max(1, page), totalPages);
            var pageItems = sorted.Skip((page - 1) * PageSize).Take(PageSize).ToList();

            // ---- 6) dropdown data ----
            var allClinics = await _context.Clinics.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            var allDepartments = await _context.Departments.AsNoTracking()
                .Include(d => d.Clinic).OrderBy(d => d.Name).ToListAsync();
            var specializations = await _context.Doctors.AsNoTracking()
                .Where(d => d.Specialization != null && d.Specialization != "")
                .Select(d => d.Specialization!)
                .Distinct().OrderBy(s => s).ToListAsync();

            var vm = new DoctorListVM
            {
                Q = q,
                ClinicId = clinicId,
                DepartmentId = departmentId,
                Specialization = specialization,
                Sort = sort,
                AvailableOnly = availableOnly,
                Doctors = pageItems,
                TotalResults = total,
                Page = page,
                PageSize = PageSize,
                ClinicOptions = new SelectList(allClinics, "Id", "Name", clinicId),
                DepartmentOptions = new SelectList(
                    allDepartments.Select(d => new
                    {
                        d.Id,
                        Label = d.Clinic != null ? $"{d.Name} - {d.Clinic.Name}" : d.Name
                    }),
                    "Id", "Label", departmentId),
                SpecializationOptions = new SelectList(specializations, specialization)
            };

            return View(vm);
        }

        // GET /Customer/Doctors/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var doctor = await _context.Doctors.AsNoTracking()
                .Include(d => d.Department).ThenInclude(dep => dep!.Clinic)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null) return NotFound();

            // ---- reviews ----
            var reviews = await _context.Reviews.AsNoTracking()
                .Include(r => r.Patient).ThenInclude(p => p.ApplicationUser)
                .Where(r => r.DoctorId == id)
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            var breakdown = Enumerable.Range(1, 5).Reverse()
                .ToDictionary(stars => stars, stars => reviews.Count(r => r.Rating == stars));

            // ---- available appointments for next horizon days ----
            var now = DateTime.Now;
            var horizon = now.AddDays(SlotHorizonDays);

            var availableAppointments = await _context.Appointments.AsNoTracking()
                .Where(a => a.DoctorId == id && a.IsAvailable && a.PatientId == null
                            && a.StartTime >= now && a.StartTime <= horizon
                            && a.Status != AppointmentStatus.Cancelled)
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            var appointmentDays = availableAppointments
                .GroupBy(a => a.StartTime.Date)
                .Select(g => new SlotDayVM
                {
                    Date = g.Key,
                    Slots = g.Select(a => new SlotVM
                    {
                        Id = a.Id,
                        Start = a.StartTime,
                        End = a.EndTime,
                        Price = a.price
                    }).ToList()
                })
                .ToList();

            var vm = new DoctorDetailsVM
            {
                Id = doctor.Id,
                Name = doctor.Name ?? "Doctor",
                Specialization = doctor.Specialization ?? string.Empty,
                ExperienceYears = doctor.ExperienceYears,
                DepartmentId = doctor.DepartmentId,
                DepartmentName = doctor.Department?.Name,
                ClinicId = doctor.Department?.ClinicId,
                ClinicName = doctor.Department?.Clinic?.Name,
                ClinicAddress = doctor.Department?.Clinic?.Address,
                ClinicPhone = doctor.Department?.Clinic?.Phone,
                ReviewCount = reviews.Count,
                AverageRating = reviews.Count == 0 ? 0 : reviews.Average(r => (double)r.Rating),
                RatingBreakdown = breakdown,
                Reviews = reviews.Take(20).Select(r => new ReviewVM
                {
                    ReviewerName = FirstName(r.Patient?.Name ?? r.Patient?.ApplicationUser?.FullName),
                    Rating = r.Rating,
                    Comment = r.Comment,
                    Date = r.Date
                }).ToList(),
                SlotDays = appointmentDays,
                TotalOpenSlots = availableAppointments.Count
            };

            return View(vm);
        }
        // Claims a free slot for the logged-in patient.
        // The UPDATE only succeeds while the slot is still free, so two people clicking
        // at the same time can never book the same slot (the second one gets 0 rows).
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(int appointmentId)
        {
            var patient = await PatientHelper.GetOrCreateAsync(_context, _userManager, User);
            if (patient == null) return Challenge();

            var slot = await _context.Appointments.AsNoTracking()
                .Where(a => a.Id == appointmentId)
                .Select(a => new { a.DoctorId })
                .FirstOrDefaultAsync();
            if (slot == null) return NotFound();

            var now = DateTime.Now;
            var claimed = await _context.Appointments
                .Where(a => a.Id == appointmentId
                            && a.PatientId == null
                            && a.IsAvailable
                            && a.Status != AppointmentStatus.Cancelled
                            && a.StartTime > now)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(a => a.PatientId, (int?)patient.Id)
                    .SetProperty(a => a.IsAvailable, false)
                    .SetProperty(a => a.Status, AppointmentStatus.Pending));

            if (claimed == 0)
            {
                TempData["Error"] = "Sorry, this time was just taken. Please choose another one.";
                return slot.DoctorId.HasValue
                    ? RedirectToAction(nameof(Details), new { id = slot.DoctorId.Value })
                    : RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Appointment booked. Please pay to confirm it.";
            return RedirectToAction("UserAppointment", "Profile", new { area = "Customer" });
        }

        // Demo shortcut: lets the patient (or an admin) mark a visit as done so reviews and
        // records unlock. Later this should be done by the Doctor, not the patient.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null) return NotFound();

            var isAdmin = User.IsInRole(CD.ADMIN_ROLE) || User.IsInRole(CD.SUPER_ADMIN_ROLE);
            if (!isAdmin)
            {
                var patient = await PatientHelper.FindAsync(_context, User);
                if (patient == null || appointment.PatientId != patient.Id) return Forbid();
            }

            if (appointment.Status == AppointmentStatus.Pending || appointment.Status == AppointmentStatus.Confirmed)
            {
                appointment.Status = AppointmentStatus.Completed;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("UserAppointment", "Profile", new { area = "Customer" });
        }

        private static string FirstName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "Patient";
            return fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        }
    }
}