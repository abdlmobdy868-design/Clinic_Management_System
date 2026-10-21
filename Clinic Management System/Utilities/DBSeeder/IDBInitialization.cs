using ClinicManagementSystem.Data;

namespace Clinic_Management_System.Utilities.DBSeeder
{
    public interface IDBInitialization
    {
        Task InitializationAsync();
        Task SeedAppointmentsAsync();
    }
}
