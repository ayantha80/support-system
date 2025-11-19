using SupportChat.Domain.Enums;

namespace SupportChat.Domain.Entities;

public class ChatSession
{
    public Guid Id { get; set; }
    public SessionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastPollAt { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public bool IsOverflow { get; set; }
    public Guid? TeamId { get; set; }

    public ChatSession()
    {
        Id = Guid.NewGuid();
        Status = SessionStatus.Requested;
        CreatedAt = DateTime.UtcNow;
    }
}

