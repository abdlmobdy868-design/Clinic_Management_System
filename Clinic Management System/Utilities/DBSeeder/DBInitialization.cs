using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Clinic_Management_System.Utilities.DBSeeder
{
    public class DBInitialization : IDBInitialization
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DBInitialization> _logger;

        public DBInitialization(ApplicationDbContext context, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, ILogger<DBInitialization> logger)
        {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task InitializationAsync()
        {
            try
            {
                if ((await _context.Database.GetPendingMigrationsAsync()).Any())
                {
                    await _context.Database.MigrateAsync();
                }

                // Make sure every role exists (also fixes databases that were seeded before Doctor was added)
                string[] roles =
                {
                    CD.SUPER_ADMIN_ROLE,
                    CD.ADMIN_ROLE,
                    CD.DOCTOR_ROLE,
                    CD.PATIENT_ROLE
                };

                foreach (var role in roles)
                {
                    if (!await _roleManager.RoleExistsAsync(role))
                    {
                        var created = await _roleManager.CreateAsync(new IdentityRole(role));
                        if (!created.Succeeded)
                        {
                            _logger.LogError("Could not create role {Role}: {Errors}", role,
                                string.Join("; ", created.Errors.Select(e => e.Description)));
                        }
                    }
                }

                var superAdmin = await _userManager.FindByNameAsync("SuperAdmin");

                if (superAdmin is null)
                {
                    superAdmin = new ApplicationUser()
                    {
                        FullName = "SuperAdmin",
                        UserName = "SuperAdmin",
                        Email = "superAdmin@gmail.com",
                        // RequireConfirmedAccount is on, so without this the SuperAdmin can never log in
                        EmailConfirmed = true,
                        Address = "Cairo"
                    };

                    var result = await _userManager.CreateAsync(superAdmin, "Asd@123");
                    if (!result.Succeeded)
                    {
                        _logger.LogError("Could not create SuperAdmin: {Errors}",
                            string.Join("; ", result.Errors.Select(e => e.Description)));
                        return;
                    }
                }
                else if (!superAdmin.EmailConfirmed)
                {
                    // SuperAdmin already exists in the database from the old seeder
                    superAdmin.EmailConfirmed = true;
                    await _userManager.UpdateAsync(superAdmin);
                }

                if (!await _userManager.IsInRoleAsync(superAdmin, CD.SUPER_ADMIN_ROLE))
                {
                    await _userManager.AddToRoleAsync(superAdmin, CD.SUPER_ADMIN_ROLE);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database initialization failed");
            }
        }

        // Adds free slots (next 5 days, 10:00-13:00) for doctors that have no upcoming appointment.
        // Safe to run on every start: it never deletes or changes existing appointments or payments.
        public async Task SeedAppointmentsAsync()
        {
            try
            {
                var now = DateTime.Now;
                var firstDay = now.Date.AddDays(1);

                var doctors = await _context.Doctors.Include(d => d.Department).ToListAsync();
                var busyDoctorIds = await _context.Appointments
                    .Where(a => a.DoctorId != null && a.StartTime >= now)
                    .Select(a => a.DoctorId!.Value)
                    .Distinct()
                    .ToListAsync();

                var slots = new List<Appointment>();
                foreach (var doctor in doctors.Where(d => !busyDoctorIds.Contains(d.Id)))
                {
                    for (int day = 0; day < 5; day++)
                    {
                        for (int hour = 10; hour < 14; hour++)
                        {
                            var start = firstDay.AddDays(day).AddHours(hour);
                            slots.Add(new Appointment
                            {
                                DoctorId = doctor.Id,
                                ClinicId = doctor.Department?.ClinicId,
                                AppointmentDateTime = start,
                                StartTime = start,
                                EndTime = start.AddMinutes(45),
                                IsAvailable = true,
                                PatientId = null,
                                Status = AppointmentStatus.Pending,
                                price = 250
                            });
                        }
                    }
                }

                if (slots.Count > 0)
                {
                    await _context.Appointments.AddRangeAsync(slots);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Seeding appointments failed");
            }
        }
    }
}
