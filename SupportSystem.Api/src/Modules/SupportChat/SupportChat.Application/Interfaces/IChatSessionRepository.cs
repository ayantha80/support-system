using SupportChat.Domain.Entities;

namespace SupportChat.Application.Interfaces;

public interface IChatSessionRepository : IRepository<ChatSession>
{
    Task<IEnumerable<ChatSession>> GetByStatusAsync(Domain.Enums.SessionStatus status);
    Task<IEnumerable<ChatSession>> GetAssignedToAgentAsync(Guid agentId);
}

