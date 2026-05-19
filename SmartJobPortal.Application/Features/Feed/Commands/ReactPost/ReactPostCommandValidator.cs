using FluentValidation;

namespace SmartJobPortal.Application.Features.Feed.Commands.ReactPost;

public class ReactPostCommandValidator
    : AbstractValidator<ReactPostCommand>
{
    public ReactPostCommandValidator()
    {
        RuleFor(x => x.PostId)
            .GreaterThan(0);

        RuleFor(x => x.ReactionType)
            .NotEmpty()
            .MaximumLength(50);
    }
}