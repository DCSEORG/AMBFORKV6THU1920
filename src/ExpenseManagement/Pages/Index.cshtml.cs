using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public IndexModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public List<Expense> Expenses { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<ExpenseStatus> Statuses { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public int? CategoryFilter { get; set; }
    
    public bool ShowError { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _expenseService.GetAllCategoriesAsync();
        Statuses = await _expenseService.GetAllStatusesAsync();
        
        if (!string.IsNullOrEmpty(Filter) || !string.IsNullOrEmpty(StatusFilter) || CategoryFilter.HasValue)
        {
            int? statusId = null;
            if (!string.IsNullOrEmpty(StatusFilter))
            {
                var status = Statuses.FirstOrDefault(s => s.StatusName == StatusFilter);
                statusId = status?.StatusId;
            }
            
            Expenses = await _expenseService.SearchExpensesAsync(Filter, CategoryFilter, statusId, null, null, null);
        }
        else
        {
            Expenses = await _expenseService.GetAllExpensesAsync();
        }
        
        ShowError = _expenseService.UsingDummyData;
        ErrorMessage = _expenseService.LastError;
    }

    public async Task<IActionResult> OnPostSubmitAsync(int id)
    {
        try
        {
            await _expenseService.SubmitExpenseAsync(id);
        }
        catch
        {
            // Error will be shown on page refresh
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _expenseService.DeleteExpenseAsync(id);
        }
        catch
        {
            // Error will be shown on page refresh
        }
        return RedirectToPage();
    }
}
