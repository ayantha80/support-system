using MediatR;
using SupportChat.Application.Interfaces;
using SupportChat.Domain.Entities;
using SupportChat.Domain.Enums;
using SupportChat.Domain.Services;

namespace SupportChat.Application.Features.Assignments.AssignChats;

public class AssignChatsCommandHandler : IRequestHandler<AssignChatsCommand>
{
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IQueueRepository _queueRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly AssignmentStrategy _assignmentStrategy;
    private readonly ShiftService _shiftService;
    private readonly Dictionary<Domain.Enums.SeniorityLevel, int> _lastAssignedIndex = new();

    public AssignChatsCommandHandler(
        IChatSessionRepository chatSessionRepository,
        IAgentRepository agentRepository,
        IQueueRepository queueRepository,
        ITeamRepository teamRepository,
        IShiftRepository shiftRepository,
        ITimeProvider timeProvider,
        AssignmentStrategy assignmentStrategy,
        ShiftService shiftService)
    {
        _chatSessionRepository = chatSessionRepository;
        _agentRepository = agentRepository;
        _queueRepository = queueRepository;
        _teamRepository = teamRepository;
        _shiftRepository = shiftRepository;
        _timeProvider = timeProvider;
        _assignmentStrategy = assignmentStrategy;
        _shiftService = shiftService;
    }

    public async Task Handle(AssignChatsCommand request, CancellationToken cancellationToken)
    {
        var currentTime = _timeProvider.CurrentTime;

        // Process inactive sessions first
        await ProcessInactiveSessionsAsync();

        // Get active team
        var allTeams = (await _teamRepository.GetAllAsync()).ToList();
        var allShifts = (await _shiftRepository.GetAllAsync()).ToList();
        var activeTeam = _shiftService.GetActiveTeam(allTeams, allShifts, currentTime);
        var overflowTeam = await _teamRepository.GetOverflowTeamAsync();

        // Process main queue
        if (activeTeam != null)
        {
            await ProcessQueueAsync(activeTeam, allShifts, currentTime, false);
        }

        // Process overflow queue if office hours
        if (_shiftService.IsOfficeHours(currentTime) && overflowTeam != null)
        {
            await ProcessQueueAsync(overflowTeam, allShifts, currentTime, true);
        }
    }

    private async Task ProcessInactiveSessionsAsync()
    {
        var activeSessions = await _chatSessionRepository.GetByStatusAsync(SessionStatus.Active);
        var assignedSessions = await _chatSessionRepository.GetByStatusAsync(SessionStatus.Assigned);

        var allSessions = activeSessions.Concat(assignedSessions).ToList();

        foreach (var session in allSessions)
        {
            if (session.AssignedAgentId.HasValue && session.Status == SessionStatus.Inactive)
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

    private async Task ProcessQueueAsync(Team team, List<Shift> shifts, TimeSpan currentTime, bool isOverflow)
    {
        var queuedItems = (await _queueRepository.GetAllQueuedItemsAsync(isOverflow))
            .OrderBy(q => q.EnqueuedAt)
            .ToList();

        if (!queuedItems.Any())
            return;

        // Get available agents for this team
        var teamAgents = (await _agentRepository.GetByTeamIdAsync(team.Id)).ToList();
        var availableAgents = _shiftService.GetAvailableAgents(teamAgents, shifts, currentTime)
            .Where(a => a.HasCapacity)
            .ToList();

        if (!availableAgents.Any())
            return;

        // Assign chats to agents
        foreach (var queueItem in queuedItems)
        {
            var session = await _chatSessionRepository.GetByIdAsync(queueItem.ChatSessionId);
            if (session == null || session.Status != SessionStatus.Queued)
                continue;

            var agent = _assignmentStrategy.SelectAgentForAssignment(availableAgents, _lastAssignedIndex);
            if (agent == null)
                break;

            // Assign session to agent
            session.AssignedAgentId = agent.Id;
            session.Status = SessionStatus.Assigned;
            session.TeamId = team.Id;
            await _chatSessionRepository.UpdateAsync(session);

            // Update agent capacity
            agent.CurrentActiveChats++;
            await _agentRepository.UpdateAsync(agent);

            // Remove from queue
            await _queueRepository.RemoveAsync(queueItem.ChatSessionId, isOverflow);

            // Update available agents list
            if (!agent.HasCapacity)
            {
                availableAgents.Remove(agent);
            }
        }
    }
}


