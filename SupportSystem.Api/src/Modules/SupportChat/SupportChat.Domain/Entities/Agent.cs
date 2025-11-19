using SupportChat.Domain.Enums;

namespace SupportChat.Domain.Entities;

public class Agent
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SeniorityLevel SeniorityLevel { get; set; }
    public int MaxConcurrency { get; private set; }
    public int CurrentActiveChats { get; set; }
    public Guid? ShiftId { get; set; }
    public bool IsOnOverflowTeam { get; set; }
    public Guid TeamId { get; set; }

    public Agent()
    {
        Id = Guid.NewGuid();
        CurrentActiveChats = 0;
    }

    public void CalculateMaxConcurrency()
    {
        var efficiency = SeniorityLevel switch
        {
            SeniorityLevel.Junior => 0.4,
            SeniorityLevel.MidLevel => 0.6,
            SeniorityLevel.Senior => 0.8,
            SeniorityLevel.TeamLead => 0.5,
            _ => 0.4
        };

        MaxConcurrency = (int)(10 * efficiency);
    }

    public bool HasCapacity => CurrentActiveChats < MaxConcurrency;
    public int AvailableCapacity => Math.Max(0, MaxConcurrency - CurrentActiveChats);
}

