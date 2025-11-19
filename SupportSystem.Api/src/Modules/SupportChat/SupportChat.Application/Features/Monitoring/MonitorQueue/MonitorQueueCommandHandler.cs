using MediatR;
using SupportChat.Application.Interfaces;
using SupportChat.Domain.Enums;
using SupportChat.Domain.Services;

namespace SupportChat.Application.Features.Monitoring.MonitorQueue;

public class MonitorQueueCommandHandler : IRequestHandler<MonitorQueueCommand>
{
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly PollMonitorService _pollMonitorService;

    public MonitorQueueCommandHandler(
        IChatSessionRepository chatSessionRepository,
        IAgentRepository agentRepository,
        ITimeProvider timeProvider,
        PollMonitorService pollMonitorService)
    {
        _chatSessionRepository = chatSessionRepository;
        _agentRepository = agentRepository;
        _timeProvider = timeProvider;
        _pollMonitorService = pollMonitorService;
    }

    public async Task Handle(MonitorQueueCommand request, CancellationToken cancellationToken)
    {
        var currentTime = _timeProvider.UtcNow;
        var activeSessions = await _chatSessionRepository.GetByStatusAsync(SessionStatus.Active);
        var assignedSessions = await _chatSessionRepository.GetByStatusAsync(SessionStatus.Assigned);

        var allSessions = activeSessions.Concat(assignedSessions).ToList();

        foreach (var session in allSessions)
        {
            if (_pollMonitorService.CheckInactivity(session, currentTime))
            {
                var wasAssigned = session.AssignedAgentId.HasValue;

                _pollMonitorService.MarkInactive(session);
                await _chatSessionRepository.UpdateAsync(session);

                // Free up agent capacity
                if (wasAssigned && session.AssignedAgentId.HasValue)
                {
                    var agent = await _agentRepository.GetByIdAsync(session.AssignedAgentId.Value);
                    if (agent != null && agent.CurrentActiveChats > 0)
                    {
                        agent.CurrentActiveChats--;
                        await _agentRepository.UpdateAsync(agent);
                    }

                    session.AssignedAgentId = null;
                    await _chatSessionRepository.UpdateAsync(session);
                }
            }
        }
    }
}


