using MediatR;
using SupportChat.Application.DTOs;

namespace SupportChat.Application.Features.ChatSessions.CreateChatSession;

public class CreateChatSessionCommand : IRequest<CreateChatSessionResponse>
{
    public string? UserId { get; set; }
}


