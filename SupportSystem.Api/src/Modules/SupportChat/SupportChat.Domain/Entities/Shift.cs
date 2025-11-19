using SupportChat.Domain.Enums;

namespace SupportChat.Domain.Entities;

public class Shift
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public ShiftType ShiftType { get; set; }
    public TimeSpan StartsAt { get; set; }
    public TimeSpan EndsAt { get; set; }

    public Shift()
    {
        Id = Guid.NewGuid();
    }

    public bool IsActive(TimeSpan currentTime)
    {
        if (StartsAt <= EndsAt)
        {
            return currentTime >= StartsAt && currentTime < EndsAt;
        }
        else // Overnight shift
        {
            return currentTime >= StartsAt || currentTime < EndsAt;
        }
    }
}

