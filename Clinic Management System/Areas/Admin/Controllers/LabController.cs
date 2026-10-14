using Clinic_Management_System.ViewModel;
using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Clinic_Management_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class LabController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LabController(ApplicationDbContext context)
        {
            _context = context;
        }

        
        public async Task<IActionResult> Index()
        {
            var lab = await _context.LabTests.ToListAsync(); 
            return View(lab);
        }

        
        [HttpGet]
        public IActionResult CreateTest()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTest(LabTestVM labTest)
        {
            if (!ModelState.IsValid)
                return View(labTest);

            LabTest lab = new LabTest()
            {
                Name = labTest.Name,
                Description = labTest.Description,
                Price = labTest.Price,
            };

            _context.LabTests.Add(lab);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        
        [HttpGet]
        public async Task<IActionResult> EditTest(int id)
        {
            var test = await _context.LabTests.FindAsync(id);
            if (test == null)
                return NotFound();

            return View(test);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTest(LabTest test)
        {
            if (!ModelState.IsValid)
                return View(test);

            _context.LabTests.Update(test);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        
        public async Task<IActionResult> DeleteTest(int id)
        {
            var test = await _context.LabTests.FindAsync(id);
            if (test != null)
            {
                _context.LabTests.Remove(test);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        
        public async Task<IActionResult> Results()
        {
            var results = await _context.labResults
                .Include(r => r.LabTest)
                .Include(r => r.MedicalRecord)
                    .ThenInclude(m => m.Patient)
                .ToListAsync();

            return View(results);
        }

        
        [HttpGet]
        public async Task<IActionResult> UploadResult(int? medicalRecordId)
        {
            ViewBag.LabTests = new SelectList(await _context.LabTests.ToListAsync(), "Id", "Name");

            ViewBag.MedicalRecords = new SelectList(
                await _context.MedicalRecords
                    .Include(m => m.Patient)
                    .Select(m => new { Id = m.Id, DisplayName = $"MR_ID: {m.Id} | Patient: {m.Patient.Name}" })
                    .ToListAsync(),
                "Id",
                "DisplayName",
                medicalRecordId
            );

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadResult(labResult labResult, IFormFile? resultFile)
        {
            if (ModelState.IsValid)
            {
                if (resultFile != null && resultFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + "-" + Path.GetFileName(resultFile.FileName);

                    
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "labs");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await resultFile.CopyToAsync(stream);
                    }

                    
                    labResult.ResultFileUrl = "/uploads/labs/" + fileName;
                }

                labResult.Date = DateTime.UtcNow;

                _context.labResults.Add(labResult);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Results));
            }

            
            ViewBag.LabTests = new SelectList(await _context.LabTests.ToListAsync(), "Id", "Name", labResult.LabTestId);
            ViewBag.MedicalRecords = new SelectList(
                await _context.MedicalRecords
                    .Include(m => m.Patient)
                    .Select(m => new { Id = m.Id, DisplayName = $"MR_ID: {m.Id} | Patient: {m.Patient.Name}" })
                    .ToListAsync(),
                "Id",
                "DisplayName",
                labResult.MedicalRecordId
            );

            return View(labResult);
        }
    }
}