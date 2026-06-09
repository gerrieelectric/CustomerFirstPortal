# Setup Guide - Customer First Portal

Welcome to the Customer First Portal project! This guide will help you get your development environment set up.

## Phase 1 Overview

Phase 1 delivers two submission forms (employee→employee, employee→customer-on-behalf), admin review/approval workflow with email notifications, admin list/settings pages, analytics dashboard, and Gerrie integration.

## Prerequisites

- **Node.js**: 20+ ([download](https://nodejs.org/))
- **.NET SDK**: 10+ ([download](https://dotnet.microsoft.com/download))
- **SQL Server**: 2019+ (local, on-prem, or container)
- **Docker & Docker Compose**: For containerized local environment
- **Git**: For version control
- **VS Code**: Recommended editor with C# and Angular extensions

## Quick Start (Docker)

The fastest way to set up the full stack locally:

```bash
# 1. Clone the repo and navigate to it
cd CustomerFirstPortal

# 2. Start all services with docker-compose
docker-compose up -d

# This starts:
# - SQL Server on localhost:1433 (sa/YourStrong!Password123)
# - Azurite (Azure Storage emulator) on localhost:10000

# 3. In a new terminal, set up the backend
cd src/backend
dotnet restore
dotnet ef database update
dotnet run

# API will be at https://localhost:5001

# 4. In another terminal, set up the frontend
cd src/frontend/customer-first-portal
npm install
npm start

# App will be at http://localhost:4200
```

## Manual Setup (Without Docker)

### Backend Setup

```bash
cd src/backend

# Restore NuGet packages
dotnet restore

# Apply EF Core migrations to your SQL Server instance
# Update the connection string in appsettings.Local.json first
dotnet ef database update

# Run the API
dotnet run

# API endpoints available at https://localhost:5001/api
# Swagger UI at https://localhost:5001/swagger
```

**Important**: Update the connection string in `src/backend/Portal.Api/appsettings.Local.json` to match your SQL Server instance.

### Frontend Setup

```bash
cd src/frontend/customer-first-portal

# Install dependencies
npm install

# Start the dev server
npm start

# The app will open at http://localhost:4200
```

### Configure Local Environment

Before running, create these configuration files with your local values:

**Backend** (`src/backend/Portal.Api/appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=CustomerFirstPortal;User Id=sa;Password=YOUR_PASSWORD;Encrypt=false;"
  },
  "Entra": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id"
  },
  "AzureStorage": {
    "BlobConnectionString": "DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net"
  }
}
```

**Frontend** (`src/frontend/customer-first-portal/src/environments/environment.ts`):
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api',
  auth: {
    authority: 'https://login.microsoftonline.com/YOUR_TENANT_ID',
    clientId: 'YOUR_CLIENT_ID',
    // ... (see template for full config)
  }
};
```

## Architecture Overview

### Backend (ASP.NET Core 10)

```
Portal.Api/          ← HTTP endpoints, middleware, DI setup
Portal.Application/  ← Business logic, commands, validators
Portal.Domain/       ← Core entities, enums, value objects
Portal.Infrastructure/ ← EF Core, Storage, Notifications, Gerrie
Portal.Tests/        ← Unit and integration tests
```

**Key patterns:**
- Commands & Queries (CQRS-inspired)
- Aggregate roots for complex logic
- Dependency injection for all services
- Result<T> pattern for error handling

### Frontend (Angular 19)

```
app/
├── core/           ← Auth, guards, interceptors, shared services
├── features/       ← Feature modules
│   ├── submissions/   ← Form pages
│   ├── admin-review/  ← Review & approval UI
│   ├── admin-list/    ← Sortable, filterable results
│   ├── settings/      ← Admin configuration
│   └── analytics/     ← KPI dashboard
└── shared/         ← Pipes, directives, utility components
```

**Key patterns:**
- Typed HTTP services with interceptors
- Reactive programming (RxJS)
- Smart/dumb component pattern
- Route guards for RBAC

## Development Workflow

### Adding a New Submission Form Field

1. **Domain**: Update `Submission` entity in `Portal.Domain/Entities/`
2. **Database**: Create EF migration: `dotnet ef migrations add AddFieldName`
3. **Application**: Update submission handler validation
4. **API**: Update DTOs and endpoint
5. **Frontend**: Add form control and update service

### Adding a New Admin Permission

1. **Domain**: Define policy in auth setup
2. **API**: Add `[Authorize(Policy = "AdminOnly")]` to endpoint
3. **Frontend**: Add role check in `AuthGuard`
4. **Frontend**: Conditionally show in navigation based on role

### Database Migrations

```bash
# Create migration
cd src/backend
dotnet ef migrations add DescriptiveNameHere

# Apply migration
dotnet ef database update

# View pending migrations
dotnet ef migrations has-pending-updates
```

## Running Tests

### Backend Tests

```bash
cd src/backend
dotnet test
```

### Frontend Tests

```bash
cd src/frontend/customer-first-portal
npm test
```

## Debugging

### Backend (VS Code)

1. Open `src/backend` folder in VS Code
2. Install C# extension (Omnisharp)
3. Press `F5` to launch debugger
4. Set breakpoints in code

### Frontend (Chrome DevTools)

1. Run `npm start`
2. Open Chrome DevTools (F12)
3. Go to Sources tab, breakpoints work normally

## Stopping Services

```bash
# If using docker-compose
docker-compose down

# Stop running processes
# Backend: Ctrl+C in terminal
# Frontend: Ctrl+C in terminal
```

## Troubleshooting

### "Could not find a .NET SDK matching version"

→ Update `src/backend/global.json` to match your installed SDK version (run `dotnet --version`)

### "Connection timeout to SQL Server"

→ Verify connection string in `appsettings.Local.json`
→ Ensure SQL Server is running: `docker-compose ps`

### "401 Unauthorized on API calls"

→ Check Entra ID token in browser DevTools
→ Verify Entra credentials in environment config
→ Check token expiry

### "Module '@angular/core' not found"

→ Run `npm install` in `src/frontend/customer-first-portal`
→ Check Node version: `node --version` (should be 20+)

## Environment Variables

The project uses layered configuration:

1. `appsettings.json` (committed, shared defaults)
2. `appsettings.{Environment}.json` (local, not committed)
3. `appsettings.Local.json` (for local dev, not committed)
4. Environment variables (highest priority)

Never commit secrets. Use local files and environment variables for sensitive data.

## Next Steps

1. Set up your local environment using the steps above
2. Read the [Architecture Guide](./ARCHITECTURE.md) for deeper understanding
3. Check the Phase 1 feature backlog in the issues tracker
4. Start with the domain entity definitions in Phase 1

## Support

For questions or setup issues:
1. Check troubleshooting section above
2. Review `.github/copilot-instructions.md` for coding conventions
3. Ask in team chat or create an issue

Good luck! 🚀
