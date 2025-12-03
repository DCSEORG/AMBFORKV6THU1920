using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class EditExpenseModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public EditExpenseModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public Expense? Expense { get; set; }
    public List<Category> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public ExpenseInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public class ExpenseInput
    {
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? Description { get; set; }
    }

    public async Task OnGetAsync()
    {
        Expense = await _expenseService.GetExpenseByIdAsync(Id);
        Categories = await _expenseService.GetAllCategoriesAsync();
    }

    public async Task<IActionResult> OnPostAsync(int expenseId)
    {
        if (!ModelState.IsValid)
        {
            Expense = await _expenseService.GetExpenseByIdAsync(expenseId);
            Categories = await _expenseService.GetAllCategoriesAsync();
            return Page();
        }

        try
        {
            var request = new ExpenseUpdateRequest
            {
                CategoryId = Input.CategoryId,
                Amount = Input.Amount,
                ExpenseDate = Input.ExpenseDate,
                Description = Input.Description
            };

            await _expenseService.UpdateExpenseAsync(expenseId, request);
            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Expense = await _expenseService.GetExpenseByIdAsync(expenseId);
            Categories = await _expenseService.GetAllCategoriesAsync();
            return Page();
        }
    }
}
