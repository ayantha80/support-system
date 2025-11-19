using SupportChat.Domain.Entities;

namespace SupportChat.Domain.Services;

public class CapacityCalculator
{
    public int CalculateAgentCapacity(Agent agent)
    {
        agent.CalculateMaxConcurrency();
        return agent.MaxConcurrency;
    }

    public int CalculateTeamCapacity(Team team)
    {
        return team.Agents
            .Where(a => !a.IsOnOverflowTeam || team.IsOverflowTeam)
            .Sum(a =>
            {
                a.CalculateMaxConcurrency();
                return a.MaxConcurrency;
            });
    }

    public int CalculateMaxQueueLength(int teamCapacity)
    {
        return (int)Math.Floor(teamCapacity * 1.5);
    }
}

