using FluentValidation;

namespace Ticketing.Application.Tickets.Commands.ChangeTicketStatus;

public class ChangeTicketStatusCommandValidator : AbstractValidator<ChangeTicketStatusCommand>
{
    public ChangeTicketStatusCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.NewStatus).IsInEnum();
    }
}
