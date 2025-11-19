using SupportChat.Application.Interfaces;
using SupportChat.Domain.Entities;

namespace SupportChat.Infrastructure.Repositories;

public class InMemoryQueueRepository : IQueueRepository
{
    private readonly List<QueueItem> _mainQueue = new();
    private readonly List<QueueItem> _overflowQueue = new();

    public Task<QueueItem> EnqueueAsync(Guid chatSessionId, bool isOverflow = false)
    {
        var queueItem = new QueueItem(chatSessionId, isOverflow);
        if (isOverflow)
        {
            _overflowQueue.Add(queueItem);
        }
        else
        {
            _mainQueue.Add(queueItem);
        }
        return Task.FromResult(queueItem);
    }

    public Task<QueueItem?> DequeueAsync(bool isOverflow = false)
    {
        var queue = isOverflow ? _overflowQueue : _mainQueue;
        if (!queue.Any())
            return Task.FromResult<QueueItem?>(null);

        var item = queue.OrderBy(q => q.EnqueuedAt).First();
        queue.Remove(item);
        return Task.FromResult<QueueItem?>(item);
    }

    public Task<int> GetQueueLengthAsync(bool isOverflow = false)
    {
        var queue = isOverflow ? _overflowQueue : _mainQueue;
        return Task.FromResult(queue.Count);
    }

    public Task<IEnumerable<QueueItem>> GetAllQueuedItemsAsync(bool isOverflow = false)
    {
        var queue = isOverflow ? _overflowQueue : _mainQueue;
        return Task.FromResult<IEnumerable<QueueItem>>(queue);
    }

    public Task RemoveAsync(Guid chatSessionId, bool isOverflow = false)
    {
        var queue = isOverflow ? _overflowQueue : _mainQueue;
        var item = queue.FirstOrDefault(q => q.ChatSessionId == chatSessionId);
        if (item != null)
        {
            queue.Remove(item);
        }
        return Task.CompletedTask;
    }
}

