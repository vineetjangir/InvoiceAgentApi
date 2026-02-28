================================================================================
                            InvoiceAgentAI - README
================================================================================

Project Name    : InvoiceAgentAI (InvoiceAgentApi)
Framework       : .NET 8 (ASP.NET Core Web API), C# 12.0
Repository      : https://github.com/vineetjangir/InvoiceAgentApi
Branch          : master

================================================================================
1. PROJECT OVERVIEW
================================================================================

InvoiceAgentAI is an AI-powered invoicing assistant API built on .NET 8.
It acts as a conversational agent that helps users interact with an invoicing
platform called "InvoiceApp". The agent leverages large language models (LLMs)
to understand user intent and perform invoice-related actions through natural
language conversation.

Supported invoice operations:
  - Creating a new invoice
  - Searching for an invoice by name/description
  - Marking an invoice as paid
  - Listing all invoices

The agent also provides contextual help by retrieving documentation pages
for the InvoiceApp UI, guiding users on how to use the platform themselves.

The agent communicates with a backend Invoice API (http://localhost:5000)
to retrieve, create, and update invoice data.

================================================================================
2. KEY FEATURES
================================================================================

  * Multi-Provider AI Support
    ---------------------------------------------------------
    The API supports three AI providers out of the box:
      - OpenAI   (default, model: gpt-4.1-mini)
      - Google Gemini
      - Anthropic Claude
    The provider and model can be switched at startup via command-line arguments.

  * Microsoft.Extensions.AI Abstraction
    ---------------------------------------------------------
    Uses the Microsoft.Extensions.AI library (IChatClient) to provide a
    unified abstraction over multiple AI providers. This enables easy
    swapping of providers without changing application logic.

  * Chat Endpoint (POST /chat)
    ---------------------------------------------------------
    Exposes a POST endpoint at /chat that accepts a list of ChatMessage
    objects. The endpoint:
      1. Prepends the system prompt (loaded from SystemPrompt.txt) along
         with the current date/time to the conversation.
      2. Sends the full message history to the configured AI provider.
      3. Returns the AI response as JSON.

    Request body : List<ChatMessage>  (conversation history)
    Response     : ChatResponse (AI-generated reply)

  * System Prompt with Dynamic Date Injection
    ---------------------------------------------------------
    The SystemPrompt.txt file is loaded once at startup from the output
    directory. On every /chat request, the current date/time is appended
    to the system prompt so the AI agent is always aware of today's date.
    The prompt instructs the agent to:
      - Perform invoice actions using registered tools
      - Retrieve documentation pages to help users navigate the UI
      - Keep responses short and concise

  * Function Invocation Pipeline
    ---------------------------------------------------------
    The ChatClientBuilder pipeline includes:
      - Logging middleware  (UseLogging)
      - Function invocation middleware (UseFunctionInvocation)
    This allows the AI agent to call registered tools/functions automatically
    during a conversation, enabling agentic behavior.

  * Tool / Function Registry (AI Function Calling)
    ---------------------------------------------------------
    FunctionRegistry.cs registers AI tools that the agent can invoke
    during a conversation. Five tools are currently registered:

      1. list_invoices
         - Retrieves a list of all invoices in the system.
         - Backed by InvoiceApiService.GetInvoicesAsync()

      2. find_invoice_by_name
         - Finds an invoice matching the given name/description.
         - Backed by InvoiceApiService.GetInvoiceByNameAsync(string)

      3. create_invoice
         - Creates a new invoice and returns the created invoice object.
         - Backed by InvoiceApiService.CreateInvoice(CreateInvoiceRequest)
         - Accepts description, amount, and optional due date.
         - Defaults status to "Pending" and due date to 30 days from now.

      4. mark_invoice_as_paid
         - Marks the invoice with the given ID as paid.
         - Backed by InvoiceApiService.MarkAsPaid(string)

      5. get_documentation_page
         - Retrieves the content of a documentation page by name.
         - Backed by DocumentationService.GetDocumentationPage(string)
         - Available pages: getting-started, viewing-invoices,
           creating-invoices, managing-invoices

    Tools are created via AIFunctionFactory.Create() using reflection
    and are automatically injected into ChatOptions for the AI model
    to call.

  * Invoice Data Models
    ---------------------------------------------------------
    Three models in InvoiceAgentApi.Model:

    Invoice:
      - Id          (int)       - Unique identifier
      - Description (string)    - Invoice description / name
      - Amount      (decimal)   - Invoice amount
      - Date        (DateTime)  - Invoice creation date
      - Status      (string)    - e.g., "Paid", "Unpaid", "Overdue"
      - Due         (DateTime)  - Payment due date

    CreateInvoiceRequest:
      - Description (string)    - Required. Invoice description.
      - Amount      (decimal)   - Required. Invoice amount.
      - Due         (DateTime?) - Optional. Defaults to 30 days from now.

    UpdateInvoiceRequest:
      - Status      (string)    - Required. New status value.

  * Invoice API Service
    ---------------------------------------------------------
    InvoiceApiService (InvoiceAgentApi.Services) is an HTTP client
    service that communicates with the backend Invoice API at
    http://localhost:5000/api/invoices. It provides:
      - GetInvoicesAsync()                       : GET all invoices
      - GetInvoiceByNameAsync(string)            : GET invoice by description
      - CreateInvoice(CreateInvoiceRequest)      : POST create new invoice
      - MarkAsPaid(string)                       : POST update invoice status
    Uses System.Text.Json with camelCase naming policy for
    serialization/deserialization.

  * Documentation Service
    ---------------------------------------------------------
    DocumentationService (InvoiceAgentApi.Services) reads markdown
    documentation pages from the Docs/ directory at runtime. The agent
    can retrieve these pages via the get_documentation_page tool to
    provide UI guidance to users.

    Available documentation pages (Docs/*.md):
      - getting-started    : Basic overview of the platform
      - viewing-invoices   : How to view invoices in the UI
      - creating-invoices  : How to create an invoice
      - managing-invoices  : How to make changes to invoices

  * CORS Policy for Local Development
    ---------------------------------------------------------
    A CORS policy ("AllowLocalhost") is configured to allow requests from
    localhost / 127.0.0.1 origins, enabling front-end clients running
    locally to communicate with the API.

  * Environment Variable Configuration via .env
    ---------------------------------------------------------
    Uses the dotenv.net library to load environment variables from a .env
    file. API keys for each provider are read from environment variables:
      - OPENAI_API_KEY
      - GEMINI_API_KEY
      - CLAUDE_API_KEY

================================================================================
3. ARCHITECTURE
================================================================================

  ┌─────────────┐      POST /chat      ┌──────────────────┐
  │  Front-End  │ ───────────────────>  │  InvoiceAgentApi │
  │  Client     │ <───────────────────  │  (port 5001)     │
  └─────────────┘    AI Response        │                  │
                                        │  IChatClient     │
                                        │    ├ OpenAI      │
                                        │    ├ Gemini      │
                                        │    └ Claude      │
                                        │                  │
                                        │  FunctionRegistry│
                                        │    ├ list_invoices
                                        │    ├ find_invoice_by_name
                                        │    ├ create_invoice
                                        │    ├ mark_invoice_as_paid
                                        │    └ get_documentation_page
                                        └──────┬─────┬─────┘
                                               │     │
                                   HTTP calls  │     │ Local file
                                               │     │ reads
                                               v     v
                                 ┌────────────────┐ ┌──────────┐
                                 │  Invoice API   │ │  Docs/   │
                                 │  (port 5000)   │ │  *.md    │
                                 │  /api/invoices │ │  files   │
                                 └────────────────┘ └──────────┘

  Flow:
  1. Client sends conversation history to POST /chat.
  2. InvoiceAgentApi prepends system prompt and forwards to AI provider.
  3. AI provider may invoke registered tools:
     - list_invoices, find_invoice_by_name, create_invoice,
       mark_invoice_as_paid → call the backend Invoice API via HTTP.
     - get_documentation_page → reads a local markdown file from Docs/.
  4. Tool results are fed back to the AI to compose a final response.
  5. The response is returned to the client.

================================================================================
4. PROJECT STRUCTURE
================================================================================

  InvoiceAgentApi/
  |
  |-- Program.cs                  Main entry point. Configures CORS, Kestrel
  |                               (port 5001), loads .env, parses CLI args
  |                               for --provider and --model, calls
  |                               Startup.ConfigureServices, loads the system
  |                               prompt, builds the app, and registers the
  |                               POST /chat endpoint.
  |
  |-- Startup.cs                  Registers AI services (IChatClient) with DI.
  |                               Configures logging, builds the chat client
  |                               pipeline with logging and function invocation
  |                               middleware. Registers ChatOptions with tools.
  |                               Registers InvoiceApiService and
  |                               DocumentationService as singletons.
  |
  |-- FunctionRegistry.cs         Static class exposing GetTools() to register
  |                               AI tools backed by InvoiceApiService and
  |                               DocumentationService methods. Registers:
  |                               list_invoices, find_invoice_by_name,
  |                               create_invoice, mark_invoice_as_paid, and
  |                               get_documentation_page.
  |
  |-- Model/
  |   |-- Invoice.cs              Data model representing an invoice entity
  |                               with Id, Description, Amount, Date, Status,
  |                               and Due properties.
  |   |-- CreateInvoiceRequest.cs Request model for creating a new invoice.
  |                               Contains Description, Amount, and optional
  |                               Due date.
  |   |-- UpdateInvoiceRequest.cs Request model for updating invoice status.
  |                               Contains Status field.
  |
  |-- Services/
  |   |-- InvoiceApiService.cs    HTTP client service that communicates with
  |                               the backend Invoice API at localhost:5000.
  |                               Provides GetInvoicesAsync(),
  |                               GetInvoiceByNameAsync(string),
  |                               CreateInvoice(CreateInvoiceRequest), and
  |                               MarkAsPaid(string) methods.
  |   |-- DocumentationService.cs Service that reads markdown documentation
  |                               pages from the Docs/ output directory.
  |                               Provides GetDocumentationPage(string).
  |
  |-- Docs/
  |   |-- getting-started.md      Basic overview of the InvoiceApp platform
  |   |-- viewing-invoices.md     Instructions on viewing invoices in the UI
  |   |-- creating-invoices.md    Instructions on creating invoices
  |   |-- managing-invoices.md    Instructions on managing/editing invoices
  |
  |-- SystemPrompt.txt            Plain-text system prompt loaded at runtime
  |                               to define the agent's instructions and role.
  |                               Copied to output directory on build.
  |
  |-- Properties/
  |   |-- launchSettings.json     Launch profiles for Development (HTTP, HTTPS,
  |                               IIS Express).
  |
  |-- InvoiceAgentApi.csproj      Project file targeting net8.0. Includes
  |                               Docs/**/*.md files for copy to output.
  |
  |-- .env                        (User-created) Environment variables file
  |                               containing API keys. Not committed to source.
  |
  |-- ReadMe.txt                  This file.

================================================================================
5. NUGET PACKAGES
================================================================================

  Package                                Version     Purpose
  -------------------------------------  ----------  ---------------------------
  Microsoft.Extensions.AI                10.3.0      AI abstraction layer
  Microsoft.Extensions.AI.OpenAI         10.3.0      OpenAI integration
  Anthropic.SDK                          5.10.0      Anthropic Claude integration
  GeminiDotnet.Extensions.AI             0.22.0      Google Gemini integration
  dotenv.net                             4.0.1       .env file loading

================================================================================
6. PREREQUISITES
================================================================================

  - .NET 8 SDK
  - API key for at least one supported AI provider
  - A .env file in the project root with the following keys:

      OPENAI_API_KEY=your-openai-key
      GEMINI_API_KEY=your-gemini-key
      CLAUDE_API_KEY=your-claude-key

  - Backend Invoice API running at http://localhost:5000
    (required for the AI tools to retrieve and modify invoice data)

================================================================================
7. HOW TO RUN
================================================================================

  1. Start the backend Invoice API on port 5000.

  2. Run InvoiceAgentApi:

     a) Default (OpenAI with gpt-4.1-mini):
        -----------------------------------
        dotnet run --project InvoiceAgentApi

     b) Specify a provider and model via CLI:
        -----------------------------------
        dotnet run --project InvoiceAgentApi -- --provider gemini --model gemini-pro
        dotnet run --project InvoiceAgentApi -- --provider claude --model claude-3-sonnet
        dotnet run --project InvoiceAgentApi -- --provider openai --model gpt-4o

  The API listens on port 5001 (all interfaces) by default.

