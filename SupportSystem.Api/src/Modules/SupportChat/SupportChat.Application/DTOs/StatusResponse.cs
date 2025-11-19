namespace SupportChat.Application.DTOs;

public class StatusResponse
{
    public string? ActiveTeam { get; set; }
    public int TeamCapacity { get; set; }
    public int MaxQueueLength { get; set; }
    public int CurrentQueueLength { get; set; }
    public int OverflowQueueLength { get; set; }
    public int ActiveSessions { get; set; }
    public bool IsOfficeHours { get; set; }
    public List<AgentStatus> Agents { get; set; } = new();
}

public class AgentStatus
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Seniority { get; set; } = string.Empty;
    public int CurrentChats { get; set; }
    public int MaxConcurrency { get; set; }
}

