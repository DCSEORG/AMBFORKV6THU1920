using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class AddExpenseModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public AddExpenseModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public List<Category> Categories { get; set; } = new();
    public List<User> Users { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public ExpenseInput Input { get; set; } = new();

    public class ExpenseInput
    {
        public int UserId { get; set; }
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; } = DateTime.Today;
        public string? Description { get; set; }
    }

    public async Task OnGetAsync()
    {
        Categories = await _expenseService.GetAllCategoriesAsync();
        Users = await _expenseService.GetAllUsersAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Categories = await _expenseService.GetAllCategoriesAsync();
            Users = await _expenseService.GetAllUsersAsync();
            return Page();
        }

        try
        {
            var request = new ExpenseCreateRequest
            {
                UserId = Input.UserId,
                CategoryId = Input.CategoryId,
                Amount = Input.Amount,
                ExpenseDate = Input.ExpenseDate,
                Description = Input.Description
            };

            await _expenseService.CreateExpenseAsync(request);
            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Categories = await _expenseService.GetAllCategoriesAsync();
            Users = await _expenseService.GetAllUsersAsync();
            return Page();
        }
    }
}
