using MediatR;
using SupportChat.Application.DTOs;
using SupportChat.Application.Interfaces;
using SupportChat.Domain.Services;

namespace SupportChat.Application.Features.Status.GetStatus;

public class GetStatusQueryHandler : IRequestHandler<GetStatusQuery, StatusResponse>
{
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IQueueRepository _queueRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly CapacityCalculator _capacityCalculator;
    private readonly ShiftService _shiftService;

    public GetStatusQueryHandler(
        IChatSessionRepository chatSessionRepository,
        IQueueRepository queueRepository,
        ITeamRepository teamRepository,
        IShiftRepository shiftRepository,
        IAgentRepository agentRepository,
        ITimeProvider timeProvider,
        CapacityCalculator capacityCalculator,
        ShiftService shiftService)
    {
        _chatSessionRepository = chatSessionRepository;
        _queueRepository = queueRepository;
        _teamRepository = teamRepository;
        _shiftRepository = shiftRepository;
        _agentRepository = agentRepository;
        _timeProvider = timeProvider;
        _capacityCalculator = capacityCalculator;
        _shiftService = shiftService;
    }

    public async Task<StatusResponse> Handle(GetStatusQuery request, CancellationToken cancellationToken)
    {
        var currentTime = _timeProvider.CurrentTime;
        var isOfficeHours = _shiftService.IsOfficeHours(currentTime);

        var allTeams = (await _teamRepository.GetAllAsync()).ToList();
        var allShifts = (await _shiftRepository.GetAllAsync()).ToList();
        var activeTeam = _shiftService.GetActiveTeam(allTeams, allShifts, currentTime);

        var teamCapacity = 0;
        var maxQueueLength = 0;
        var activeTeamName = activeTeam?.Name;

        if (activeTeam != null)
        {
            teamCapacity = _capacityCalculator.CalculateTeamCapacity(activeTeam);
            maxQueueLength = _capacityCalculator.CalculateMaxQueueLength(teamCapacity);
        }

        var currentQueueLength = await _queueRepository.GetQueueLengthAsync(false);
        var overflowQueueLength = await _queueRepository.GetQueueLengthAsync(true);

        var activeSessions = (await _chatSessionRepository.GetByStatusAsync(Domain.Enums.SessionStatus.Active)).Count();

        var allAgents = (await _agentRepository.GetAllAsync()).ToList();
        var agentStatuses = allAgents.Select(a =>
        {
            a.CalculateMaxConcurrency();
            return new AgentStatus
            {
                Id = a.Id,
                Name = a.Name,
                Seniority = a.SeniorityLevel.ToString(),
                CurrentChats = a.CurrentActiveChats,
                MaxConcurrency = a.MaxConcurrency
            };
        }).ToList();

        return new StatusResponse
        {
            ActiveTeam = activeTeamName,
            TeamCapacity = teamCapacity,
            MaxQueueLength = maxQueueLength,
            CurrentQueueLength = currentQueueLength,
            OverflowQueueLength = overflowQueueLength,
            ActiveSessions = activeSessions,
            IsOfficeHours = isOfficeHours,
            Agents = agentStatuses
        };
    }
}


