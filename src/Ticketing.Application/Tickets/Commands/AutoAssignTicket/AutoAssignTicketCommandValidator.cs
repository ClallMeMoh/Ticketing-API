using FluentValidation;

namespace Ticketing.Application.Tickets.Commands.AutoAssignTicket;

public class AutoAssignTicketCommandValidator : AbstractValidator<AutoAssignTicketCommand>
{
    public AutoAssignTicketCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty();

        RuleFor(x => x.Trigger)
            .NotEmpty()
            .MaximumLength(50);
    }
}
