using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// Send a message to the AI chat assistant
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
    {
        var response = await _chatService.SendMessageAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Check if chat functionality is available
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetStatus()
    {
        return Ok(new { 
            configured = _chatService.IsConfigured,
            message = _chatService.IsConfigured 
                ? "Chat functionality is available" 
                : "Chat functionality is not available. Deploy with GenAI resources using 'bash deploy-with-chat.sh'"
        });
    }
}
