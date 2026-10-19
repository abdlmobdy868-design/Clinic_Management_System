using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicManagementSystem.Areas.Customer.ViewModels
{
    // ---------- Home ----------
    public class DepartmentCardVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public int DoctorCount { get; set; }
    }

    public class ClinicCardVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public int DoctorCount { get; set; }
        public List<DepartmentCardVM> Departments { get; set; } = new();
    }

    public class HomeVM
    {
        public List<ClinicCardVM> Clinics { get; set; } = new();
        public List<DepartmentCardVM> Departments { get; set; } = new();
        public int TotalDoctors { get; set; }
    }

    // ---------- Doctors list ----------
    public class DoctorCardVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public string? DepartmentName { get; set; }
        public string? ClinicName { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int OpenSlots { get; set; }
        public DateTime? NextSlot { get; set; }
        public decimal? FromPrice { get; set; }
        public char Initial => string.IsNullOrWhiteSpace(Name) ? 'D' : char.ToUpper(Name.Trim()[0]);
    }

    public class DoctorListVM
    {
        // Filters (echoed back to the form)
        public string? Q { get; set; }
        public int? ClinicId { get; set; }
        public int? DepartmentId { get; set; }
        public string? Specialization { get; set; }
        public string Sort { get; set; } = "rating";
        public bool AvailableOnly { get; set; }

        // Dropdown data
        public SelectList ClinicOptions { get; set; } = default!;
        public SelectList DepartmentOptions { get; set; } = default!;
        public SelectList SpecializationOptions { get; set; } = default!;

        // Results + paging
        public List<DoctorCardVM> Doctors { get; set; } = new();
        public int TotalResults { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalResults / (double)PageSize));

        public bool HasFilters =>
            !string.IsNullOrWhiteSpace(Q) || ClinicId.HasValue || DepartmentId.HasValue ||
            !string.IsNullOrWhiteSpace(Specialization) || AvailableOnly;
    }

    // ---------- Doctor details ----------
    public class ReviewVM
    {
        public string ReviewerName { get; set; } = "Patient";
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime Date { get; set; }
    }

    public class SlotVM
    {
        public int Id { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public decimal Price { get; set; }
    }

    public class SlotDayVM
    {
        public DateTime Date { get; set; }
        public List<SlotVM> Slots { get; set; } = new();
    }

    public class DoctorDetailsVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }

        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int? ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public string? ClinicAddress { get; set; }
        public string? ClinicPhone { get; set; }

        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        // key = stars (5..1), value = number of reviews
        public Dictionary<int, int> RatingBreakdown { get; set; } = new();
        public List<ReviewVM> Reviews { get; set; } = new();

        public List<SlotDayVM> SlotDays { get; set; } = new();
        public int TotalOpenSlots { get; set; }

        public char Initial => string.IsNullOrWhiteSpace(Name) ? 'D' : char.ToUpper(Name.Trim()[0]);
    }
}
