using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Utilities;
using ClinicManagementSystem.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{CD.ADMIN_ROLE} ,{CD.SUPER_ADMIN_ROLE}")]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AppointmentsController(ApplicationDbContext context) => _context = context;

        // ---------------------------------------------------------------- list
        public async Task<IActionResult> Index(string? view, AppointmentStatus? status, int? doctorId, DateTime? date)
        {
            var query = _context.Appointments.AsNoTracking()
                .Include(a => a.Doctor).Include(a => a.Patient).Include(a => a.Clinic)
                .AsQueryable();

            if (view == "free") query = query.Where(a => a.PatientId == null && a.Status != AppointmentStatus.Cancelled);
            else if (view == "booked") query = query.Where(a => a.PatientId != null);

            if (status.HasValue) query = query.Where(a => a.Status == status.Value);
            if (doctorId.HasValue) query = query.Where(a => a.DoctorId == doctorId.Value);
            if (date.HasValue)
            {
                var day = date.Value.Date;
                query = query.Where(a => a.StartTime >= day && a.StartTime < day.AddDays(1));
            }

            var list = await query.OrderBy(a => a.StartTime).Take(300).ToListAsync();
            var ids = list.Select(a => a.Id).ToList();

            var paidIds = await _context.Payments.AsNoTracking()
                .Where(p => p.AppointmentId != null && ids.Contains(p.AppointmentId.Value) && p.PaymentStatus == PaymentStatus.Paid)
                .Select(p => p.AppointmentId!.Value).Distinct().ToListAsync();

            var recordIds = await _context.MedicalRecords.AsNoTracking()
                .Where(m => m.AppointmentId != null && ids.Contains(m.AppointmentId.Value))
                .Select(m => m.AppointmentId!.Value).Distinct().ToListAsync();

            var all = _context.Appointments.AsNoTracking();
            var vm = new AppointmentsIndexVM
            {
                View = view,
                Status = status,
                DoctorId = doctorId,
                Date = date,
                FreeCount = await all.CountAsync(a => a.PatientId == null && a.Status != AppointmentStatus.Cancelled),
                PendingCount = await all.CountAsync(a => a.PatientId != null && a.Status == AppointmentStatus.Pending),
                ConfirmedCount = await all.CountAsync(a => a.Status == AppointmentStatus.Confirmed),
                CompletedCount = await all.CountAsync(a => a.Status == AppointmentStatus.Completed),
                CancelledCount = await all.CountAsync(a => a.Status == AppointmentStatus.Cancelled),
                Doctors = await DoctorItemsAsync(doctorId),
                Rows = list.Select(a => new AppointmentRowVM
                {
                    Id = a.Id,
                    DoctorId = a.DoctorId,
                    DoctorName = a.Doctor?.Name ?? "-",
                    PatientId = a.PatientId,
                    PatientName = a.Patient?.Name,
                    ClinicName = a.Clinic?.Name,
                    Start = a.StartTime,
                    End = a.EndTime,
                    Price = a.price,
                    Status = a.Status,
                    IsFree = a.PatientId == null && a.Status != AppointmentStatus.Cancelled,
                    IsPaid = paidIds.Contains(a.Id),
                    HasRecord = recordIds.Contains(a.Id)
                }).ToList()
            };

            return View(vm);
        }

        // ------------------------------------------------- create free slots
        [HttpGet]
        public async Task<IActionResult> Create(int? doctorId)
        {
            var vm = new SlotGeneratorVM { DoctorId = doctorId };
            await FillSlotListsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SlotGeneratorVM vm)
        {
            if (!TimeSpan.TryParse(vm.FromTime, out var fromTime)) ModelState.AddModelError(nameof(vm.FromTime), "Invalid start time");
            if (!TimeSpan.TryParse(vm.ToTime, out var toTime)) ModelState.AddModelError(nameof(vm.ToTime), "Invalid end time");
            if (ModelState.IsValid && toTime <= fromTime)
                ModelState.AddModelError(nameof(vm.ToTime), "End time must be after start time");

            var doctor = vm.DoctorId.HasValue
                ? await _context.Doctors.Include(d => d.Department).FirstOrDefaultAsync(d => d.Id == vm.DoctorId.Value)
                : null;
            if (vm.DoctorId.HasValue && doctor == null)
                ModelState.AddModelError(nameof(vm.DoctorId), "The selected doctor does not exist");

            var clinicId = vm.ClinicId ?? doctor?.Department?.ClinicId;
            if (ModelState.IsValid && clinicId == null)
                ModelState.AddModelError(nameof(vm.ClinicId), "Select a clinic (this doctor's department has none)");

            if (!ModelState.IsValid)
            {
                await FillSlotListsAsync(vm);
                return View(vm);
            }

            var firstDay = vm.StartDate.Date;
            var lastDay = firstDay.AddDays(vm.Days);

            // existing (not cancelled) appointments of this doctor in the range, to avoid overlaps
            var existing = await _context.Appointments.AsNoTracking()
                .Where(a => a.DoctorId == doctor!.Id
                         && a.Status != AppointmentStatus.Cancelled
                         && a.StartTime < lastDay.AddDays(1)
                         && a.EndTime > firstDay)
                .Select(a => new { a.StartTime, a.EndTime })
                .ToListAsync();

            int created = 0, skipped = 0;
            var step = TimeSpan.FromMinutes(vm.DurationMinutes);
            var slots = new List<Appointment>();

            for (var day = firstDay; day < lastDay; day = day.AddDays(1))
            {
                for (var t = fromTime; t + step <= toTime; t += step)
                {
                    var start = day + t;
                    var end = start + step;

                    var overlaps = existing.Any(e => e.StartTime < end && e.EndTime > start);
                    if (overlaps) { skipped++; continue; }

                    slots.Add(new Appointment
                    {
                        DoctorId = doctor!.Id,
                        ClinicId = clinicId,
                        AppointmentDateTime = start,
                        StartTime = start,
                        EndTime = end,
                        IsAvailable = true,
                        PatientId = null,
                        Status = AppointmentStatus.Pending,
                        price = vm.Price
                    });
                    created++;
                }
            }

            if (created > 0)
            {
                _context.Appointments.AddRange(slots);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"{created} slot(s) created" + (skipped > 0 ? $", {skipped} skipped (overlapping an existing appointment)." : ".");
            return RedirectToAction(nameof(Index), new { view = "free", doctorId = doctor!.Id });
        }

        // ---------------------------------------------------------------- edit
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var a = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();

            var vm = new AppointmentEditVM
            {
                Id = a.Id,
                DoctorId = a.DoctorId,
                ClinicId = a.ClinicId,
                PatientId = a.PatientId,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Price = a.price,
                Status = a.Status
            };
            await FillEditListsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AppointmentEditVM vm)
        {
            var a = await _context.Appointments.FirstOrDefaultAsync(x => x.Id == vm.Id);
            if (a == null) return NotFound();

            if (vm.EndTime <= vm.StartTime)
                ModelState.AddModelError(nameof(vm.EndTime), "End time must be after start time");

            if (!ModelState.IsValid)
            {
                await FillEditListsAsync(vm);
                return View(vm);
            }

            // only the fields of the form are changed; nothing else can be over-posted
            a.DoctorId = vm.DoctorId;
            a.ClinicId = vm.ClinicId;
            a.PatientId = vm.PatientId;
            a.StartTime = vm.StartTime;
            a.EndTime = vm.EndTime;
            a.AppointmentDateTime = vm.StartTime;
            a.price = vm.Price;
            a.Status = vm.Status;
            // free = nobody booked it and it is not cancelled
            a.IsAvailable = vm.PatientId == null && vm.Status != AppointmentStatus.Cancelled;

            if (vm.Status == AppointmentStatus.Cancelled) await RefundPaidAsync(a.Id);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Appointment updated.";
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------- change status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            var a = await _context.Appointments.FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();

            if (!Enum.TryParse<AppointmentStatus>(status, true, out var newStatus))
            {
                TempData["Error"] = "Unknown status.";
                return RedirectToAction(nameof(Index));
            }

            if (a.PatientId == null && newStatus != AppointmentStatus.Cancelled && newStatus != AppointmentStatus.Pending)
            {
                TempData["Error"] = "A free slot has no patient yet, so it can't be confirmed or completed.";
                return RedirectToAction(nameof(Index));
            }

            a.Status = newStatus;
            a.IsAvailable = a.PatientId == null && newStatus != AppointmentStatus.Cancelled;
            if (newStatus == AppointmentStatus.Cancelled) await RefundPaidAsync(a.Id);

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Appointment #{a.Id} is now {newStatus}.";
            return RedirectToAction(nameof(Index));
        }

        // --------------------------------------------------------------- delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var a = await _context.Appointments.FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return RedirectToAction(nameof(Index));

            var hasHistory = await _context.MedicalRecords.AnyAsync(m => m.AppointmentId == id)
                          || await _context.Payments.AnyAsync(p => p.AppointmentId == id);
            if (hasHistory)
            {
                TempData["Error"] = "This appointment has a payment or medical record. Cancel it instead of deleting it.";
                return RedirectToAction(nameof(Index));
            }

            _context.Appointments.Remove(a);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Appointment deleted.";
            return RedirectToAction(nameof(Index));
        }

        // -------------------------------------------------------------- helpers
        private async Task RefundPaidAsync(int appointmentId)
        {
            var paid = await _context.Payments
                .Where(p => p.AppointmentId == appointmentId && p.PaymentStatus == PaymentStatus.Paid)
                .ToListAsync();
            foreach (var p in paid) p.PaymentStatus = PaymentStatus.Refunded;
        }

        private async Task<List<SelectListItem>> DoctorItemsAsync(int? selected)
        {
            var docs = await _context.Doctors.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
            return docs.Select(d => new SelectListItem(d.Name ?? $"Doctor {d.Id}", d.Id.ToString(), selected == d.Id)).ToList();
        }

        private async Task<List<SelectListItem>> ClinicItemsAsync(int? selected)
        {
            var clinics = await _context.Clinics.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            return clinics.Select(c => new SelectListItem(c.Name ?? $"Clinic {c.Id}", c.Id.ToString(), selected == c.Id)).ToList();
        }

        private async Task FillSlotListsAsync(SlotGeneratorVM vm)
        {
            vm.Doctors = await DoctorItemsAsync(vm.DoctorId);
            vm.Clinics = await ClinicItemsAsync(vm.ClinicId);
        }

        private async Task FillEditListsAsync(AppointmentEditVM vm)
        {
            vm.Doctors = await DoctorItemsAsync(vm.DoctorId);
            vm.Clinics = await ClinicItemsAsync(vm.ClinicId);
            var patients = await _context.Patients.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
            vm.Patients = patients
                .Select(p => new SelectListItem(string.IsNullOrWhiteSpace(p.Name) ? $"Patient {p.Id}" : p.Name, p.Id.ToString(), vm.PatientId == p.Id))
                .ToList();
        }
    }
}
