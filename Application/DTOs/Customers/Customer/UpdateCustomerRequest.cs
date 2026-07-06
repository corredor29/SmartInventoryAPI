namespace Application.DTOs.Customers.Customer
{
    public class UpdateCustomerRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? DocumentNumber { get; set; }
    }
}