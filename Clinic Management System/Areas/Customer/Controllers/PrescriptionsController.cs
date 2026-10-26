using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class PrescriptionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public PrescriptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // My prescriptions (optionally only the ones of one appointment)
        public async Task<IActionResult> Index(int? appointmentId)
        {
            var patient = await PatientHelper.FindAsync(_context, User);
            if (patient == null) return View(new List<Prescription>());

            var query = _context.Prescriptions.AsNoTracking()
                .Include(p => p.MedicalRecord)
                    .ThenInclude(m => m!.Doctor)
                .Where(p => p.MedicalRecord != null && p.MedicalRecord.PatientId == patient.Id);

            if (appointmentId != null)
                query = query.Where(p => p.MedicalRecord!.AppointmentId == appointmentId);

            return View(await query.OrderByDescending(p => p.Date).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var patient = await PatientHelper.FindAsync(_context, User);
            if (patient == null) return NotFound();

            // only my own prescriptions: another patient's id returns 404
            var prescription = await _context.Prescriptions.AsNoTracking()
                .Include(p => p.MedicalRecord)
                    .ThenInclude(m => m!.Doctor)
                .FirstOrDefaultAsync(p => p.Id == id
                                          && p.MedicalRecord != null
                                          && p.MedicalRecord.PatientId == patient.Id);
            if (prescription == null) return NotFound();

            ViewBag.Items = await _context.PrescriptionItems.AsNoTracking()
                .Where(i => i.PrescriptionId == id)
                .ToListAsync();

            return View(prescription);
        }
    }
}
