using MediatR;
using Microsoft.AspNetCore.Mvc;
using SupportChat.Application.DTOs;
using SupportChat.Application.Features.ChatSessions.CreateChatSession;
using SupportChat.Application.Features.ChatSessions.PollSession;

namespace SupportChat.Api.Controllers;

[ApiController]
[Route("api/chat-sessions")]
public class ChatSessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatSessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<CreateChatSessionResponse>> CreateChatSession(
        [FromBody] CreateChatSessionCommand command)
    {
        var response = await _mediator.Send(command);

        if (response.Status == Domain.Enums.SessionStatus.Refused)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpGet("{id}/poll")]
    public async Task<ActionResult<PollSessionResponse>> PollSession(Guid id)
    {
        try
        {
            var query = new PollSessionQuery(id);
            var response = await _mediator.Send(query);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

