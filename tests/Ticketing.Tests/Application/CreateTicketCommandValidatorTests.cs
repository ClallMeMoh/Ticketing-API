using Ticketing.Application.Tickets.Commands.CreateTicket;
using Ticketing.Domain.Enums;

namespace Ticketing.Tests.Application;

public class CreateTicketCommandValidatorTests
{
    private readonly CreateTicketCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new CreateTicketCommand("Title", "Description", TicketPriority.Medium);
        var result = _validator.Validate(command);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData("Title", "")]
    [InlineData("", "")]
    public void Validate_EmptyFields_IsInvalid(string title, string description)
    {
        var command = new CreateTicketCommand(title, description, TicketPriority.Low);
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_TitleTooLong_IsInvalid()
    {
        var command = new CreateTicketCommand(new string('x', 301), "Desc", TicketPriority.Low);
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_DescriptionTooLong_IsInvalid()
    {
        var command = new CreateTicketCommand("Title", new string('x', 4001), TicketPriority.Low);
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
    }
}
