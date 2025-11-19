using MediatR;
using SupportChat.Application.DTOs;

namespace SupportChat.Application.Features.ChatSessions.PollSession;

public class PollSessionQuery : IRequest<PollSessionResponse>
{
    public Guid SessionId { get; set; }

    public PollSessionQuery(Guid sessionId)
    {
        SessionId = sessionId;
    }
}


