using FluentValidation;

namespace Ticketing.Application.Agents.Commands.CreateAgentProfile;

public class CreateAgentProfileCommandValidator : AbstractValidator<CreateAgentProfileCommand>
{
    public CreateAgentProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.MaxConcurrentTickets)
            .GreaterThanOrEqualTo(1);
    }
}
