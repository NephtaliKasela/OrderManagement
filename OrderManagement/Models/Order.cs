namespace OrderManagement.Models
{
    public class Order
    {
        public int Id { get; set; } // Order identifier
        public string CustomerName { get; set; } // Customer name
        public DateTime OrderDate { get; set; } // Order date
        public decimal TotalAmount { get; set; } // Total order amount
        public string Currency { get; set; } // Currency of the order amount (e.g., USD, EUR)
        public string Status { get; set; } // Order status: Pending, Processing, Completed, Cancelled
        public decimal Priority { get; set; } // Order processing priority
        public decimal TotalAmountInBaseCurrency { get; set; } // Total amount in base currency (e.g., USD)
    }

}
