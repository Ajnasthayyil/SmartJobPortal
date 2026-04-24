namespace SmartJobPortal.Application.DTOs.Admin;

public class AdminDashboardResponse
{
    // User stats
    public int TotalUsers { get; set; }
    public int TotalCandidates { get; set; }
    public int TotalRecruiters { get; set; }
    public int PendingRecruiterApprovals { get; set; }
    public int BlockedUsers { get; set; }

    // Job stats
    public int TotalJobs { get; set; }
    public int ActiveJobs { get; set; }
    public int InactiveJobs { get; set; }

    // Application stats
    public int TotalApplications { get; set; }
    public int ApplicationsToday { get; set; }

    // Recent activity
    public List<RecentUserActivity> RecentUsers { get; set; } = new();
    public List<RecentJobActivity> RecentJobs { get; set; } = new();
}

public class RecentUserActivity
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RecentJobActivity
{
    public int JobId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int TotalApplicants { get; set; }
    public DateTime PostedAt { get; set; }
}