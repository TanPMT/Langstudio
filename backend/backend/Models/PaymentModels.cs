namespace backend.Models;

public class PaymentRequestModel
{
    public string OrderDescription { get; set; }
    public double Amount { get; set; } = 50000000; // Giá cố định 50,000 VND
    public string OrderType { get; set; } = "pro_subscription";
}

public class PaymentResponseModel
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string TransactionId { get; set; }
    public string Amount { get; set; }
    public string OrderDescription { get; set; }
}

public class PaymentTransaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; }
    public string TransactionId { get; set; }
    public double Amount { get; set; }
    public string OrderDescription { get; set; }
    public string Status { get; set; } // "Success" or "Failed"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}