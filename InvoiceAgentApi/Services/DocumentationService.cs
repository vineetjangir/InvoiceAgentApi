namespace InvoiceAppAI.Services
{
    public class DocumentationService
    {
        private readonly string _docsDirectory;
        public DocumentationService()
        {
            _docsDirectory = Path.Combine(AppContext.BaseDirectory, "Docs");
        }

        public string? GetDocumentationPage(string pageName)
        {
            if (string.IsNullOrWhiteSpace(pageName))
                return null;
            var safePageName = Path.GetFileName(pageName);
            var filePath = Path.Combine(_docsDirectory, $"{safePageName}.md");

            if (!File.Exists(filePath))
                return null;
            return File.ReadAllText(filePath);
        }
    }
}
