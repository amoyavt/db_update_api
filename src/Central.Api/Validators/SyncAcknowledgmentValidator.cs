using FluentValidation;
using Shared.Models;

namespace Central.Api.Validators;

public class SyncAcknowledgmentValidator : AbstractValidator<SyncAcknowledgmentDto>
{
    public SyncAcknowledgmentValidator()
    {
        RuleFor(x => x.ManifestId)
            .NotEmpty()
            .WithMessage("ManifestId is required")
            .Length(26)
            .WithMessage("ManifestId must be a valid ULID (26 characters)");

        RuleFor(x => x.Mac)
            .NotEmpty()
            .WithMessage("MAC address is required")
            .Matches(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$")
            .WithMessage("MAC address must be in valid format");

        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required")
            .Must(status => status == SyncStatus.Success || status == SyncStatus.Failed)
            .WithMessage($"Status must be '{SyncStatus.Success}' or '{SyncStatus.Failed}'");

        RuleFor(x => x.LocalCounts)
            .NotNull()
            .WithMessage("LocalCounts is required");

        RuleFor(x => x.LocalChecksums)
            .NotNull()
            .WithMessage("LocalChecksums is required");

        RuleFor(x => x.DurationMs)
            .GreaterThanOrEqualTo(0)
            .WithMessage("DurationMs must be non-negative");
    }
}