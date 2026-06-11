using FluentValidation;

namespace OrigamiPlatform.Application.Commands.Community;

public class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.Request.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(1000).WithMessage("Content must not exceed 1000 characters.");

        RuleFor(x => x.Request.MediaUrls)
            .Must(x => x == null || x.Count <= 10).WithMessage("Cannot attach more than 10 media files.");
    }
}