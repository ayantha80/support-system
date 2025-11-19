using SupportChat.Application.Interfaces;
using SupportChat.Domain.Entities;

namespace SupportChat.Infrastructure.Repositories;

public class InMemoryAgentRepository : IAgentRepository
{
    private readonly Dictionary<Guid, Agent> _agents = new();

    public Task<Agent?> GetByIdAsync(Guid id)
    {
        _agents.TryGetValue(id, out var agent);
        return Task.FromResult(agent);
    }

    public Task<IEnumerable<Agent>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<Agent>>(_agents.Values);
    }

    public Task<Agent> AddAsync(Agent entity)
    {
        _agents[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(Agent entity)
    {
        if (_agents.ContainsKey(entity.Id))
        {
            _agents[entity.Id] = entity;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _agents.Remove(id);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Agent>> GetByTeamIdAsync(Guid teamId)
    {
        var agents = _agents.Values.Where(a => a.TeamId == teamId);
        return Task.FromResult(agents);
    }

    public Task<IEnumerable<Agent>> GetAvailableAgentsAsync(List<Guid> teamIds)
    {
        var agents = _agents.Values.Where(a => teamIds.Contains(a.TeamId));
        return Task.FromResult(agents);
    }
}

