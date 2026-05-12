using FluentValidation;

namespace SmartJobPortal.Application.Features.Recruiter.Commands.UpdateProfile;

public class UpdateRecruiterProfileCommandValidator : AbstractValidator<UpdateRecruiterProfileCommand>
{
    public UpdateRecruiterProfileCommandValidator()
    {
        RuleFor(x => x.Request.CompanyName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Industry).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.Location).NotEmpty().MaximumLength(100);
    }
}
