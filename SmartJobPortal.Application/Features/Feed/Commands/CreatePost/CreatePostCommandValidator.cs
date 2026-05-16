using FluentValidation;

namespace SmartJobPortal.Application.Features.Feed.Commands.CreatePost;

public class CreatePostCommandValidator
    : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(3000);
    }
}