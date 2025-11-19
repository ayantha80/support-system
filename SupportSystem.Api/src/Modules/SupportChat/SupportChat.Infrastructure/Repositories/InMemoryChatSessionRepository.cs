using SupportChat.Application.Interfaces;
using SupportChat.Domain.Entities;
using SupportChat.Domain.Enums;

namespace SupportChat.Infrastructure.Repositories;

public class InMemoryChatSessionRepository : IChatSessionRepository
{
    private readonly Dictionary<Guid, ChatSession> _sessions = new();

    public Task<ChatSession?> GetByIdAsync(Guid id)
    {
        _sessions.TryGetValue(id, out var session);
        return Task.FromResult(session);
    }

    public Task<IEnumerable<ChatSession>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<ChatSession>>(_sessions.Values);
    }

    public Task<ChatSession> AddAsync(ChatSession entity)
    {
        _sessions[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(ChatSession entity)
    {
        if (_sessions.ContainsKey(entity.Id))
        {
            _sessions[entity.Id] = entity;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _sessions.Remove(id);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<ChatSession>> GetByStatusAsync(SessionStatus status)
    {
        var sessions = _sessions.Values.Where(s => s.Status == status);
        return Task.FromResult(sessions);
    }

    public Task<IEnumerable<ChatSession>> GetAssignedToAgentAsync(Guid agentId)
    {
        var sessions = _sessions.Values.Where(s => s.AssignedAgentId == agentId);
        return Task.FromResult(sessions);
    }
}

