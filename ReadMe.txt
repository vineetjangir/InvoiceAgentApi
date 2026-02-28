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
  - Searching for an invoice by name
  - Marking an invoice as paid

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

  * Function Invocation Pipeline
    ---------------------------------------------------------
    The ChatClientBuilder pipeline includes:
      - Logging middleware  (UseLogging)
      - Function invocation middleware (UseFunctionInvocation)
    This allows the AI agent to call registered tools/functions automatically
    during a conversation, enabling agentic behavior.

  * Tool / Function Registry
    ---------------------------------------------------------
    FunctionRegistry.cs provides an extensible mechanism to register AI tools
    (AITool instances) that the agent can invoke. Tools are injected into
    ChatOptions and made available to the AI model during each conversation
    turn. Currently the registry yields no tools (yield break) and is ready
    for extension.

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
3. PROJECT STRUCTURE
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
  |
  |-- FunctionRegistry.cs         Static class exposing GetTools() to provide
  |                               AI tools to the chat pipeline. Extensible
  |                               via yield return of AITool instances.
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

================================================================================
4. NUGET PACKAGES
================================================================================

  Package                                Version     Purpose
  -------------------------------------  ----------  ---------------------------
  Microsoft.Extensions.AI                10.3.0      AI abstraction layer
  Microsoft.Extensions.AI.OpenAI         10.3.0      OpenAI integration
  Anthropic.SDK                          5.10.0      Anthropic Claude integration
  GeminiDotnet.Extensions.AI             0.22.0      Google Gemini integration
  dotenv.net                             4.0.1       .env file loading

================================================================================
5. PREREQUISITES
================================================================================

  - .NET 8 SDK
  - API key for at least one supported AI provider
  - A .env file in the project root with the following keys:

      OPENAI_API_KEY=your-openai-key
      GEMINI_API_KEY=your-gemini-key
      CLAUDE_API_KEY=your-claude-key

================================================================================
6. HOW TO RUN
================================================================================

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
7. API ENDPOINTS
================================================================================

  POST /chat
  -----------
  Description : Send a conversation to the AI agent and receive a response.
  Request Body: JSON array of ChatMessage objects (conversation history).
  Behavior    :
      1. The system prompt from SystemPrompt.txt is prepended as the first
         message with the current date/time appended.
      2. The full message list is sent to the configured AI provider.
      3. The AI response is returned as a 200 OK JSON result.
  Example     :
      POST http://localhost:5001/chat
      Content-Type: application/json

      [
        { "role": "user", "content": "Create an invoice for John Doe" }
      ]

================================================================================
8. CONFIGURATION DETAILS
================================================================================

  * Kestrel is configured to listen on 0.0.0.0:5001.
  * CORS allows any header and method from localhost origins.
  * ChatOptions are configured with:
      - Temperature : 1
      - MaxOutputTokens : 5000
  * Logging is set to Information level with Console output.
  * System prompt is loaded once from SystemPrompt.txt at app startup.

================================================================================
9. EXTENDING THE AGENT WITH TOOLS
================================================================================

  To add new tools the agent can invoke during conversation:

  1. Open FunctionRegistry.cs
  2. Inside GetTools(), yield return new AITool instances. Example:

       public static IEnumerable<AITool> GetTools(this IServiceProvider sp)
       {
           yield return AIFunctionFactory.Create(
               (string customerName) => { /* create invoice logic */ },
               "CreateInvoice",
               "Creates a new invoice for the given customer");
       }

  3. The tools are automatically passed to ChatOptions and made available
     to the AI model for function calling.

================================================================================
10. CHANGELOG
================================================================================

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