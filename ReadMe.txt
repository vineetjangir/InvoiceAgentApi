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

The agent communicates with a backend Invoice API (http://localhost:5000)
to retrieve and query invoice data.

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
    The prompt also instructs the agent to use available tools to read
    invoice data.

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
    during a conversation. Two tools are currently registered:

      1. list_invoices
         - Retrieves a list of all invoices in the system.
         - Backed by InvoiceApiService.GetInvoicesAsync()

      2. find_invoice_by_name
         - Finds an invoice matching the given name/description.
         - Backed by InvoiceApiService.GetInvoiceByNameAsync(string)

    Tools are created via AIFunctionFactory.Create() using reflection
    on InvoiceApiService methods and are automatically injected into
    ChatOptions for the AI model to call.

  * Invoice Data Model
    ---------------------------------------------------------
    The Invoice model (InvoiceAppAI.Model.Invoice) represents an
    invoice entity with the following properties:
      - Id          (int)       - Unique identifier
      - Description (string)    - Invoice description / name
      - Amount      (decimal)   - Invoice amount
      - Date        (DateTime)  - Invoice creation date
      - Status      (string)    - e.g., "Paid", "Unpaid", "Overdue"
      - Due         (DateTime)  - Payment due date

  * Invoice API Service
    ---------------------------------------------------------
    InvoiceApiService (InvoiceAgentApi.Services) is an HTTP client
    service that communicates with the backend Invoice API at
    http://localhost:5000/api/invoices. It provides:
      - GetInvoicesAsync()              : GET all invoices
      - GetInvoiceByNameAsync(string)   : GET invoice by description
    Uses System.Text.Json with camelCase naming policy for
    serialization/deserialization.

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
                                        │    └ find_invoice_by_name
                                        └───────┬──────────┘
                                                │ HTTP
                                                v
                                        ┌──────────────────┐
                                        │  Invoice API     │
                                        │  (port 5000)     │
                                        │  /api/invoices   │
                                        └──────────────────┘

  Flow:
  1. Client sends conversation history to POST /chat.
  2. InvoiceAgentApi prepends system prompt and forwards to AI provider.
  3. AI provider may invoke registered tools (list_invoices,
     find_invoice_by_name) which call the backend Invoice API.
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
  |                               Registers InvoiceApiService as a singleton.
  |
  |-- FunctionRegistry.cs         Static class exposing GetTools() to register
  |                               AI tools backed by InvoiceApiService methods.
  |                               Currently registers: list_invoices and
  |                               find_invoice_by_name.
  |
  |-- Model/
  |   |-- Invoice.cs              Data model representing an invoice entity
  |                               with Id, Description, Amount, Date, Status,
  |                               and Due properties.
  |
  |-- Services/
  |   |-- InvoiceApiService.cs    HTTP client service that communicates with
  |                               the backend Invoice API at localhost:5000.
  |                               Provides GetInvoicesAsync() and
  |                               GetInvoiceByNameAsync(string) methods.
  |
  |-- SystemPrompt.txt            Plain-text system prompt loaded at runtime
  |                               to define the agent's instructions and role.
  |                               Copied to output directory on build.
  |
  |-- Properties/
  |   |-- launchSettings.json     Launch profiles for Development (HTTP, HTTPS,
  |                               IIS Express).
  |
  |-- InvoiceAgentApi.csproj      Project file targeting net8.0.
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
    (required for the AI tools to retrieve invoice data)

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
         find_invoice_by_name) to fetch data from the backend Invoice API.
      4. The AI response is returned as a 200 OK JSON result.
  Example     :
      POST http://localhost:5001/chat
      Content-Type: application/json

      [
        { "role": "user", "content": "Show me all invoices" }
      ]

================================================================================
9. REGISTERED AI TOOLS
================================================================================

  Tool Name              Method                              Description
  ---------------------  ----------------------------------  ----------------------
  list_invoices          InvoiceApiService.GetInvoicesAsync   Retrieves all invoices
  find_invoice_by_name   InvoiceApiService.GetInvoiceByName   Finds invoice by name
                         Async(string)

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

================================================================================
11. EXTENDING THE AGENT WITH TOOLS
================================================================================

  To add new tools the agent can invoke during conversation:

  1. Add a new method to InvoiceApiService (or create a new service).
  2. Register the service in Startup.ConfigureServices if new.
  3. Open FunctionRegistry.cs and yield return a new AITool:

       yield return AIFunctionFactory.Create(
           typeof(InvoiceApiService).GetMethod(nameof(InvoiceApiService.NewMethod))!,
           invoiceApiService,
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