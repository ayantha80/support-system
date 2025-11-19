using Moq;
using SupportChat.Application.DTOs;
using SupportChat.Application.Features.ChatSessions.CreateChatSession;
using SupportChat.Application.Interfaces;
using SupportChat.Domain.Entities;
using SupportChat.Domain.Enums;
using SupportChat.Domain.Services;

namespace SupportSystem.Test;

public class CreateChatSessionCommandHandlerTests
{
    private readonly Mock<IChatSessionRepository> _chatSessionRepositoryMock;
    private readonly Mock<IQueueRepository> _queueRepositoryMock;
    private readonly Mock<ITeamRepository> _teamRepositoryMock;
    private readonly Mock<IShiftRepository> _shiftRepositoryMock;
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly QueueRulesService _queueRulesService;
    private readonly ShiftService _shiftService;
    private readonly CapacityCalculator _capacityCalculator;
    private readonly CreateChatSessionCommandHandler _handler;

    public CreateChatSessionCommandHandlerTests()
    {
        _chatSessionRepositoryMock = new Mock<IChatSessionRepository>();
        _queueRepositoryMock = new Mock<IQueueRepository>();
        _teamRepositoryMock = new Mock<ITeamRepository>();
        _shiftRepositoryMock = new Mock<IShiftRepository>();
        _timeProviderMock = new Mock<ITimeProvider>();
        _capacityCalculator = new CapacityCalculator();
        _queueRulesService = new QueueRulesService(_capacityCalculator);
        _shiftService = new ShiftService();

        _handler = new CreateChatSessionCommandHandler(
            _chatSessionRepositoryMock.Object,
            _queueRepositoryMock.Object,
            _teamRepositoryMock.Object,
            _shiftRepositoryMock.Object,
            _timeProviderMock.Object,
            _queueRulesService,
            _shiftService);
    }

