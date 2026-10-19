using ClinicManagementSystem.Areas.Customer.ViewModels;
using ClinicManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Areas.Customer.Controllers
{
    [Area("Customer")]
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var clinics = await _context.Clinics.AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            var departments = await _context.Departments.AsNoTracking()
                .OrderBy(d => d.Name)
                .ToListAsync();

            // doctors per department
            var perDepartment = (await _context.Doctors.AsNoTracking()
                    .Where(d => d.DepartmentId != null)
                    .GroupBy(d => d.DepartmentId)
                    .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
                    .ToListAsync())
                .Where(x => x.DepartmentId.HasValue)
                .ToDictionary(x => x.DepartmentId!.Value, x => x.Count);

            var clinicNames = clinics.ToDictionary(c => c.Id, c => c.Name ?? "Clinic");

            var departmentCards = departments.Select(d => new DepartmentCardVM
            {
                Id = d.Id,
                Name = d.Name ?? string.Empty,
                ClinicId = d.ClinicId,
                ClinicName = d.ClinicId.HasValue && clinicNames.TryGetValue(d.ClinicId.Value, out var cn) ? cn : null,
                DoctorCount = perDepartment.TryGetValue(d.Id, out var count) ? count : 0
            }).ToList();

            var clinicCards = clinics.Select(c =>
            {
                var deps = departmentCards.Where(d => d.ClinicId == c.Id).ToList();
                return new ClinicCardVM
                {
                    Id = c.Id,
                    Name = c.Name ?? "Clinic",
                    Address = c.Address,
                    Phone = c.Phone,
                    Departments = deps,
                    DoctorCount = deps.Sum(d => d.DoctorCount)
                };
            }).ToList();

            var vm = new HomeVM
            {
                Clinics = clinicCards,
                Departments = departmentCards,
                TotalDoctors = await _context.Doctors.CountAsync()
            };

            return View(vm);
        }
    }
}
