namespace THomeApp.Models;
public class DailyRevenue { public DateTime Day { get; set; } public decimal Cash { get; set; } public decimal Transfer { get; set; } }
public class ProductSummary { public string Name { get; set; } = ""; public int Quantity { get; set; } public decimal Revenue { get; set; } }
