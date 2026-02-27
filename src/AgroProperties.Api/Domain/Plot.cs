using System.ComponentModel.DataAnnotations;

namespace AgroProperties.Api.Domain;

public class Plot
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid PropertyId { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = default!;

    [Required, MaxLength(120)]
    public string Crop { get; set; } = default!;

    [Required, MaxLength(40)]
    public string Status { get; set; } = "Normal";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public FarmProperty? Property { get; set; }
}