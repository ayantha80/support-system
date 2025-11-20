# Support System API

A modern support chat management system built with .NET 8, implementing Clean Architecture principles and CQRS pattern for efficient chat session handling, queue management, and agent assignment.

## Architecture

This project follows **Clean Architecture** (Onion Architecture) principles, organized as a **modular monolith** with clear separation of concerns:

### Layer Structure

```
SupportSystem.Api/
├── src/
│   ├── Host/                          # Application entry point
│   └── Modules/
│       └── SupportChat/
│           ├── SupportChat.Domain/    # Core business entities and domain logic
│           ├── SupportChat.Application/# Application layer (CQRS handlers, DTOs)
│           ├── SupportChat.Infrastructure/ # Data access, repositories, background services
│           └── SupportChat.Api/       # API controllers
└── SupportSystem.Test/                # Unit tests
```

### Architecture Principles

- **Domain Layer**: Contains business entities, enums, and domain services. No external dependencies.
- **Application Layer**: Implements CQRS pattern with MediatR. Contains command/query handlers, DTOs, and application interfaces.
- **Infrastructure Layer**: Implements data persistence (currently in-memory repositories), background services, and external integrations.
- **API Layer**: RESTful API controllers that handle HTTP requests and delegate to application layer via MediatR.

### Design Patterns

- **CQRS (Command Query Responsibility Segregation)**: Separates read and write operations using MediatR
- **Repository Pattern**: Abstracts data access through interfaces
- **Dependency Injection**: Full DI container support with service lifetime management
- **Background Services**: Hosted services for asynchronous processing (chat assignment, poll monitoring)

## 🛠️ Technologies

### Core Framework
- **.NET 8.0** - Latest LTS version of .NET
- **ASP.NET Core Web API** - RESTful API framework

### Key Libraries
- **MediatR 12.2.0** - CQRS pattern implementation and in-process messaging
- **Swashbuckle.AspNetCore 6.4.0** - Swagger/OpenAPI documentation

### Testing
- **xUnit 2.5.3** - Unit testing framework
- **Moq 4.20.70** - Mocking framework for unit tests
- **Microsoft.NET.Test.Sdk** - Test SDK
- **coverlet.collector** - Code coverage collection

### Data Storage
- **In-Memory Repositories** - Currently using in-memory data storage (easily replaceable with database implementations)

## 📋 Features

### Chat Session Management
- Create new chat sessions with automatic queue assignment
- Session status tracking (Queued, Active, Assigned, Inactive, Refused)
- Poll-based session status updates
- Automatic session inactivity detection

### Queue Management
- Main queue for regular team assignments
- Overflow queue for handling capacity overflow
- Intelligent queue routing based on team capacity and office hours
- Queue length monitoring

### Team & Agent Management
- Multi-team support with overflow team capability
- Agent seniority levels (Team Lead, Mid-Level, Junior)
- Dynamic capacity calculation based on seniority
- Shift-based team activation

### Background Services
- **AssignmentBackgroundService**: Automatically assigns queued chats to available agents every 5 seconds
- **PollMonitorBackgroundService**: Monitors active sessions for inactivity every 2 seconds

### Business Logic
- Office hours detection
- Shift-based team activation
- Capacity-based queue routing
- Agent concurrency management

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or Rider (recommended)

### Running the Application

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd SupportSystem.Api
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run the application**
   ```bash
   cd src/Host
   dotnet run
   ```

5. **Access Swagger UI**
   - Navigate to `https://localhost:<port>/swagger` (port shown in console output)

### Running Tests

```bash
dotnet test
```

## 📡 API Endpoints

### Chat Sessions
- `POST /api/chat-sessions` - Create a new chat session
- `GET /api/chat-sessions/{id}/poll` - Poll session status

### Status
- `GET /api/status` - Get system status and queue information

## 🔧 Configuration

The application uses in-memory data storage by default. Initial seed data includes:
- 3 regular teams (Team A, Team B, Team C)
- 1 overflow team
- Agents with varying seniority levels
- Shift configurations

## 📦 Project Structure

```
SupportChat.Domain/
  ├── Entities/          # Domain entities (Agent, ChatSession, Team, etc.)
  ├── Enums/             # Domain enumerations
  └── Services/          # Domain services (business logic)

SupportChat.Application/
  ├── DTOs/              # Data transfer objects
  ├── Features/          # CQRS commands and queries
  ├── Handlers/          # Command/Query handlers
  └── Interfaces/        # Application interfaces

SupportChat.Infrastructure/
  ├── Data/              # Seed data
  ├── HostedServices/    # Background services
  ├── Repositories/      # Repository implementations
  └── TimeProvider/      # Time abstraction

SupportChat.Api/
  └── Controllers/       # API controllers
```

## 🧪 Testing

The solution includes unit tests using xUnit and Moq. Tests are located in the `SupportSystem.Test` project.

## 🔄 Future Enhancements

- Database persistence (Entity Framework Core, SQL Server, PostgreSQL, etc.)
- Real-time communication (SignalR)
- Authentication and authorization
- Advanced analytics and reporting
- Multi-tenant support
- Message history and persistence

[Add contribution guidelines if applicable]

