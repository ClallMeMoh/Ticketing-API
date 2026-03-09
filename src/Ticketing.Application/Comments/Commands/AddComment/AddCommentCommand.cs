using MediatR;

namespace Ticketing.Application.Comments.Commands.AddComment;

public record AddCommentCommand(Guid TicketId, string Content) : IRequest<Guid>;
