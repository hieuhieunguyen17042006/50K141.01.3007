namespace THomeApp.Models;

// Temporary server-side state, never an invoice or a receipt.
public sealed record PreparedOrder(string Id, string Owner, string InputJson, HoaDon Preview, DateTimeOffset ExpiresAt);
