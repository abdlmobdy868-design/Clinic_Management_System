using ClinicManagementSystem.Models;
using ClinicManagementSystem.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ClinicsController : Controller
    {
        private readonly IGenericRepository<Clinic> _repo;
        public ClinicsController(IGenericRepository<Clinic> repo) => _repo = repo;

        public async Task<IActionResult> Index() => View(await _repo.GetAllAsync());

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Clinic clinic)
        {
            if (ModelState.IsValid)
            {
                await _repo.AddAsync(clinic);
                await _repo.SaveAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(clinic);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var clinic = await _repo.GetByIdAsync(id);
            return View(clinic);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Clinic clinic)
        {
            _repo.Update(clinic);
            await _repo.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var clinic = await _repo.GetByIdAsync(id);
            _repo.Delete(clinic);
            await _repo.SaveAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}