namespace Ticketing.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entity, object id)
        : base($"{entity} with ID '{id}' was not found.") { }
}
