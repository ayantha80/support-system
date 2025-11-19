using SupportChat.Domain.Entities;
using SupportChat.Domain.Enums;

namespace SupportChat.Domain.Services;

public class QueueRulesService
{
    private readonly CapacityCalculator _capacityCalculator;

    public QueueRulesService(CapacityCalculator capacityCalculator)
    {
        _capacityCalculator = capacityCalculator;
    }

    public bool CanAcceptChat(Team team, int currentQueueLength, bool isOfficeHours, Team? overflowTeam = null)
    {
        var teamCapacity = _capacityCalculator.CalculateTeamCapacity(team);
        var maxQueueLength = _capacityCalculator.CalculateMaxQueueLength(teamCapacity);

        if (currentQueueLength < maxQueueLength)
            return true;

        // Queue is full, check overflow
        if (isOfficeHours && overflowTeam != null)
        {
            var overflowCapacity = _capacityCalculator.CalculateTeamCapacity(overflowTeam);
            var overflowMaxQueueLength = _capacityCalculator.CalculateMaxQueueLength(overflowCapacity);
            // We'll check overflow queue length separately
            return true; // Overflow can be used
        }

        return false;
    }

    public QueueDecision DetermineQueueDecision(
        Team mainTeam,
        int mainQueueLength,
        Team? overflowTeam,
        int overflowQueueLength,
        bool isOfficeHours)
    {
        var mainCapacity = _capacityCalculator.CalculateTeamCapacity(mainTeam);
        var mainMaxQueueLength = _capacityCalculator.CalculateMaxQueueLength(mainCapacity);

        if (mainQueueLength < mainMaxQueueLength)
        {
            return new QueueDecision
            {
                CanAccept = true,
                UseOverflow = false,
                Status = SessionStatus.Queued
            };
        }

        // Main queue is full
        if (isOfficeHours && overflowTeam != null)
        {
            var overflowCapacity = _capacityCalculator.CalculateTeamCapacity(overflowTeam);
            var overflowMaxQueueLength = _capacityCalculator.CalculateMaxQueueLength(overflowCapacity);

            if (overflowQueueLength < overflowMaxQueueLength)
            {
                return new QueueDecision
                {
                    CanAccept = true,
                    UseOverflow = true,
                    Status = SessionStatus.Queued
                };
            }
        }

        return new QueueDecision
        {
            CanAccept = false,
            UseOverflow = false,
            Status = SessionStatus.Refused
        };
    }
}

public class QueueDecision
{
    public bool CanAccept { get; set; }
    public bool UseOverflow { get; set; }
    public SessionStatus Status { get; set; }
}

