using SupportChat.Domain.Entities;
using SupportChat.Domain.Enums;

namespace SupportChat.Domain.Services;

public class ShiftService
{
    public bool IsOfficeHours(TimeSpan currentTime)
    {
        // Office hours: 08:00 - 20:00
        var officeStart = new TimeSpan(8, 0, 0);
        var officeEnd = new TimeSpan(20, 0, 0);
        return currentTime >= officeStart && currentTime < officeEnd;
    }

    public Team? GetActiveTeam(List<Team> teams, List<Shift> shifts, TimeSpan currentTime)
    {
        var activeShift = shifts.FirstOrDefault(s => s.IsActive(currentTime));
        if (activeShift == null)
            return null;

        return teams.FirstOrDefault(t => t.Id == activeShift.TeamId && !t.IsOverflowTeam);
    }

    public bool IsAgentShiftActive(Agent agent, List<Shift> shifts, TimeSpan currentTime)
    {
        if (agent.ShiftId == null)
            return false;

        var shift = shifts.FirstOrDefault(s => s.Id == agent.ShiftId.Value);
        if (shift == null)
            return false;

        return shift.IsActive(currentTime);
    }

    public List<Agent> GetAvailableAgents(List<Agent> agents, List<Shift> shifts, TimeSpan currentTime)
    {
        return agents
            .Where(a => IsAgentShiftActive(a, shifts, currentTime))
            .ToList();
    }
}

