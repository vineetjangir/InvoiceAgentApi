namespace InvoiceAgentApi.Model
{
    public class CreateInvoiceRequest
    {
        public required string Description { get; set; }
        public required decimal Amount { get; set; }
        public DateTime? Due { get; set; }
    }
}
