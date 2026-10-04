using ClinicManagementSystem.Utilities;
using System.Reflection.Metadata.Ecma335;

namespace ClinicManagementSystem.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; }

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; }

        public int ClinicId { get; set; }
        public Clinic Clinic { get; set; }

        public DateTime AppointmentDateTime { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public bool IsAvailable { get; set; }
        public AppointmentStatus Status { get; set; }

        public decimal price { get; set; }

    }
}
