namespace API.DTOs;

public record CarModelResponse(Guid Id, Guid BrandId, string Name, int? YearStart, int? YearEnd, DateTime CreatedAt);

public record CreateCarModelRequest(Guid BrandId, string Name, int? YearStart, int? YearEnd);

public record UpdateCarModelRequest(Guid Id, Guid BrandId, string Name, int? YearStart, int? YearEnd);