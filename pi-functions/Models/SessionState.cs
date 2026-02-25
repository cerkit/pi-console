using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PiFunctions.Models;

public class SessionState
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string ClientId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ActiveChannel { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "PENDING";

    [Column(TypeName = "jsonb")]
    public string? CurrentUiState { get; set; } 

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}