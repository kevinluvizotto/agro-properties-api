using System.ComponentModel.DataAnnotations;

namespace AgroProperties.Api.Domain;

public class FarmProperty
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProducerId { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = default!;

    [MaxLength(250)]
    public string? Location { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<Plot> Plots { get; set; } = new();
}