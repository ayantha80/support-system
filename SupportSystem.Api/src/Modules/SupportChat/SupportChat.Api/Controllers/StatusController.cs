using MediatR;
using Microsoft.AspNetCore.Mvc;
using SupportChat.Application.DTOs;
using SupportChat.Application.Features.Status.GetStatus;

namespace SupportChat.Api.Controllers;

[ApiController]
[Route("api/status")]
public class StatusController : ControllerBase
{
    private readonly IMediator _mediator;

    public StatusController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<StatusResponse>> GetStatus()
    {
        var query = new GetStatusQuery();
        var response = await _mediator.Send(query);
        return Ok(response);
    }
}

