namespace ClinicManagementSystem.ViewModel
{
    public class DashboardVM
    {
        // Current user
        public string FullName { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;

        // Summary cards
        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalClinics { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalAppointments { get; set; }
        public int TodayAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public int TotalLabTests { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal ThisMonthRevenue { get; set; }
        public decimal PendingPayments { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }

        // Charts
        public List<string> MonthLabels { get; set; } = new();
        public List<decimal> MonthlyRevenue { get; set; } = new();
        public List<int> MonthlyAppointments { get; set; } = new();
        public List<string> StatusLabels { get; set; } = new();
        public List<int> StatusCounts { get; set; } = new();
        public int MalePatients { get; set; }
        public int FemalePatients { get; set; }

        // Tables
        public List<RecentAppointmentRow> RecentAppointments { get; set; } = new();
        public List<TopDoctorRow> TopDoctors { get; set; } = new();
    }

    public class RecentAppointmentRow
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class TopDoctorRow
    {
        public string Name { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public int Appointments { get; set; }
        public double Rating { get; set; }
    }
}
