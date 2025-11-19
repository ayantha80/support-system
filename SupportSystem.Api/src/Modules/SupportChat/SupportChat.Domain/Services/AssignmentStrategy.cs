using SupportChat.Domain.Entities;
using SupportChat.Domain.Enums;

namespace SupportChat.Domain.Services;

public class AssignmentStrategy
{
    public Agent? SelectAgentForAssignment(List<Agent> availableAgents, Dictionary<SeniorityLevel, int> lastAssignedIndex)
    {
        if (!availableAgents.Any())
            return null;

        // Filter agents with capacity
        var agentsWithCapacity = availableAgents
            .Where(a => a.HasCapacity)
            .ToList();

        if (!agentsWithCapacity.Any())
            return null;

        // Order by seniority: Junior first, then Mid, then Senior, then Team Lead
        var orderedBySeniority = agentsWithCapacity
            .OrderBy(a => a.SeniorityLevel)
            .GroupBy(a => a.SeniorityLevel)
            .ToList();

        // Get the first seniority level with available agents
        var firstSeniorityGroup = orderedBySeniority.FirstOrDefault();
        if (firstSeniorityGroup == null)
            return null;

        var agentsInLevel = firstSeniorityGroup.ToList();

        // Round-robin within the same seniority level
        var seniorityKey = firstSeniorityGroup.Key;
        if (!lastAssignedIndex.ContainsKey(seniorityKey))
        {
            lastAssignedIndex[seniorityKey] = -1;
        }

        lastAssignedIndex[seniorityKey] = (lastAssignedIndex[seniorityKey] + 1) % agentsInLevel.Count;
        return agentsInLevel[lastAssignedIndex[seniorityKey]];
    }

    public List<(Agent agent, int count)> DistributeChats(
        List<Agent> availableAgents,
        int totalChats,
        Dictionary<SeniorityLevel, int> lastAssignedIndex)
    {
        var distribution = new Dictionary<Guid, (Agent agent, int count)>();
        var remainingChats = totalChats;

        // Create a working copy of agents to track their state during distribution
        var workingAgents = availableAgents
            .Where(a => a.HasCapacity)
            .Select(a => new { Agent = a, OriginalChats = a.CurrentActiveChats })
            .ToList();

        // Restore original state after distribution
        try
        {
            while (remainingChats > 0 && workingAgents.Any(w => w.Agent.HasCapacity))
            {
                var agentsWithCapacity = workingAgents
                    .Where(w => w.Agent.HasCapacity)
                    .Select(w => w.Agent)
                    .ToList();

                if (!agentsWithCapacity.Any())
                    break;

                var agent = SelectAgentForAssignment(agentsWithCapacity, lastAssignedIndex);
                if (agent == null)
                    break;

                // Assign one chat at a time to ensure proper distribution
                var chatsToAssign = 1;
                remainingChats -= chatsToAssign;

                if (distribution.ContainsKey(agent.Id))
                {
                    var existing = distribution[agent.Id];
                    distribution[agent.Id] = (agent, existing.count + chatsToAssign);
                }
                else
                {
                    distribution[agent.Id] = (agent, chatsToAssign);
                }

                // Update agent's current active chats for next iteration
                agent.CurrentActiveChats += chatsToAssign;
            }
        }
        finally
        {
            // Restore original state
            foreach (var workingAgent in workingAgents)
            {
                workingAgent.Agent.CurrentActiveChats = workingAgent.OriginalChats;
            }
        }

        return distribution.Values.Where(d => d.count > 0).ToList();
    }
}

