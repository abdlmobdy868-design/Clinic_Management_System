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
                if (_context.Database.GetPendingMigrations().Any())
                {
                    _context.Database.Migrate();
                }

                if (!_roleManager.Roles.Any())
                {
                    await _roleManager.CreateAsync(new IdentityRole(CD.SUPER_ADMIN_ROLE));
                    await _roleManager.CreateAsync(new IdentityRole(CD.ADMIN_ROLE));
                    await _roleManager.CreateAsync(new IdentityRole(CD.PATIENT_ROLE));

                    await _userManager.CreateAsync(new ApplicationUser()
                    {
                        FullName = "SuperAdmin",
                        UserName = "SuperAdmin",
                        Email = "superAdmin@gmail.com",
                        Address = "Cairo"
                    }, "Asd@123");

                    var user = await _userManager.FindByNameAsync("SuperAdmin");

                    await _userManager.AddToRoleAsync(user, CD.SUPER_ADMIN_ROLE);
                }

            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }
        public async Task SeedAppointmentsAsync()
        {
            if (await _context.Appointments.AnyAsync()) return;

            var doctors = await _context.Doctors.Include(d => d.Department).ToListAsync();
            var now = DateTime.Now.Date.AddDays(1);

            var appointmentsList = new List<Appointment>();

            foreach (var doctor in doctors)
            {
                for (int day = 0; day < 5; day++)
                {
                    var currentDate = now.AddDays(day);

                    for (int hour = 10; hour < 14; hour += 1)
                    {
                        appointmentsList.Add(new Appointment
                        {
                            DoctorId = doctor.Id,
                            ClinicId = doctor.Department?.ClinicId ?? 1,
                            StartTime = currentDate.AddHours(hour),
                            EndTime = currentDate.AddHours(hour).AddMinutes(45),
                            IsAvailable = true,
                            PatientId = null,
                            Status = AppointmentStatus.Pending,
                            price = 250
                        });
                    }
                }
            }

            await _context.Appointments.AddRangeAsync(appointmentsList);
            await _context.SaveChangesAsync();
        }

    }
}
