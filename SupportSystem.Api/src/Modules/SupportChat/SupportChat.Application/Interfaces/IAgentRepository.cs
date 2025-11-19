using SupportChat.Domain.Entities;

namespace SupportChat.Application.Interfaces;

public interface IAgentRepository : IRepository<Agent>
{
    Task<IEnumerable<Agent>> GetByTeamIdAsync(Guid teamId);
    Task<IEnumerable<Agent>> GetAvailableAgentsAsync(List<Guid> teamIds);
}

