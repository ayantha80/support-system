using SupportChat.Domain.Entities;

namespace SupportChat.Application.Interfaces;

public interface ITeamRepository : IRepository<Team>
{
    Task<Team?> GetByNameAsync(string name);
    Task<Team?> GetOverflowTeamAsync();
}

