using SupportChat.Application.Interfaces;
using SupportChat.Domain.Entities;

namespace SupportChat.Infrastructure.Repositories;

public class InMemoryTeamRepository : ITeamRepository
{
    private readonly Dictionary<Guid, Team> _teams = new();

    public Task<Team?> GetByIdAsync(Guid id)
    {
        _teams.TryGetValue(id, out var team);
        return Task.FromResult(team);
    }

    public Task<IEnumerable<Team>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<Team>>(_teams.Values);
    }

    public Task<Team> AddAsync(Team entity)
    {
        _teams[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(Team entity)
    {
        if (_teams.ContainsKey(entity.Id))
        {
            _teams[entity.Id] = entity;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _teams.Remove(id);
        return Task.CompletedTask;
    }

    public Task<Team?> GetByNameAsync(string name)
    {
        var team = _teams.Values.FirstOrDefault(t => t.Name == name);
        return Task.FromResult(team);
    }

    public Task<Team?> GetOverflowTeamAsync()
    {
        var team = _teams.Values.FirstOrDefault(t => t.IsOverflowTeam);
        return Task.FromResult(team);
    }
}

