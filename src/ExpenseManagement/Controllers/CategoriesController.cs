using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public CategoriesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Gets all expense categories
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Category>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Category>>> GetAll()
    {
        var categories = await _expenseService.GetAllCategoriesAsync();
        return Ok(categories);
    }

    /// <summary>
    /// Gets a category by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Category), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Category>> GetById(int id)
    {
        var category = await _expenseService.GetCategoryByIdAsync(id);
        if (category == null)
            return NotFound();
        return Ok(category);
    }
}
