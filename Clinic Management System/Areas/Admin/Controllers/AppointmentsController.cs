using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
[Area("Admin")]
[Authorize(Roles = $"{CD.ADMIN_ROLE} ,{CD.SUPER_ADMIN_ROLE}")]
public class AppointmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    public AppointmentsController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index() => View(await _context.Appointments.Include(a => a.Doctor).Include(a => a.Patient).ToListAsync());
    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Doctors = new SelectList(_context.Doctors.Where(d => d.Name != null).ToList(), "Id", "Name");
        ViewBag.Patients = new SelectList(_context.Users.Where(u => u.UserType == "Patient").ToList(), "Id", "FullName");
        ViewBag.Clinics = new SelectList(_context.Clinics.Where(c => c.Name != null).ToList(), "Id", "Name");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Appointment appointment)
    {
        if (appointment.DoctorId == 0 || appointment.PatientId == 0 || appointment.ClinicId == 0)
        {
            ModelState.AddModelError("", "Please select a doctor, patient, and clinic.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Doctors = new SelectList(_context.Doctors.Where(d => d.Name != null).ToList(), "Id", "Name");
            ViewBag.Patients = new SelectList(_context.Users.Where(u => u.UserType == "Patient").ToList(), "Id", "FullName");
            ViewBag.Clinics = new SelectList(_context.Clinics.Where(c => c.Name != null).ToList(), "Id", "Name");
            return View(appointment);
        }

        try
        {
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Error saving: " + ex.InnerException?.Message);
            ViewBag.Doctors = new SelectList(_context.Doctors, "Id", "Name");
            ViewBag.Patients = new SelectList(_context.Users.Where(u => u.UserType == "Patient"), "Id", "FullName");
            ViewBag.Clinics = new SelectList(_context.Clinics, "Id", "Name");
            return View(appointment);
        }
    }

    [HttpPost]
public async Task<IActionResult> ChangeStatus(int id, string status)
{
    var app = await _context.Appointments.FindAsync(id);
    if (app != null)
    {
        if (Enum.TryParse<AppointmentStatus>(status, true, out var newStatus))
        {
            app.Status = newStatus;
            await _context.SaveChangesAsync();
        }
    }
    return RedirectToAction(nameof(Index));
}




    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var app = await _context.Appointments.FindAsync(id);
        ViewBag.Doctors = new SelectList(_context.Doctors, "Id", "Name", app.DoctorId);
        ViewBag.Patients = new SelectList(_context.Patients, "Id", "Name", app.PatientId);
        return View(app);
    }
    [HttpPost]
    public async Task<IActionResult> Edit(Appointment app)
    {
        _context.Appointments.Update(app);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // DELETE
    public async Task<IActionResult> Delete(int id)
    {
        var app = await _context.Appointments.FindAsync(id);
        if (app != null)
        {
            _context.Appointments.Remove(app);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}