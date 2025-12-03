using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ApproveModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public ApproveModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public List<Expense> PendingExpenses { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }
    
    public bool ShowError { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadPendingExpensesAsync();
    }

    public async Task<IActionResult> OnPostApproveAsync(string expenseIds)
    {
        if (string.IsNullOrEmpty(expenseIds))
        {
            await LoadPendingExpensesAsync();
            return Page();
        }

        var ids = expenseIds.Split(',').Select(int.Parse);
        var approvedCount = 0;

        foreach (var id in ids)
        {
            try
            {
                // Default manager ID is 2 (Bob Manager)
                await _expenseService.ApproveExpenseAsync(id, 2);
                approvedCount++;
            }
            catch
            {
                // Continue with other expenses
            }
        }

        SuccessMessage = $"Successfully approved {approvedCount} expense(s).";
        await LoadPendingExpensesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRejectAsync(string expenseIds)
    {
        if (string.IsNullOrEmpty(expenseIds))
        {
            await LoadPendingExpensesAsync();
            return Page();
        }

        var ids = expenseIds.Split(',').Select(int.Parse);
        var rejectedCount = 0;

        foreach (var id in ids)
        {
            try
            {
                // Default manager ID is 2 (Bob Manager)
                await _expenseService.RejectExpenseAsync(id, 2);
                rejectedCount++;
            }
            catch
            {
                // Continue with other expenses
            }
        }

        SuccessMessage = $"Successfully rejected {rejectedCount} expense(s).";
        await LoadPendingExpensesAsync();
        return Page();
    }

    private async Task LoadPendingExpensesAsync()
    {
        PendingExpenses = await _expenseService.GetPendingExpensesAsync();
        
        if (!string.IsNullOrEmpty(Filter))
        {
            PendingExpenses = PendingExpenses
                .Where(e => 
                    (e.Description?.Contains(Filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    e.CategoryName.Contains(Filter, StringComparison.OrdinalIgnoreCase) ||
                    e.UserName.Contains(Filter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        
        ShowError = _expenseService.UsingDummyData;
        ErrorMessage = _expenseService.LastError;
    }
}
