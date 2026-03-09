using FluentValidation;

namespace Ticketing.Application.Tickets.Commands.DeleteTicket;

public class DeleteTicketCommandValidator : AbstractValidator<DeleteTicketCommand>
{
    public DeleteTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
    }
}
