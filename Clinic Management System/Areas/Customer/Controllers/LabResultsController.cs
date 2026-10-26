using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class LabResultsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public LabResultsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? appointmentId)
        {
            var patient = await PatientHelper.FindAsync(_context, User);
            if (patient == null) return View(new List<labResult>());

            var query = _context.labResults.AsNoTracking()
                .Include(x => x.MedicalRecord)
                .Include(x => x.LabTest)
                .Where(x => x.MedicalRecord != null && x.MedicalRecord.PatientId == patient.Id);

            if (appointmentId != null)
                query = query.Where(x => x.MedicalRecord!.AppointmentId == appointmentId);

            return View(await query.OrderByDescending(x => x.Date).ToListAsync());
        }

        public async Task<IActionResult> Download(int id)
        {
            var patient = await PatientHelper.FindAsync(_context, User);
            if (patient == null) return NotFound();

            var file = await _context.labResults.AsNoTracking()
                .Include(x => x.MedicalRecord)
                .FirstOrDefaultAsync(x => x.Id == id
                                          && x.MedicalRecord != null
                                          && x.MedicalRecord.PatientId == patient.Id);

            if (file == null || string.IsNullOrEmpty(file.ResultFileUrl))
                return NotFound();

            // ResultFileUrl is a web path like /uploads/labs/xxx.pdf
            return File(file.ResultFileUrl, "application/pdf");
        }
    }
}
