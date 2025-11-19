namespace SupportChat.Domain.Entities;

public class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Agent> Agents { get; set; } = new();
    public bool IsOverflowTeam { get; set; }

    public Team()
    {
        Id = Guid.NewGuid();
    }
}

