namespace THomeApp.Models;
public class ReturnableLine
{
    public string MaSP { get; set; } = "";
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public int Purchased { get; set; }
    public int Remaining { get; set; }
    public decimal Price { get; set; }
    public int? SourceVoucherLineId { get; set; }
    public string? SourceVoucher { get; set; }
}
