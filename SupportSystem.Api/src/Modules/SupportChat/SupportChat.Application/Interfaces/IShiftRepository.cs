using SupportChat.Domain.Entities;

namespace SupportChat.Application.Interfaces;

public interface IShiftRepository : IRepository<Shift>
{
    Task<IEnumerable<Shift>> GetByTeamIdAsync(Guid teamId);
    new Task<IEnumerable<Shift>> GetAllAsync();
}

