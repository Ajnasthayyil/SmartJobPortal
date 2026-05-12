using FluentValidation;

namespace SmartJobPortal.Application.Features.Candidate.Commands.UpdateCandidateProfile;

public class UpdateCandidateProfileCommandValidator : AbstractValidator<UpdateCandidateProfileCommand>
{
    public UpdateCandidateProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request.Headline).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Summary).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Request.Location).NotEmpty().MaximumLength(100);
    }
}
