using FluentValidation;

namespace SmartJobPortal.Application.Features.Feed.Commands.EditComment;

public class EditCommentCommandValidator
    : AbstractValidator<EditCommentCommand>
{
    public EditCommentCommandValidator()
    {
        RuleFor(x => x.CommentId)
            .GreaterThan(0);

        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(1000);
    }
}