using Microsoft.Extensions.AI;

namespace InvoiceAgentApi
{
    public static class FunctionRegistry
    {
        public static IEnumerable<AITool> GetTools(this IServiceProvider sp)
        {
            yield break;
        }
    }
}
