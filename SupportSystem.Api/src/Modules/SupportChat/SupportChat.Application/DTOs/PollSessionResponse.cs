using SupportChat.Domain.Enums;

namespace SupportChat.Application.DTOs;

public class PollSessionResponse
{
    public Guid ChatSessionId { get; set; }
    public SessionStatus Status { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public string? AssignedAgentName { get; set; }
    public bool IsOverflow { get; set; }
}

