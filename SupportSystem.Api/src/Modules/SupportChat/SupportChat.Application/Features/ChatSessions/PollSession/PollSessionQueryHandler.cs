using MediatR;
using SupportChat.Application.DTOs;
using SupportChat.Application.Interfaces;
using SupportChat.Domain.Services;

namespace SupportChat.Application.Features.ChatSessions.PollSession;

public class PollSessionQueryHandler : IRequestHandler<PollSessionQuery, PollSessionResponse>
{
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly PollMonitorService _pollMonitorService;

    public PollSessionQueryHandler(
        IChatSessionRepository chatSessionRepository,
        IAgentRepository agentRepository,
        PollMonitorService pollMonitorService)
    {
        _chatSessionRepository = chatSessionRepository;
        _agentRepository = agentRepository;
        _pollMonitorService = pollMonitorService;
    }

    public async Task<PollSessionResponse> Handle(PollSessionQuery request, CancellationToken cancellationToken)
    {
        var session = await _chatSessionRepository.GetByIdAsync(request.SessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Chat session {request.SessionId} not found.");
        }

        _pollMonitorService.UpdateLastPoll(session);
        await _chatSessionRepository.UpdateAsync(session);

        Domain.Entities.Agent? agent = null;
        if (session.AssignedAgentId.HasValue)
        {
            agent = await _agentRepository.GetByIdAsync(session.AssignedAgentId.Value);
        }

        return new PollSessionResponse
        {
            ChatSessionId = session.Id,
            Status = session.Status,
            AssignedAgentId = session.AssignedAgentId,
            AssignedAgentName = agent?.Name,
            IsOverflow = session.IsOverflow
        };
    }
}


