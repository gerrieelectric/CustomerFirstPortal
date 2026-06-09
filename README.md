# Customer First Portal

Employee recognition and acknowledgment platform built with Angular, ASP.NET Core, and SQL Server. Supports employee-to-employee and employee-on-behalf-of-customer submissions, with admin review/approval workflow, email notifications, analytics, and Gerrie integration.

## Project Structure

```
CustomerFirstPortal/
├── src/
│   ├── frontend/                 # Angular 19 web application
│   ├── backend/                  # ASP.NET Core 8 API
│   └── shared/                   # Shared contracts and models
├── infrastructure/               # Terraform, Bicep, Docker configs
├── .github/                      # GitHub Actions workflows
└── docker-compose.yml            # Local development environment
```

## Prerequisites

- Node.js 20+ and npm 10+
- .NET 8 SDK
- SQL Server 2019+ (on-prem or container)
- Azure Storage account (Blob for attachments)
- Microsoft Entra ID tenant (for authentication)
- Docker & Docker Compose (for local dev environment)

## Getting Started

### 1. Clone and Install

```bash
cd CustomerFirstPortal
```

### 2. Backend Setup

```bash
cd src/backend/Portal.Api
dotnet restore
dotnet ef database update
dotnet run
```

API runs on `https://localhost:5001` by default.

### 3. Frontend Setup

```bash
cd src/frontend/customer-first-portal
npm install
npm start
```

App runs on `http://localhost:4200` by default.

### 4. Local Development with Docker Compose

```bash
docker-compose up -d
```

This starts:
- SQL Server on port 1433
- API on port 5001
- Angular dev server on port 4200

## Configuration

### Environment Variables

See `src/backend/appsettings.json` and `src/frontend/environment.ts` for configuration templates.

**Backend required settings:**
- `ConnectionStrings:DefaultConnection` - SQL Server connection string
- `AzureStorage:ConnectionString` - Azure Blob Storage connection
- `Entra:TenantId`, `Entra:ClientId` - Entra ID credentials
- `Email:SmtpHost`, `Email:SmtpPort`, `Email:From` - Notification sender
- `Gerrie:ApiUrl`, `Gerrie:ApiKey` - Gerrie integration endpoint

**Frontend required settings:**
- `apiUrl` - Backend API base URL
- `auth.clientId`, `auth.tenantId` - Entra ID client config

## Architecture Layers (Backend)

- **Portal.Api** - HTTP endpoints, dependency injection, middleware
- **Portal.Application** - Business logic, commands, queries, validators
- **Portal.Domain** - Core entities, enums, value objects
- **Portal.Infrastructure** - EF Core, storage, notifications, Gerrie client

## Phase 1 Features

- ✅ Employee→Employee submission form
- ✅ Employee→Customer submission form (with optional attachments)
- ✅ Admin review and approval/rejection workflow
- ✅ Email notifications with secure review links
- ✅ Sortable/filterable admin list
- ✅ Admin settings for RBAC and notification groups
- ✅ Analytics dashboard (top recognized, approval metrics)
- ✅ Gerrie integration (outbound sync of approved records)

## Testing

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

## Deployment

See `/infrastructure` for deployment scripts and configurations for Azure App Service, Container Apps, or on-prem hosting.

## Contributing

1. Create a feature branch from `main`
2. Follow the architecture and code style conventions
3. Write tests for new features
4. Submit pull request for review

## License

Internal use only.