================================================================================
8. API ENDPOINTS
================================================================================

  POST /chat
  -----------
  Description : Send a conversation to the AI agent and receive a response.
  Request Body: JSON array of ChatMessage objects (conversation history).
  Behavior    :
      1. The system prompt from SystemPrompt.txt is prepended as the first
         message with the current date/time appended.
      2. The full message list is sent to the configured AI provider.
      3. The AI may invoke registered tools (list_invoices,
         find_invoice_by_name, create_invoice, mark_invoice_as_paid,
         get_documentation_page) to perform actions or fetch data.
      4. The AI response is returned as a 200 OK JSON result.
  Examples    :
      POST http://localhost:5001/chat
      Content-Type: application/json

      [
        { "role": "user", "content": "Show me all invoices" }
      ]

      [
        { "role": "user", "content": "Create an invoice for Web Design, $500" }
      ]

      [
        { "role": "user", "content": "How do I create an invoice in the UI?" }
      ]

================================================================================
9. REGISTERED AI TOOLS
================================================================================

  Tool Name                Method                              Description
  -----------------------  ----------------------------------  ----------------------
  list_invoices            InvoiceApiService                   Retrieves all invoices
                           .GetInvoicesAsync()

  find_invoice_by_name     InvoiceApiService                   Finds invoice by name
                           .GetInvoiceByNameAsync(string)

  create_invoice           InvoiceApiService                   Creates a new invoice
                           .CreateInvoice(CreateInvoiceRequest)

  mark_invoice_as_paid     InvoiceApiService                   Marks invoice as paid
                           .MarkAsPaid(string)

  get_documentation_page   DocumentationService                Retrieves a docs page
                           .GetDocumentationPage(string)       by name from Docs/

  These tools are resolved from FunctionRegistry.GetTools() and injected
  into ChatOptions. The AI model can call them automatically during a
  conversation when it determines the user's intent matches a tool.

