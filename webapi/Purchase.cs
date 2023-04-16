namespace webapi;

public class Purchase
{
    public Guid PurchaseId { get; set; }
    
    public DateTime Date { get; set; }

    public Decimal Total { get; set; }

    public string Cart { get; set; }

    public string Name { get; set; }

    public string Ccnum { get; set; }
}
