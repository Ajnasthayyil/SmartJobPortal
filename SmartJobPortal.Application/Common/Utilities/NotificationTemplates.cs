namespace SmartJobPortal.Application.Common.Utilities;

public static class NotificationTemplates
{
    // ── Application status changes ────────────────────────────────
    public static (string title, string message, string type)
        ApplicationStatusChanged(
            string status, string jobTitle, string companyName)
    {
        return status switch
        {
            "UnderReview" => (
                "Application Under Review",
                $"Your application for {jobTitle} at {companyName} " +
                $"is now being reviewed.",
                "StatusUpdate"),

            "Shortlisted" => (
                "🎉 Shortlisted!",
                $"You are shortlisted for the {jobTitle} role at {companyName}. Thank you.",
                "Shortlisted"),

            "Interview" => (
                "📅 Interview Scheduled",
                $"Your interview for {jobTitle} at {companyName} has been scheduled. Thank you.",
                "Interview"),

            "Offered" => (
                "🎊 Job Offer!",
                $"Congratulations! You are offered the {jobTitle} position at {companyName}. Thank you.",
                "Offer"),

            "Rejected" => (
                "Application Update",
                $"You are rejected for the {jobTitle} position at {companyName}. Thank you.",
                "Rejected"),

            _ => (
                "Application Update",
                $"Your status for {jobTitle} at {companyName} was updated. Thank you.",
                "StatusUpdate")
        };
    }

    //  Admin actions
    public static (string title, string message, string type)
        RecruiterApproved(string fullName)
    => (
        "✅ Account Approved!",
        $"Welcome {fullName}! Your recruiter account has been approved. " +
        $"You can now post jobs and find great candidates.",
        "AccountApproved");

    public static (string title, string message, string type)
        RecruiterRejected()
    => (
        "Account Application Update",
        "Your recruiter account application was reviewed and was not " +
        "approved at this time. Contact support for more information.",
        "AccountRejected");

    public static (string title, string message, string type)
        AccountBlocked()
    => (
        "Account Suspended",
        "Your account has been suspended. Please contact support " +
        "if you believe this is an error.",
        "AccountBlocked");

    public static (string title, string message, string type)
        AccountUnblocked()
    => (
        "Account Reactivated",
        "Your account has been reactivated. You can now log in and " +
        "continue using Talex.",
        "AccountActive");

    //  Job match 
    public static (string title, string message, string type)
        NewJobMatch(string jobTitle, string companyName, int matchScore)
    => (
        "🎯 New Job Match Found",
        $"{matchScore}% match: {jobTitle} at {companyName} was just " +
        $"posted and matches your profile.",
        "JobMatch");
}