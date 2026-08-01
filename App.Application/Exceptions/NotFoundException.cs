namespace App.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entityName, Guid id)
        : base($"'{entityName}' with Id '{id}' was not found.")
    {
    }
}
