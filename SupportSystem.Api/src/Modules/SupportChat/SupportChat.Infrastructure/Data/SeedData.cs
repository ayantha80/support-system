using SupportChat.Application.Interfaces;
using SupportChat.Domain.Entities;
using SupportChat.Domain.Enums;

namespace SupportChat.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(
        ITeamRepository teamRepository,
        IAgentRepository agentRepository,
        IShiftRepository shiftRepository)
    {
        // Create Teams
        var teamA = new Team { Name = "Team A", IsOverflowTeam = false };
        var teamB = new Team { Name = "Team B", IsOverflowTeam = false };
        var teamC = new Team { Name = "Team C", IsOverflowTeam = false };
        var overflowTeam = new Team { Name = "Overflow Team", IsOverflowTeam = true };

        await teamRepository.AddAsync(teamA);
        await teamRepository.AddAsync(teamB);
        await teamRepository.AddAsync(teamC);
        await teamRepository.AddAsync(overflowTeam);

        // Team A: 1 Team Lead, 2 Mid-Level, 1 Junior
        var teamLeadA = new Agent
        {
            Name = "Team Lead A",
            SeniorityLevel = SeniorityLevel.TeamLead,
            TeamId = teamA.Id
        };
        teamLeadA.CalculateMaxConcurrency();

        var midA1 = new Agent
        {
            Name = "Mid-Level A1",
            SeniorityLevel = SeniorityLevel.MidLevel,
            TeamId = teamA.Id
        };
        midA1.CalculateMaxConcurrency();

        var midA2 = new Agent
        {
            Name = "Mid-Level A2",
            SeniorityLevel = SeniorityLevel.MidLevel,
            TeamId = teamA.Id
        };
        midA2.CalculateMaxConcurrency();

        var juniorA = new Agent
        {
            Name = "Junior A",
            SeniorityLevel = SeniorityLevel.Junior,
            TeamId = teamA.Id
        };
        juniorA.CalculateMaxConcurrency();

        await agentRepository.AddAsync(teamLeadA);
        await agentRepository.AddAsync(midA1);
        await agentRepository.AddAsync(midA2);
        await agentRepository.AddAsync(juniorA);

        // Team B: 1 Senior, 1 Mid-Level, 2 Junior
        var seniorB = new Agent
        {
            Name = "Senior B",
            SeniorityLevel = SeniorityLevel.Senior,
            TeamId = teamB.Id
        };
        seniorB.CalculateMaxConcurrency();

        var midB = new Agent
        {
            Name = "Mid-Level B",
            SeniorityLevel = SeniorityLevel.MidLevel,
            TeamId = teamB.Id
        };
        midB.CalculateMaxConcurrency();

        var juniorB1 = new Agent
        {
            Name = "Junior B1",
            SeniorityLevel = SeniorityLevel.Junior,
            TeamId = teamB.Id
        };
        juniorB1.CalculateMaxConcurrency();

        var juniorB2 = new Agent
        {
            Name = "Junior B2",
            SeniorityLevel = SeniorityLevel.Junior,
            TeamId = teamB.Id
        };
        juniorB2.CalculateMaxConcurrency();

        await agentRepository.AddAsync(seniorB);
        await agentRepository.AddAsync(midB);
        await agentRepository.AddAsync(juniorB1);
        await agentRepository.AddAsync(juniorB2);

        // Team C: 2 Mid-Level (night shift)
        var midC1 = new Agent
        {
            Name = "Mid-Level C1",
            SeniorityLevel = SeniorityLevel.MidLevel,
            TeamId = teamC.Id
        };
        midC1.CalculateMaxConcurrency();

        var midC2 = new Agent
        {
            Name = "Mid-Level C2",
            SeniorityLevel = SeniorityLevel.MidLevel,
            TeamId = teamC.Id
        };
        midC2.CalculateMaxConcurrency();

        await agentRepository.AddAsync(midC1);
        await agentRepository.AddAsync(midC2);

        // Overflow Team: 6 Juniors
        for (int i = 1; i <= 6; i++)
        {
            var overflowAgent = new Agent
            {
                Name = $"Overflow Agent {i}",
                SeniorityLevel = SeniorityLevel.Junior,
                TeamId = overflowTeam.Id,
                IsOnOverflowTeam = true
            };
            overflowAgent.CalculateMaxConcurrency();
            await agentRepository.AddAsync(overflowAgent);
        }

        // Create Shifts
        // Morning: 08:00 - 16:00 (Team A)
        var morningShiftA = new Shift
        {
            TeamId = teamA.Id,
            ShiftType = ShiftType.Morning,
            StartsAt = new TimeSpan(8, 0, 0),
            EndsAt = new TimeSpan(16, 0, 0)
        };
        await shiftRepository.AddAsync(morningShiftA);

        // Afternoon: 16:00 - 00:00 (Team B)
        var afternoonShiftB = new Shift
        {
            TeamId = teamB.Id,
            ShiftType = ShiftType.Afternoon,
            StartsAt = new TimeSpan(16, 0, 0),
            EndsAt = new TimeSpan(0, 0, 0)
        };
        await shiftRepository.AddAsync(afternoonShiftB);

        // Night: 00:00 - 08:00 (Team C)
        var nightShiftC = new Shift
        {
            TeamId = teamC.Id,
            ShiftType = ShiftType.Night,
            StartsAt = new TimeSpan(0, 0, 0),
            EndsAt = new TimeSpan(8, 0, 0)
        };
        await shiftRepository.AddAsync(nightShiftC);

        // Assign shifts to agents
        teamLeadA.ShiftId = morningShiftA.Id;
        midA1.ShiftId = morningShiftA.Id;
        midA2.ShiftId = morningShiftA.Id;
        juniorA.ShiftId = morningShiftA.Id;

        seniorB.ShiftId = afternoonShiftB.Id;
        midB.ShiftId = afternoonShiftB.Id;
        juniorB1.ShiftId = afternoonShiftB.Id;
        juniorB2.ShiftId = afternoonShiftB.Id;

        midC1.ShiftId = nightShiftC.Id;
        midC2.ShiftId = nightShiftC.Id;

        await agentRepository.UpdateAsync(teamLeadA);
        await agentRepository.UpdateAsync(midA1);
        await agentRepository.UpdateAsync(midA2);
        await agentRepository.UpdateAsync(juniorA);
        await agentRepository.UpdateAsync(seniorB);
        await agentRepository.UpdateAsync(midB);
        await agentRepository.UpdateAsync(juniorB1);
        await agentRepository.UpdateAsync(juniorB2);
        await agentRepository.UpdateAsync(midC1);
        await agentRepository.UpdateAsync(midC2);
    }
}

