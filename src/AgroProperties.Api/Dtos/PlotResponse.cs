namespace AgroProperties.Api.Dtos;
public record PlotResponse(Guid Id, Guid PropertyId, string Name, string Crop, string Status);