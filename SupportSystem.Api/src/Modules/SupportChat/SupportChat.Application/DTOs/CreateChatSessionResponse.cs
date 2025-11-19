using SupportChat.Domain.Enums;

namespace SupportChat.Application.DTOs;

public class CreateChatSessionResponse
{
    public Guid ChatSessionId { get; set; }
    public SessionStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsOverflow { get; set; }
}

