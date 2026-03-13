using FluentValidation;

namespace Ticketing.Application.Agents.Commands.UpdateAgentProfile;

public class UpdateAgentProfileCommandValidator : AbstractValidator<UpdateAgentProfileCommand>
{
    public UpdateAgentProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.MaxConcurrentTickets)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.EfficiencyScore)
            .InclusiveBetween(0, 2);
    }
}
