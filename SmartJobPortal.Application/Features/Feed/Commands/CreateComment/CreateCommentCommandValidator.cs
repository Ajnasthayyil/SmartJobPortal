using FluentValidation;

namespace SmartJobPortal.Application.Features.Feed.Commands.CreateComment;

public class CreateCommentCommandValidator
    : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.PostId)
            .GreaterThan(0);
    }
}