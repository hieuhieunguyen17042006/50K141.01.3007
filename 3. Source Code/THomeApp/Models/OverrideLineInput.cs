namespace THomeApp.Models;

// The invoice's current composite row key is retained when selecting another product.
public sealed class OverrideLineInput
{
    public string OriginalMaSP { get; set; } = "";
    public string MaSP { get; set; } = "";
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
}
