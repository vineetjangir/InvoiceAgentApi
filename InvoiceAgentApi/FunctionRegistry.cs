using InvoiceAgentApi.Services;
using Microsoft.Extensions.AI;

namespace InvoiceAgentApi
{
    public static class FunctionRegistry
    {
        public static IEnumerable<AITool> GetTools(this IServiceProvider sp)
        {
            var invoiceApiService = sp.GetRequiredService<InvoiceApiService>();

            yield return AIFunctionFactory.Create(
                typeof(InvoiceApiService).GetMethod(nameof(InvoiceApiService.GetInvoicesAsync), Type.EmptyTypes)!,
                invoiceApiService,
                new AIFunctionFactoryOptions
                {
                    Name = "list_invoices",
                    Description = "Retrieves a list of all invoices in the system"
                });

            yield return AIFunctionFactory.Create(
                typeof(InvoiceApiService).GetMethod(nameof(InvoiceApiService.GetInvoiceByNameAsync), [typeof(string)])!,
                invoiceApiService,
                new AIFunctionFactoryOptions
                {
                    Name = "find_invoice_by_name",
                    Description = "Finds the invoice with this name"
                });
        }
    }
}
