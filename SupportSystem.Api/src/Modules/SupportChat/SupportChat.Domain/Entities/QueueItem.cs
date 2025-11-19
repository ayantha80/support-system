namespace SupportChat.Domain.Entities;

public class QueueItem
{
    public Guid ChatSessionId { get; set; }
    public DateTime EnqueuedAt { get; set; }
    public bool IsOverflow { get; set; }

    public QueueItem(Guid chatSessionId, bool isOverflow = false)
    {
        ChatSessionId = chatSessionId;
        EnqueuedAt = DateTime.UtcNow;
        IsOverflow = isOverflow;
    }
}

