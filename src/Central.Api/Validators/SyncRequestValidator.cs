using FluentValidation;
using Shared.Models;

namespace Central.Api.Validators;

public class SyncRequestValidator : AbstractValidator<SyncRequestDto>
{
    public SyncRequestValidator()
    {
        RuleFor(x => x.Mac)
            .NotEmpty()
            .WithMessage("MAC address is required")
            .Matches(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$")
            .WithMessage("MAC address must be in valid format (e.g., 48:b0:2d:e9:c3:b7)");
    }
}