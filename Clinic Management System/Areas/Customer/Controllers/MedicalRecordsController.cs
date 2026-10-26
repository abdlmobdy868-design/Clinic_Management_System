using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            var patient = await PatientHelper.FindAsync(_context, User);
            if (patient == null) return View(new List<MedicalRecord>());

            // PatientId is the Patient table id (int), not the Identity user id (string)
            var query = _context.MedicalRecords.AsNoTracking()
                .Include(m => m.Appointment)
                .ThenInclude(a => a!.Doctor)
                .Where(m => m.PatientId == patient.Id);

            if (appointmentId != null)
                query = query.Where(m => m.AppointmentId == appointmentId);

            var records = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
            return View(records);
        }

        public async Task<IActionResult> Details(int id)
        {
            var patient = await PatientHelper.FindAsync(_context, User);
            if (patient == null) return NotFound();

            // Only my own records: another patient's id returns 404
            var record = await _context.MedicalRecords.AsNoTracking()
                .Include(m => m.Appointment)
                .ThenInclude(a => a!.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id && m.PatientId == patient.Id);

            if (record == null) return NotFound();
            return View(record);
        }
    }
}
