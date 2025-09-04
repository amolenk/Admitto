# Admitto - Open Source Ticketing System

**ALWAYS follow these instructions first and only fallback to additional search and context gathering if the information in the instructions is incomplete or found to be in error.**

Admitto is a .NET 9 microservices ticketing system for small, free events with a Next.js admin interface, built using clean architecture principles and .NET Aspire for orchestration.

## Working Effectively

### Prerequisites and Installation
- .NET 9 SDK is required (already available in the environment)
- Install pnpm globally: `npm install -g pnpm`
- NEVER CANCEL builds or long-running commands - they take time but complete successfully

### Bootstrap and Build Process
1. **Restore dependencies** - takes ~45 seconds, NEVER CANCEL:
   ```bash
   dotnet restore
   ```
   
2. **Build core .NET projects** - takes ~3 seconds:
   ```bash
   dotnet build --no-restore
   ```
   **Note**: Build will show 31 errors in integration tests - this is expected due to incomplete domain model

3. **Install UI dependencies** - takes ~21 seconds:
   ```bash
   cd src/Admitto.UI.Admin && pnpm install
   ```

4. **Run unit tests** - takes ~1 second:
   ```bash
   dotnet test tests/Admitto.Domain.Tests --no-build --no-restore
   ```

### Individual Project Build Status
✅ **Successfully build**: Domain, Application, Infrastructure, API, CLI, Worker, Migration, JobRunner  
❌ **Integration tests fail**: 31 compilation errors due to missing domain types  
❌ **Next.js UI build fails**: Network restrictions prevent Google Fonts access  
❌ **Aspire AppHost fails**: Requires complex container orchestration (Kubernetes/DCP)

### What You CAN Test and Run

**CLI Tool** (fully functional):
```bash
dotnet run --project src/Admitto.Cli --no-build -- --help
dotnet run --project src/Admitto.Cli --no-build -- attendee --help
```

**Domain Tests** (pass successfully):
```bash
dotnet test tests/Admitto.Domain.Tests --no-build --no-restore
```

### What DOES NOT Work (Document as Known Issues)

**API Service**: Requires Microsoft Graph configuration
- Error: MicrosoftGraphOptions ClientId/ClientSecret required
- Configuration needed in appsettings

**Next.js UI**: Network restrictions prevent build
- Error: Cannot fetch fonts from Google Fonts
- Would work in unrestricted development environment

**Aspire AppHost**: Complex orchestration requirements
- Requires: PostgreSQL, Azure Service Bus, OpenFGA, Keycloak, MailDev
- Use individual project testing instead

**Integration Tests**: Missing domain model components
- Missing types: TicketTypeId, Registration, EmailSettingsDto, TeamMemberDto, TicketTypeDto, AccessTokenHandler
- Missing "Registrations" namespace in Application layer

## Architecture Overview

### Core Projects
- **Admitto.Domain**: Business entities and domain logic
- **Admitto.Application**: Use cases, command/query handlers, projections
- **Admitto.Infrastructure**: Data persistence, messaging, external services
- **Admitto.Api**: REST API endpoints using minimal APIs
- **Admitto.AppHost**: .NET Aspire orchestration for all services

### Supporting Projects
- **Admitto.Cli**: Command-line tool for administration
- **Admitto.Worker**: Background service processing
- **Admitto.JobRunner**: Job scheduling and execution
- **Admitto.Migration**: Database schema management
- **Admitto.UI.Admin**: Next.js React admin interface
- **Admitto.ServiceDefaults**: Shared configuration and observability

### Key Technologies
- **.NET 9**: Primary runtime
- **Entity Framework Core**: Data access
- **PostgreSQL**: Database
- **Azure Service Bus**: Messaging
- **OpenFGA**: Authorization
- **Keycloak**: Authentication  
- **Next.js**: Frontend framework
- **.NET Aspire**: Cloud-native orchestration

## Validation and Testing

### Always run these commands before committing:
1. `dotnet restore` - ensure dependencies are current
2. `dotnet build --no-restore` - verify core projects compile
3. `dotnet test tests/Admitto.Domain.Tests --no-build --no-restore` - run passing tests

### Manual Validation Scenarios
**CLI Tool Validation**:
```bash
# Test help system
dotnet run --project src/Admitto.Cli --no-build -- --help

# Test attendee commands
dotnet run --project src/Admitto.Cli --no-build -- attendee --help
```

**Build Time Validation**:
- Domain tests complete in ~1 second
- Core build completes in ~3 seconds  
- Full restore takes ~45 seconds (expected)

### Do NOT attempt to validate:
- Full application orchestration (requires complex infrastructure)
- Integration tests (known to fail due to missing domain model)
- Next.js UI build (network restrictions)
- API startup without proper configuration

## Development Workflows

### Making Changes to Domain/Application
1. Build affected projects: `dotnet build src/Admitto.Domain src/Admitto.Application --no-restore`
2. Run domain tests: `dotnet test tests/Admitto.Domain.Tests --no-build --no-restore`
3. Test CLI functionality if applicable

### Working with CLI
1. Build CLI: `dotnet build src/Admitto.Cli --no-restore`
2. Test commands: `dotnet run --project src/Admitto.Cli --no-build -- <command> --help`

### Infrastructure and Deployment
- **Azure**: Uses Bicep templates in `/infra` directory
- **Azure Developer CLI**: `azure.yaml` configures deployment
- **GitHub Actions**: Automated workflows in `.github/workflows`

## Common Issues and Workarounds

### Integration Test Compilation Errors
**Status**: Expected - do not attempt to fix
**Cause**: Missing domain model types (TicketTypeId, Registration, etc.)
**Workaround**: Focus on domain tests which pass successfully

### Microsoft Graph Configuration Required
**Status**: Expected for API startup
**Cause**: Missing ClientId/ClientSecret in configuration
**Workaround**: Use CLI tool for testing business logic

### Network Access Restrictions
**Status**: Expected in sandboxed environments
**Cause**: Cannot access external resources (Google Fonts, etc.)
**Workaround**: Validate individual .NET projects instead

### Aspire AppHost Complexity
**Status**: Expected - requires full infrastructure
**Cause**: Needs PostgreSQL, Service Bus, OpenFGA, Keycloak containers
**Workaround**: Test individual projects separately

## Quick Reference

### Build Timeouts (NEVER CANCEL)
- Restore: 60+ seconds recommended timeout
- Build: 30+ seconds recommended timeout  
- Tests: 30+ seconds recommended timeout

### Key Files
- `Admitto.sln`: Main solution file
- `azure.yaml`: Azure Developer CLI configuration
- `src/Admitto.AppHost/AppHost.cs`: Service orchestration
- `JOBS.md`: Background jobs documentation
- `next-steps.md`: Azure deployment guide

### Directory Structure
```
src/
├── Admitto.Domain/          # Core business logic
├── Admitto.Application/     # Use cases and handlers  
├── Admitto.Infrastructure/  # External concerns
├── Admitto.Api/            # REST API
├── Admitto.AppHost/        # Aspire orchestration
├── Admitto.Cli/            # Command-line tool
├── Admitto.Worker/         # Background services
├── Admitto.UI.Admin/       # Next.js admin interface
tests/
├── Admitto.Domain.Tests/        # ✅ Working unit tests
├── Admitto.IntegrationTests/    # ❌ Compilation issues
infra/                      # Azure Bicep templates
```

### Remember
- Always validate CLI tool functionality after domain changes
- Domain tests are reliable indicator of core business logic health
- Focus on individual project validation rather than full orchestration
- Integration tests are incomplete - do not spend time fixing them