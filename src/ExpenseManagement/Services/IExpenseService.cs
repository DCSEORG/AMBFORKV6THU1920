using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public interface IExpenseService
{
    // Expense operations
    Task<List<Expense>> GetAllExpensesAsync();
    Task<Expense?> GetExpenseByIdAsync(int expenseId);
    Task<List<Expense>> GetExpensesByStatusAsync(string statusName);
    Task<List<Expense>> GetExpensesByUserAsync(int userId);
    Task<List<Expense>> GetPendingExpensesAsync();
    Task<List<Expense>> SearchExpensesAsync(string? searchTerm, int? categoryId, int? statusId, int? userId, DateTime? startDate, DateTime? endDate);
    Task<int> CreateExpenseAsync(ExpenseCreateRequest request);
    Task UpdateExpenseAsync(int expenseId, ExpenseUpdateRequest request);
    Task DeleteExpenseAsync(int expenseId);
    Task SubmitExpenseAsync(int expenseId);
    Task ApproveExpenseAsync(int expenseId, int reviewedBy);
    Task RejectExpenseAsync(int expenseId, int reviewedBy);
    
    // Category operations
    Task<List<Category>> GetAllCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int categoryId);
    
    // Status operations
    Task<List<ExpenseStatus>> GetAllStatusesAsync();
    
    // User operations
    Task<List<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int userId);
    Task<List<User>> GetManagersAsync();
    
    // Error info
    string? LastError { get; }
    bool UsingDummyData { get; }
}