================================================================================
10. CONFIGURATION DETAILS
================================================================================

  * Kestrel is configured to listen on 0.0.0.0:5001.
  * Backend Invoice API is expected at http://localhost:5000/api/invoices.
  * CORS allows any header and method from localhost origins.
  * ChatOptions are configured with:
      - Temperature : 1
      - MaxOutputTokens : 5000
  * Logging is set to Information level with Console output.
  * System prompt is loaded once from SystemPrompt.txt at app startup.
  * InvoiceApiService is registered as a singleton in DI.
  * DocumentationService is registered as a singleton in DI.
  * Docs/**/*.md files are copied to output directory on build.

================================================================================
11. EXTENDING THE AGENT WITH TOOLS
================================================================================

  To add new tools the agent can invoke during conversation:

  1. Add a new method to InvoiceApiService, DocumentationService,
     or create a new service.
  2. Register the service in Startup.ConfigureServices if new.
  3. Open FunctionRegistry.cs and yield return a new AITool:

       yield return AIFunctionFactory.Create(
           typeof(MyService).GetMethod(nameof(MyService.MyMethod),
           [typeof(ParamType)])!,
           myServiceInstance,
           new AIFunctionFactoryOptions
           {
               Name = "tool_name",
               Description = "What this tool does"
           });

  4. The tool is automatically passed to ChatOptions and made available
     to the AI model for function calling.

