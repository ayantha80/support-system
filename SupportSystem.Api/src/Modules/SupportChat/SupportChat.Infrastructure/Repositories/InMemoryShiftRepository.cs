using SupportChat.Application.Interfaces;
using SupportChat.Domain.Entities;

namespace SupportChat.Infrastructure.Repositories;

public class InMemoryShiftRepository : IShiftRepository
{
    private readonly Dictionary<Guid, Shift> _shifts = new();

    public Task<Shift?> GetByIdAsync(Guid id)
    {
        _shifts.TryGetValue(id, out var shift);
        return Task.FromResult(shift);
    }

    public Task<IEnumerable<Shift>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<Shift>>(_shifts.Values);
    }

    public Task<Shift> AddAsync(Shift entity)
    {
        _shifts[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(Shift entity)
    {
        if (_shifts.ContainsKey(entity.Id))
        {
            _shifts[entity.Id] = entity;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _shifts.Remove(id);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Shift>> GetByTeamIdAsync(Guid teamId)
    {
        var shifts = _shifts.Values.Where(s => s.TeamId == teamId);
        return Task.FromResult(shifts);
    }
}

