using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicManagementSystem.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class MedicalRecordsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public MedicalRecordsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? appointmentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var query = _context.MedicalRecords
                .Include(m => m.Appointment)
                .ThenInclude(a => a.Doctor)
                .Where(m => m.PatientId.ToString() == userId);

            if (appointmentId != null)
            {
                query = query.Where(m => m.AppointmentId == appointmentId);
            }

            var records = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
            return View(records);
        }

        public async Task<IActionResult> Details(int id)
        {
            var record = await _context.MedicalRecords
                .Include(m => m.Appointment)
                .ThenInclude(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (record == null) return NotFound();
            return View(record);
        }
    }
}