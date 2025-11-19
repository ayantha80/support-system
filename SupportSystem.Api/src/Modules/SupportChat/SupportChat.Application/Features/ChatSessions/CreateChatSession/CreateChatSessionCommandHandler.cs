using MediatR;
using SupportChat.Application.DTOs;
using SupportChat.Application.Interfaces;
using SupportChat.Domain.Entities;
using SupportChat.Domain.Enums;
using SupportChat.Domain.Services;

namespace SupportChat.Application.Features.ChatSessions.CreateChatSession;

public class CreateChatSessionCommandHandler : IRequestHandler<CreateChatSessionCommand, CreateChatSessionResponse>
{
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IQueueRepository _queueRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly QueueRulesService _queueRulesService;
    private readonly ShiftService _shiftService;

    public CreateChatSessionCommandHandler(
        IChatSessionRepository chatSessionRepository,
        IQueueRepository queueRepository,
        ITeamRepository teamRepository,
        IShiftRepository shiftRepository,
        ITimeProvider timeProvider,
        QueueRulesService queueRulesService,
        ShiftService shiftService)
    {
        _chatSessionRepository = chatSessionRepository;
        _queueRepository = queueRepository;
        _teamRepository = teamRepository;
        _shiftRepository = shiftRepository;
        _timeProvider = timeProvider;
        _queueRulesService = queueRulesService;
        _shiftService = shiftService;
    }

    public async Task<CreateChatSessionResponse> Handle(CreateChatSessionCommand request, CancellationToken cancellationToken)
    {
        var currentTime = _timeProvider.CurrentTime;
        var isOfficeHours = _shiftService.IsOfficeHours(currentTime);

        // Get active team
        var allTeams = (await _teamRepository.GetAllAsync()).ToList();
        var allShifts = (await _shiftRepository.GetAllAsync()).ToList();
        var activeTeam = _shiftService.GetActiveTeam(allTeams, allShifts, currentTime);
        var overflowTeam = await _teamRepository.GetOverflowTeamAsync();

        if (activeTeam == null && !isOfficeHours)
        {
            // No active team and outside office hours
            var session = new ChatSession
            {
                Status = SessionStatus.Refused
            };
            await _chatSessionRepository.AddAsync(session);

            return new CreateChatSessionResponse
            {
                ChatSessionId = session.Id,
                Status = SessionStatus.Refused,
                Message = "No active team available. Service is currently unavailable."
            };
        }

        // Get queue lengths
        var mainQueueLength = await _queueRepository.GetQueueLengthAsync(false);
        var overflowQueueLength = await _queueRepository.GetQueueLengthAsync(true);

        // Determine queue decision
        var queueDecision = _queueRulesService.DetermineQueueDecision(
            activeTeam ?? allTeams.First(),
            mainQueueLength,
            overflowTeam,
            overflowQueueLength,
            isOfficeHours);

        var session2 = new ChatSession
        {
            Status = queueDecision.Status,
            IsOverflow = queueDecision.UseOverflow
        };

        await _chatSessionRepository.AddAsync(session2);

        if (queueDecision.CanAccept)
        {
            await _queueRepository.EnqueueAsync(session2.Id, queueDecision.UseOverflow);
            session2.Status = SessionStatus.Queued;
            await _chatSessionRepository.UpdateAsync(session2);
        }

        return new CreateChatSessionResponse
        {
            ChatSessionId = session2.Id,
            Status = session2.Status,
            Message = queueDecision.CanAccept
                ? (queueDecision.UseOverflow ? "Chat session queued in overflow team." : "Chat session queued.")
                : "Queue is full. Chat session refused.",
            IsOverflow = queueDecision.UseOverflow
        };
    }
}


