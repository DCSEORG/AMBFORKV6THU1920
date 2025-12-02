using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public class ChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly IExpenseService _expenseService;
    
    public bool IsConfigured { get; private set; }

    public ChatService(IConfiguration configuration, ILogger<ChatService> logger, IExpenseService expenseService)
    {
        _configuration = configuration;
        _logger = logger;
        _expenseService = expenseService;
        
        var endpoint = _configuration["OpenAI:Endpoint"];
        IsConfigured = !string.IsNullOrEmpty(endpoint);
    }

    public async Task<ChatResponse> SendMessageAsync(ChatRequest request)
    {
        if (!IsConfigured)
        {
            return new ChatResponse
            {
                Success = false,
                Message = "Chat functionality is not available. GenAI services were not deployed. " +
                         "Please run 'bash deploy-with-chat.sh' to deploy the application with GenAI resources enabled.",
                Error = "GenAI services not configured"
            };
        }

        try
        {
            var endpoint = _configuration["OpenAI:Endpoint"]!;
            var deploymentName = _configuration["OpenAI:DeploymentName"] ?? "gpt-4o";
            
            // Use ManagedIdentityCredential with explicit client ID
            var managedIdentityClientId = _configuration["ManagedIdentityClientId"];
            Azure.Core.TokenCredential credential;
            
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                credential = new ManagedIdentityCredential(managedIdentityClientId);
            }
            else
            {
                _logger.LogInformation("Using DefaultAzureCredential");
                credential = new DefaultAzureCredential();
            }
            
            var client = new OpenAIClient(new Uri(endpoint), credential);
            
            var chatMessages = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage(GetSystemPrompt())
            };
            
            // Add conversation history
            foreach (var msg in request.History)
            {
                if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                    chatMessages.Add(new ChatRequestUserMessage(msg.Content));
                else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                    chatMessages.Add(new ChatRequestAssistantMessage(msg.Content));
            }
            
            // Add current message
            chatMessages.Add(new ChatRequestUserMessage(request.Message));
            
            var options = new ChatCompletionsOptions(deploymentName, chatMessages)
            {
                Temperature = 0.7f,
                MaxTokens = 1000,
                Functions =
                {
                    GetExpensesFunction(),
                    GetCategoriesFunction(),
                    GetPendingExpensesFunction(),
                    CreateExpenseFunction(),
                    ApproveExpenseFunction(),
                    RejectExpenseFunction()
                }
            };
            
            var response = await ProcessChatWithFunctionCallingAsync(client, options, deploymentName);
            
            return new ChatResponse
            {
                Success = true,
                Message = response
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendMessageAsync");
            return new ChatResponse
            {
                Success = false,
                Message = "An error occurred while processing your request.",
                Error = ex.Message
            };
        }
    }

    private async Task<string> ProcessChatWithFunctionCallingAsync(OpenAIClient client, ChatCompletionsOptions options, string deploymentName)
    {
        while (true)
        {
            var response = await client.GetChatCompletionsAsync(options);
            var choice = response.Value.Choices[0];
            
            if (choice.FinishReason == CompletionsFinishReason.FunctionCall)
            {
                var functionCall = choice.Message.FunctionCall;
                _logger.LogInformation("Function called: {FunctionName}", functionCall.Name);
                
                var functionResult = await ExecuteFunctionAsync(functionCall.Name, functionCall.Arguments);
                
                // Add assistant's function call message
                options.Messages.Add(new ChatRequestAssistantMessage(choice.Message.Content ?? "")
                {
                    FunctionCall = functionCall
                });
                
                // Add function result
                options.Messages.Add(new ChatRequestFunctionMessage(functionCall.Name, functionResult));
            }
            else
            {
                return choice.Message.Content ?? "I'm sorry, I couldn't generate a response.";
            }
        }
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, string arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<JsonElement>(arguments);
            
            switch (functionName)
            {
                case "get_expenses":
                    var status = args.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;
                    var expenses = string.IsNullOrEmpty(status) 
                        ? await _expenseService.GetAllExpensesAsync()
                        : await _expenseService.GetExpensesByStatusAsync(status);
                    return JsonSerializer.Serialize(expenses);
                
                case "get_categories":
                    var categories = await _expenseService.GetAllCategoriesAsync();
                    return JsonSerializer.Serialize(categories);
                
                case "get_pending_expenses":
                    var pending = await _expenseService.GetPendingExpensesAsync();
                    return JsonSerializer.Serialize(pending);
                
                case "create_expense":
                    var createRequest = new ExpenseCreateRequest
                    {
                        UserId = args.GetProperty("user_id").GetInt32(),
                        CategoryId = args.GetProperty("category_id").GetInt32(),
                        Amount = args.GetProperty("amount").GetDecimal(),
                        ExpenseDate = args.GetProperty("expense_date").GetDateTime(),
                        Description = args.TryGetProperty("description", out var descProp) ? descProp.GetString() : null
                    };
                    var newId = await _expenseService.CreateExpenseAsync(createRequest);
                    return JsonSerializer.Serialize(new { success = true, expenseId = newId });
                
                case "approve_expense":
                    var approveId = args.GetProperty("expense_id").GetInt32();
                    var approverId = args.TryGetProperty("reviewer_id", out var reviewerProp) ? reviewerProp.GetInt32() : 2;
                    await _expenseService.ApproveExpenseAsync(approveId, approverId);
                    return JsonSerializer.Serialize(new { success = true, message = "Expense approved successfully" });
                
                case "reject_expense":
                    var rejectId = args.GetProperty("expense_id").GetInt32();
                    var rejectReviewerId = args.TryGetProperty("reviewer_id", out var rejectReviewerProp) ? rejectReviewerProp.GetInt32() : 2;
                    await _expenseService.RejectExpenseAsync(rejectId, rejectReviewerId);
                    return JsonSerializer.Serialize(new { success = true, message = "Expense rejected successfully" });
                
                default:
                    return JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private string GetSystemPrompt()
    {
        return @"You are a helpful assistant for the Expense Management System. You help users manage their expenses, 
view expense reports, and process approvals.

You have access to the following functions:
- get_expenses: Retrieve expenses, optionally filtered by status (Draft, Submitted, Approved, Rejected)
- get_categories: Get all expense categories (Travel, Meals, Supplies, Accommodation, Other)
- get_pending_expenses: Get all expenses awaiting approval
- create_expense: Create a new expense
- approve_expense: Approve a pending expense
- reject_expense: Reject a pending expense

When displaying expense lists, format them nicely with:
- Date, Category, Amount (in GBP £), and Status
- Use bullet points or numbered lists for clarity
- Show the total amount when appropriate

Be helpful and concise. If users ask about functionality not available through these functions, 
explain what you can help with instead.";
    }

    private static FunctionDefinition GetExpensesFunction()
    {
        return new FunctionDefinition
        {
            Name = "get_expenses",
            Description = "Retrieves expenses from the database. Can filter by status.",
            Parameters = BinaryData.FromString(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""status"": {
                        ""type"": ""string"",
                        ""description"": ""Filter by status: Draft, Submitted, Approved, or Rejected"",
                        ""enum"": [""Draft"", ""Submitted"", ""Approved"", ""Rejected""]
                    }
                },
                ""required"": []
            }")
        };
    }

    private static FunctionDefinition GetCategoriesFunction()
    {
        return new FunctionDefinition
        {
            Name = "get_categories",
            Description = "Retrieves all expense categories from the database.",
            Parameters = BinaryData.FromString(@"{
                ""type"": ""object"",
                ""properties"": {},
                ""required"": []
            }")
        };
    }

    private static FunctionDefinition GetPendingExpensesFunction()
    {
        return new FunctionDefinition
        {
            Name = "get_pending_expenses",
            Description = "Retrieves all expenses that are pending approval (status = Submitted).",
            Parameters = BinaryData.FromString(@"{
                ""type"": ""object"",
                ""properties"": {},
                ""required"": []
            }")
        };
    }

    private static FunctionDefinition CreateExpenseFunction()
    {
        return new FunctionDefinition
        {
            Name = "create_expense",
            Description = "Creates a new expense in the system.",
            Parameters = BinaryData.FromString(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""user_id"": {
                        ""type"": ""integer"",
                        ""description"": ""The ID of the user creating the expense""
                    },
                    ""category_id"": {
                        ""type"": ""integer"",
                        ""description"": ""The category ID (1=Travel, 2=Meals, 3=Supplies, 4=Accommodation, 5=Other)""
                    },
                    ""amount"": {
                        ""type"": ""number"",
                        ""description"": ""The expense amount in GBP (e.g., 25.50)""
                    },
                    ""expense_date"": {
                        ""type"": ""string"",
                        ""format"": ""date"",
                        ""description"": ""The date of the expense (YYYY-MM-DD)""
                    },
                    ""description"": {
                        ""type"": ""string"",
                        ""description"": ""Description of the expense""
                    }
                },
                ""required"": [""user_id"", ""category_id"", ""amount"", ""expense_date""]
            }")
        };
    }

    private static FunctionDefinition ApproveExpenseFunction()
    {
        return new FunctionDefinition
        {
            Name = "approve_expense",
            Description = "Approves a pending expense.",
            Parameters = BinaryData.FromString(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""expense_id"": {
                        ""type"": ""integer"",
                        ""description"": ""The ID of the expense to approve""
                    },
                    ""reviewer_id"": {
                        ""type"": ""integer"",
                        ""description"": ""The ID of the manager approving the expense (default: 2)""
                    }
                },
                ""required"": [""expense_id""]
            }")
        };
    }

    private static FunctionDefinition RejectExpenseFunction()
    {
        return new FunctionDefinition
        {
            Name = "reject_expense",
            Description = "Rejects a pending expense.",
            Parameters = BinaryData.FromString(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""expense_id"": {
                        ""type"": ""integer"",
                        ""description"": ""The ID of the expense to reject""
                    },
                    ""reviewer_id"": {
                        ""type"": ""integer"",
                        ""description"": ""The ID of the manager rejecting the expense (default: 2)""
                    }
                },
                ""required"": [""expense_id""]
            }")
        };
    }
}