================================================================================
12. CHANGELOG
================================================================================

  [Update 4 - Create/Update Invoice, Documentation Service & Pages]
  - Added Model/CreateInvoiceRequest.cs:
      * Request model with Description (required), Amount (required),
        and Due (optional, defaults to 30 days from now)
  - Added Model/UpdateInvoiceRequest.cs:
      * Request model with Status field (e.g., "Paid", "Unpaid", "Overdue")
  - Updated Model/Invoice.cs:
      * Moved namespace from InvoiceAppAI.Model to InvoiceAgentApi.Model
  - Updated Services/InvoiceApiService.cs:
      * Added CreateInvoice(CreateInvoiceRequest) method:
        - POSTs a new invoice to the backend API
        - Sets default status to "Pending"
        - Defaults Due date to 30 days from now if not provided
      * Added MarkAsPaid(string invoiceId) method:
        - POSTs status update to /api/invoices/{id}/status
        - Sets status to "Paid" via UpdateInvoiceRequest
  - Added Services/DocumentationService.cs:
      * Reads markdown files from Docs/ directory in output folder
      * GetDocumentationPage(string pageName) returns file content
      * Uses Path.GetFileName() for safe file path handling
  - Added Docs/ folder with markdown documentation pages:
      * getting-started.md, viewing-invoices.md, creating-invoices.md,
        managing-invoices.md
  - Updated FunctionRegistry.cs:
      * Resolves DocumentationService from DI
      * Registers "get_documentation_page" tool backed by
        DocumentationService.GetDocumentationPage(string)
      * Registers "create_invoice" tool backed by
        InvoiceApiService.CreateInvoice(CreateInvoiceRequest)
      * Registers "mark_invoice_as_paid" tool backed by
        InvoiceApiService.MarkAsPaid(string)
      * Total registered tools: 5 (was 2)
  - Updated Startup.cs:
      * Registered DocumentationService as singleton in DI
  - Updated SystemPrompt.txt:
      * Changed agent role to "controls an invoicing platform"
      * Added documentation page retrieval instructions
      * Listed four available documentation pages
  - Updated InvoiceAgentApi.csproj:
      * Added Docs/**/*.md glob for copy to output directory

  [Update 3 - Invoice Model, Service & Tool Registration]
  - Added Model/Invoice.cs with Invoice data model containing:
      * Id, Description, Amount, Date, Status, Due properties
      * Uses "required" modifier for mandatory fields
  - Added Services/InvoiceApiService.cs:
      * HTTP client service targeting http://localhost:5000/api/invoices
      * GetInvoicesAsync() - retrieves all invoices
      * GetInvoiceByNameAsync(string) - finds invoice by description
      * Uses System.Text.Json with camelCase naming policy
  - Updated FunctionRegistry.cs:
      * Resolves InvoiceApiService from DI
      * Registers "list_invoices" tool backed by GetInvoicesAsync()
      * Registers "find_invoice_by_name" tool backed by
        GetInvoiceByNameAsync(string)
      * Tools created via AIFunctionFactory.Create() with reflection
  - Updated Startup.cs:
      * Registered InvoiceApiService as singleton in DI
      * Added using directive: InvoiceAgentApi.Services
  - Updated SystemPrompt.txt:
      * Added instruction for agent to use available tools to read invoices

  [Update 2 - Program.cs]
  - Wired up Startup.ConfigureServices call with provider and model args
  - Added system prompt loading from SystemPrompt.txt at startup
  - Registered the POST /chat endpoint with the following behavior:
      * Injects current date/time into the system prompt on each request
      * Prepends the system prompt as a System ChatMessage
      * Concatenates user-supplied conversation history
      * Calls chatClient.GetResponseAsync with registered ChatOptions
      * Returns the AI response as 200 OK
  - Added app.UseCors("AllowLocalhost") to the middleware pipeline
  - Added using directives: InvoiceAgentApi, Microsoft.Extensions.AI

  [Initial Release]
  - Set up .NET 8 ASP.NET Core Web API project
  - Integrated multi-provider AI support (OpenAI, Gemini, Claude)
  - Implemented Microsoft.Extensions.AI chat client abstraction
  - Added ChatClientBuilder pipeline with logging and function invocation
  - Created FunctionRegistry for extensible tool registration
  - Configured CORS policy for local development
  - Added .env support via dotenv.net for API key management
  - Added SystemPrompt.txt defining agent behavior for InvoiceApp
  - Configured Kestrel to listen on port 5001
  - Added CLI argument parsing for --provider and --model selection

================================================================================