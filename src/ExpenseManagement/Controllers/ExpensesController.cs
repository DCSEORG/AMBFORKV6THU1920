using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Gets all expenses
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Expense>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Expense>>> GetAll()
    {
        var expenses = await _expenseService.GetAllExpensesAsync();
        return Ok(expenses);
    }

    /// <summary>
    /// Gets an expense by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Expense), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Expense>> GetById(int id)
    {
        var expense = await _expenseService.GetExpenseByIdAsync(id);
        if (expense == null)
            return NotFound();
        return Ok(expense);
    }

    /// <summary>
    /// Gets expenses by status
    /// </summary>
    [HttpGet("status/{statusName}")]
    [ProducesResponseType(typeof(List<Expense>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Expense>>> GetByStatus(string statusName)
    {
        var expenses = await _expenseService.GetExpensesByStatusAsync(statusName);
        return Ok(expenses);
    }

    /// <summary>
    /// Gets expenses by user
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<Expense>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Expense>>> GetByUser(int userId)
    {
        var expenses = await _expenseService.GetExpensesByUserAsync(userId);
        return Ok(expenses);
    }

    /// <summary>
    /// Gets pending expenses awaiting approval
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<Expense>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Expense>>> GetPending()
    {
        var expenses = await _expenseService.GetPendingExpensesAsync();
        return Ok(expenses);
    }

    /// <summary>
    /// Search expenses with filters
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<Expense>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Expense>>> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] int? categoryId,
        [FromQuery] int? statusId,
        [FromQuery] int? userId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var expenses = await _expenseService.SearchExpensesAsync(searchTerm, categoryId, statusId, userId, startDate, endDate);
        return Ok(expenses);
    }

    /// <summary>
    /// Creates a new expense
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Create([FromBody] ExpenseCreateRequest request)
    {
        try
        {
            var id = await _expenseService.CreateExpenseAsync(request);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing expense
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Update(int id, [FromBody] ExpenseUpdateRequest request)
    {
        try
        {
            await _expenseService.UpdateExpenseAsync(id, request);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes an expense
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await _expenseService.DeleteExpenseAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Submits an expense for approval
    /// </summary>
    [HttpPost("{id}/submit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Submit(int id)
    {
        try
        {
            await _expenseService.SubmitExpenseAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Approves an expense
    /// </summary>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Approve(int id, [FromQuery] int reviewedBy)
    {
        try
        {
            await _expenseService.ApproveExpenseAsync(id, reviewedBy);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Rejects an expense
    /// </summary>
    [HttpPost("{id}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Reject(int id, [FromQuery] int reviewedBy)
    {
        try
        {
            await _expenseService.RejectExpenseAsync(id, reviewedBy);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
