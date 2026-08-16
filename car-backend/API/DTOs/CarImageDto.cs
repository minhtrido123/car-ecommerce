namespace API.DTOs;

public record CreateProductImageRequest(Guid ProductId, string Url, bool IsPrimary);

public record UpdateProductImageRequest(Guid Id, Guid ProductId, string Url, bool IsPrimary);
