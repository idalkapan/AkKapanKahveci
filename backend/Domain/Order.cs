class Order
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Session { get; set; } = "";
    public string Customer { get; set; } = "";
    public string Concept { get; set; } = "";
    public decimal Total { get; set; }
    public decimal Discount { get; set; }
    public string ItemsJson { get; set; } = "";
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public OrderPreparationStatus PreparationStatus { get; set; } = OrderPreparationStatus.Received;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.DemoPaid;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long Version { get; set; } = 1;
}
