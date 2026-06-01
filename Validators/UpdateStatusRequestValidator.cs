using FluentValidation;
using LMSDashboard.DTOs;
using LMSDashboard.Models;

namespace LMSDashboard.Validators;

public class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => Enum.TryParse<ContentStatus>(s, true, out _))
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<ContentStatus>())}");
    }
}
