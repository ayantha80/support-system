using SupportChat.Application.Interfaces;

namespace SupportChat.Infrastructure.TimeProvider;

public class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public TimeSpan CurrentTime => DateTime.UtcNow.TimeOfDay;
}

