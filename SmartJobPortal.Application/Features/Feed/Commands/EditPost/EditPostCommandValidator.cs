using FluentValidation;

namespace SmartJobPortal.Application.Features.Feed.Commands.EditPost;

public class EditPostCommandValidator
    : AbstractValidator<EditPostCommand>
{
    public EditPostCommandValidator()
    {
        RuleFor(x => x.PostId)
            .GreaterThan(0);

        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(3000);
    }
}