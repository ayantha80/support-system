namespace SupportChat.Application.Interfaces;

public interface ITimeProvider
{
    DateTime UtcNow { get; }
    TimeSpan CurrentTime { get; }
}

