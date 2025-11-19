using SupportChat.Domain.Entities;
using SupportChat.Domain.Enums;

namespace SupportChat.Domain.Services;

public class PollMonitorService
{
    private readonly TimeSpan _inactivityThreshold;

    public PollMonitorService(TimeSpan inactivityThreshold)
    {
        _inactivityThreshold = inactivityThreshold;
    }

    public void UpdateLastPoll(ChatSession session)
    {
        session.LastPollAt = DateTime.UtcNow;
        if (session.Status == SessionStatus.Queued || session.Status == SessionStatus.Assigned)
        {
            session.Status = SessionStatus.Active;
        }
    }

    public bool CheckInactivity(ChatSession session, DateTime currentTime)
    {
        if (session.LastPollAt == null)
            return false;

        var timeSinceLastPoll = currentTime - session.LastPollAt.Value;
        return timeSinceLastPoll > _inactivityThreshold;
    }

    public void MarkInactive(ChatSession session)
    {
        if (session.Status == SessionStatus.Active || session.Status == SessionStatus.Assigned)
        {
            session.Status = SessionStatus.Inactive;
            // Capacity will be freed when the assignment service processes inactive sessions
        }
    }
}

