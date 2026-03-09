using MediatR;
using Ticketing.Application.Exceptions;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Repositories;

namespace Ticketing.Application.Comments.Commands.AddComment;

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, Guid>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AddCommentCommandHandler(
        ITicketRepository ticketRepository,
        ICommentRepository commentRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _commentRepository = commentRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        var comment = new Comment(ticket.Id, _currentUser.UserId, request.Content);

        await _commentRepository.AddAsync(comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return comment.Id;
    }
}