    [Fact]
    public async Task Handle_NoActiveTeamOutsideOfficeHours_ShouldRefuseSession()
    {
        // Arrange
        var command = new CreateChatSessionCommand { UserId = "user123" };
        var currentTime = new TimeSpan(22, 0, 0); // 10 PM - outside office hours
        var sessionId = Guid.NewGuid();

        _timeProviderMock.Setup(x => x.CurrentTime).Returns(currentTime);
        _teamRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Team>());
        _shiftRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Shift>());

        _chatSessionRepositoryMock
            .Setup(x => x.AddAsync(It.Is<ChatSession>(s => s.Status == SessionStatus.Refused)))
            .Callback<ChatSession>(s => s.Id = sessionId)
            .ReturnsAsync((ChatSession s) => s);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sessionId, result.ChatSessionId);
        Assert.Equal(SessionStatus.Refused, result.Status);
        Assert.Contains("No active team available", result.Message);
        Assert.False(result.IsOverflow);

        _chatSessionRepositoryMock.Verify(x => x.AddAsync(It.Is<ChatSession>(s => s.Status == SessionStatus.Refused)), Times.Once);
        _queueRepositoryMock.Verify(x => x.EnqueueAsync(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ActiveTeamWithAvailableCapacity_ShouldQueueInMainQueue()
    {
        // Arrange
        var command = new CreateChatSessionCommand { UserId = "user123" };
        var currentTime = new TimeSpan(10, 0, 0); // 10 AM - office hours
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var mainTeam = new Team
        {
            Id = teamId,
            Name = "Main Team",
            IsOverflowTeam = false,
            Agents = new List<Agent> { new Agent { SeniorityLevel = SeniorityLevel.MidLevel } }
        };

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            StartsAt = new TimeSpan(9, 0, 0),
            EndsAt = new TimeSpan(17, 0, 0)
        };

        _timeProviderMock.Setup(x => x.CurrentTime).Returns(currentTime);
        _teamRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Team> { mainTeam });
        _shiftRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Shift> { shift });
        _teamRepositoryMock.Setup(x => x.GetOverflowTeamAsync()).ReturnsAsync((Team?)null);
        // MidLevel agent has capacity 6, max queue length = 9
        // Set queue lengths below capacity to ensure acceptance
        _queueRepositoryMock.Setup(x => x.GetQueueLengthAsync(false)).ReturnsAsync(5);
        _queueRepositoryMock.Setup(x => x.GetQueueLengthAsync(true)).ReturnsAsync(0);

        ChatSession? capturedSession = null;
        _chatSessionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ChatSession>()))
            .Callback<ChatSession>(s =>
            {
                s.Id = sessionId;
                capturedSession = s;
            })
            .ReturnsAsync((ChatSession s) => s);

        _chatSessionRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ChatSession>()))
            .Returns(Task.CompletedTask);

        _queueRepositoryMock
            .Setup(x => x.EnqueueAsync(sessionId, false))
            .ReturnsAsync(new QueueItem(sessionId, false));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sessionId, result.ChatSessionId);
        Assert.Equal(SessionStatus.Queued, result.Status);
        Assert.Equal("Chat session queued.", result.Message);
        Assert.False(result.IsOverflow);

        _chatSessionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ChatSession>()), Times.Once);
        _queueRepositoryMock.Verify(x => x.EnqueueAsync(sessionId, false), Times.Once);
        _chatSessionRepositoryMock.Verify(x => x.UpdateAsync(It.Is<ChatSession>(s => s.Status == SessionStatus.Queued)), Times.Once);
    }

    [Fact]
    public async Task Handle_MainQueueFullWithOverflowAvailable_ShouldQueueInOverflow()
    {
        // Arrange
        var command = new CreateChatSessionCommand { UserId = "user123" };
        var currentTime = new TimeSpan(10, 0, 0); // 10 AM - office hours
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var overflowTeamId = Guid.NewGuid();

        var mainTeam = new Team
        {
            Id = teamId,
            Name = "Main Team",
            IsOverflowTeam = false,
            Agents = new List<Agent> { new Agent { SeniorityLevel = SeniorityLevel.MidLevel } }
        };

        var overflowTeam = new Team
        {
            Id = overflowTeamId,
            Name = "Overflow Team",
            IsOverflowTeam = true,
            Agents = new List<Agent> { new Agent { SeniorityLevel = SeniorityLevel.MidLevel } }
        };

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            StartsAt = new TimeSpan(9, 0, 0),
            EndsAt = new TimeSpan(17, 0, 0)
        };

        _timeProviderMock.Setup(x => x.CurrentTime).Returns(currentTime);
        _teamRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Team> { mainTeam, overflowTeam });
        _shiftRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Shift> { shift });
        _teamRepositoryMock.Setup(x => x.GetOverflowTeamAsync()).ReturnsAsync(overflowTeam);
        // MidLevel agent has capacity 6, max queue length = 9
        // Set main queue above capacity, overflow below capacity
        _queueRepositoryMock.Setup(x => x.GetQueueLengthAsync(false)).ReturnsAsync(10); // Main queue full
        _queueRepositoryMock.Setup(x => x.GetQueueLengthAsync(true)).ReturnsAsync(5); // Overflow has space

        _chatSessionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ChatSession>()))
            .Callback<ChatSession>(s => s.Id = sessionId)
            .ReturnsAsync((ChatSession s) => s);

        _chatSessionRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ChatSession>()))
            .Returns(Task.CompletedTask);

        _queueRepositoryMock
            .Setup(x => x.EnqueueAsync(sessionId, true))
            .ReturnsAsync(new QueueItem(sessionId, true));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sessionId, result.ChatSessionId);
        Assert.Equal(SessionStatus.Queued, result.Status);
        Assert.Equal("Chat session queued in overflow team.", result.Message);
        Assert.True(result.IsOverflow);

        _chatSessionRepositoryMock.Verify(x => x.AddAsync(It.Is<ChatSession>(s => s.IsOverflow == true)), Times.Once);
        _queueRepositoryMock.Verify(x => x.EnqueueAsync(sessionId, true), Times.Once);
        _chatSessionRepositoryMock.Verify(x => x.UpdateAsync(It.Is<ChatSession>(s => s.Status == SessionStatus.Queued)), Times.Once);
    }

    [Fact]
    public async Task Handle_BothQueuesFull_ShouldRefuseSession()
    {
        // Arrange
        var command = new CreateChatSessionCommand { UserId = "user123" };
        var currentTime = new TimeSpan(10, 0, 0); // 10 AM - office hours
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var overflowTeamId = Guid.NewGuid();

        var mainTeam = new Team
        {
            Id = teamId,
            Name = "Main Team",
            IsOverflowTeam = false,
            Agents = new List<Agent> { new Agent { SeniorityLevel = SeniorityLevel.MidLevel } }
        };

        var overflowTeam = new Team
        {
            Id = overflowTeamId,
            Name = "Overflow Team",
            IsOverflowTeam = true,
            Agents = new List<Agent> { new Agent { SeniorityLevel = SeniorityLevel.MidLevel } }
        };

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            StartsAt = new TimeSpan(9, 0, 0),
            EndsAt = new TimeSpan(17, 0, 0)
        };

        _timeProviderMock.Setup(x => x.CurrentTime).Returns(currentTime);
        _teamRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Team> { mainTeam, overflowTeam });
        _shiftRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Shift> { shift });
        _teamRepositoryMock.Setup(x => x.GetOverflowTeamAsync()).ReturnsAsync(overflowTeam);
        // MidLevel agent has capacity 6, max queue length = 9
        // Set queue lengths above capacity to ensure refusal
        _queueRepositoryMock.Setup(x => x.GetQueueLengthAsync(false)).ReturnsAsync(10); // Main queue full
        _queueRepositoryMock.Setup(x => x.GetQueueLengthAsync(true)).ReturnsAsync(10); // Overflow also full
        

        _chatSessionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ChatSession>()))
            .Callback<ChatSession>(s => s.Id = sessionId)
            .ReturnsAsync((ChatSession s) => s);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sessionId, result.ChatSessionId);
        Assert.Equal(SessionStatus.Refused, result.Status);
        Assert.Equal("Queue is full. Chat session refused.", result.Message);
        Assert.False(result.IsOverflow);

        _chatSessionRepositoryMock.Verify(x => x.AddAsync(It.Is<ChatSession>(s => s.Status == SessionStatus.Refused)), Times.Once);
        _queueRepositoryMock.Verify(x => x.EnqueueAsync(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
        _chatSessionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ChatSession>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoActiveTeamButInsideOfficeHours_ShouldUseFirstTeam()
    {
        // Arrange
        var command = new CreateChatSessionCommand { UserId = "user123" };
        var currentTime = new TimeSpan(10, 0, 0); // 10 AM - office hours
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var mainTeam = new Team
        {
            Id = teamId,
            Name = "Main Team",
            IsOverflowTeam = false,
            Agents = new List<Agent> { new Agent { SeniorityLevel = SeniorityLevel.MidLevel } }
        };

        _timeProviderMock.Setup(x => x.CurrentTime).Returns(currentTime);
        _teamRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Team> { mainTeam });
        _shiftRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Shift>());
        _teamRepositoryMock.Setup(x => x.GetOverflowTeamAsync()).ReturnsAsync((Team?)null);
        // MidLevel agent has capacity 6, max queue length = 9
        // Set queue lengths below capacity to ensure acceptance
        _queueRepositoryMock.Setup(x => x.GetQueueLengthAsync(false)).ReturnsAsync(5);
        _queueRepositoryMock.Setup(x => x.GetQueueLengthAsync(true)).ReturnsAsync(0);

        _chatSessionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ChatSession>()))
            .Callback<ChatSession>(s => s.Id = sessionId)
            .ReturnsAsync((ChatSession s) => s);

        _chatSessionRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ChatSession>()))
            .Returns(Task.CompletedTask);

        _queueRepositoryMock
            .Setup(x => x.EnqueueAsync(sessionId, false))
            .ReturnsAsync(new QueueItem(sessionId, false));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SessionStatus.Queued, result.Status);
    }

    [Fact]
    public async Task Handle_OutsideOfficeHoursWithActiveTeam_ShouldProcessNormally()
    {
        // Arrange
        var command = new CreateChatSessionCommand { UserId = "user123" };
        var currentTime = new TimeSpan(22, 0, 0); // 10 PM - outside office hours
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var mainTeam = new Team
        {
            Id = teamId,
            Name = "Main Team",
            IsOverflowTeam = false,
            Agents = new List<Agent> { new Agent { SeniorityLevel = SeniorityLevel.MidLevel } }
        };

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            StartsAt = new TimeSpan(20, 0, 0),
            EndsAt = new TimeSpan(8, 0, 0) // Overnight shift
        };

        _timeProviderMock.Setup(x => x.CurrentTime).Returns(currentTime);
        _teamRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Team> { mainTeam });
        _shiftRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Shift> { shift });
        _teamRepositoryMock.Setup(x => x.GetOverflowTeamAsync()).ReturnsAsync((Team?)null);
        // MidLevel agent has capacity 6, max queue length = 9
        // Set queue lengths below capacity to ensure acceptance
        _queueRepositoryMock.Setup(x => x.GetQueueLengthAsync(false)).ReturnsAsync(5);
        _queueRepositoryMock.Setup(x => x.GetQueueLengthAsync(true)).ReturnsAsync(0);

        _chatSessionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ChatSession>()))
            .Callback<ChatSession>(s => s.Id = sessionId)
            .ReturnsAsync((ChatSession s) => s);

        _chatSessionRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ChatSession>()))
            .Returns(Task.CompletedTask);

        _queueRepositoryMock
            .Setup(x => x.EnqueueAsync(sessionId, false))
            .ReturnsAsync(new QueueItem(sessionId, false));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SessionStatus.Queued, result.Status);
    }
}

