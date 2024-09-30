namespace OrderManagement.DTOs
{
    public class AddOrderDTO
    {
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; } 
        public string Currency { get; set; } 
    }
}
