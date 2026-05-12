using FluentValidation;

namespace SmartJobPortal.Application.Features.Job.Commands.ApplyJob;

public class ApplyJobCommandValidator : AbstractValidator<ApplyJobCommand>
{
    public ApplyJobCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request.JobId).NotEmpty().WithMessage("JobId is required.");
        RuleFor(x => x.Request.CoverNote).MaximumLength(2000).WithMessage("Cover note is too long.");
    }
}
