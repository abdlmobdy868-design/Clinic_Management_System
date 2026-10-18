namespace ClinicManagementSystem.Models
{
    public class DepartmentVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? ClinicName { get; set; }
        public int DoctorsCount { get; set; }
    }
}
