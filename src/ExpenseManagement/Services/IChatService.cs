using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(ChatRequest request);
    bool IsConfigured { get; }
}
