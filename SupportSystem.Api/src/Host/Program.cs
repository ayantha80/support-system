using SupportChat.Application.Interfaces;
using SupportChat.Domain.Services;
using SupportChat.Infrastructure.Data;
using SupportChat.Infrastructure.HostedServices;
using SupportChat.Infrastructure.Repositories;
using SupportChat.Infrastructure.TimeProvider;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SupportChat.Application.Features.ChatSessions.CreateChatSession.CreateChatSessionCommandHandler).Assembly));

// Register SupportChat module repositories
builder.Services.AddSingleton<IChatSessionRepository, InMemoryChatSessionRepository>();
builder.Services.AddSingleton<IAgentRepository, InMemoryAgentRepository>();
builder.Services.AddSingleton<ITeamRepository, InMemoryTeamRepository>();
builder.Services.AddSingleton<IQueueRepository, InMemoryQueueRepository>();
builder.Services.AddSingleton<IShiftRepository, InMemoryShiftRepository>();

// Register time provider
builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();

// Register domain services
builder.Services.AddSingleton<CapacityCalculator>();
builder.Services.AddSingleton<AssignmentStrategy>();
builder.Services.AddSingleton<QueueRulesService>();
builder.Services.AddSingleton<PollMonitorService>(sp => 
    new PollMonitorService(TimeSpan.FromSeconds(3))); // 3 seconds threshold for 3 missing polls
builder.Services.AddSingleton<ShiftService>();

// Register background services
builder.Services.AddHostedService<AssignmentBackgroundService>();
builder.Services.AddHostedService<PollMonitorBackgroundService>();

var app = builder.Build();

// Seed initial data
using (var scope = app.Services.CreateScope())
{
    var teamRepo = scope.ServiceProvider.GetRequiredService<ITeamRepository>();
    var agentRepo = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
    var shiftRepo = scope.ServiceProvider.GetRequiredService<IShiftRepository>();
    await SeedData.SeedAsync(teamRepo, agentRepo, shiftRepo);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

