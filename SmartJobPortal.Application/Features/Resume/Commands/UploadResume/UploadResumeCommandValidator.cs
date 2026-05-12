using FluentValidation;

namespace SmartJobPortal.Application.Features.Resume.Commands.UploadResume;

public class UploadResumeCommandValidator : AbstractValidator<UploadResumeCommand>
{
    public UploadResumeCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.File).NotNull().WithMessage("File is required.");
        RuleFor(x => x.File.Length).LessThanOrEqualTo(5_242_880).WithMessage("File size must be less than 5MB.");
        RuleFor(x => x.File.ContentType).Must(x => x == "application/pdf" || 
                                                  x == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            .WithMessage("Only PDF and DOCX files are allowed.");
    }
}
