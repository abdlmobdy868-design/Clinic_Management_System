using ClinicManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var results = _context.labResults
                .Include(x => x.MedicalRecord)
                .Include(x => x.LabTest)
                .Where(x => x.MedicalRecord != null && x.MedicalRecord.PatientId.ToString() == userId)
                .ToList();

            return View(results);
        }

        public IActionResult Download(int id)
        {
            var file = _context.labResults.Find(id);
            if (file == null || string.IsNullOrEmpty(file.ResultFileUrl))
                return NotFound();

            return File(file.ResultFileUrl, "application/pdf");
        }
    }
}