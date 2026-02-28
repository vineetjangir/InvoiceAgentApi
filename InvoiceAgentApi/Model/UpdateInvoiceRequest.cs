namespace InvoiceAgentApi.Model
{
    public class UpdateInvoiceRequest
    {
        public required string Status { get; set; } // e.g., "Paid", "Unpaid", "Overdue"
    }
}
