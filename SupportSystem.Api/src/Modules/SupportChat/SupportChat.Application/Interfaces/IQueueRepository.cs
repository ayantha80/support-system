using SupportChat.Domain.Entities;

namespace SupportChat.Application.Interfaces;

public interface IQueueRepository
{
    Task<QueueItem> EnqueueAsync(Guid chatSessionId, bool isOverflow = false);
    Task<QueueItem?> DequeueAsync(bool isOverflow = false);
    Task<int> GetQueueLengthAsync(bool isOverflow = false);
    Task<IEnumerable<QueueItem>> GetAllQueuedItemsAsync(bool isOverflow = false);
    Task RemoveAsync(Guid chatSessionId, bool isOverflow = false);
}

