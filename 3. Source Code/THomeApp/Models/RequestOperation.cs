using System.ComponentModel.DataAnnotations;

namespace THomeApp.Models;

// Stored in the same transaction as the document and its audit records.
public class RequestOperation
{
    [Key, MaxLength(64)] public string Id { get; set; } = "";
    [MaxLength(64)] public string PayloadHash { get; set; } = "";
    public string? RedirectUrl { get; set; }
    public string? JsonResult { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
