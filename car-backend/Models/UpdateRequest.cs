namespace Models;

public record UpdateRequest<T>(Guid Id, T Data);