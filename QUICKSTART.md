# QUICKSTART.md - Get Running in 5 Minutes

## The Fastest Way to Start

### Prerequisites
1. SQL Server running and reachable at your configured connection string
2. Update `src/backend/Portal.Api/appsettings.Local.json` with your SQL Server details
3. Run the initial EF migration
4. Start the backend and frontend servers

### Setup Steps

```bash
# Terminal 1: Backend
cd src/backend
dotnet restore
./.tools/dotnet-ef.exe database update --project ./Portal.Infrastructure --startup-project ./Portal.Api
dotnet run

# Terminal 2: Frontend
cd src/frontend/customer-first-portal
npm install
npm start
```

You'll have:
- ✅ API running at `https://localhost:5001`
- ✅ Frontend at `http://localhost:4200`
- ✅ SQL Server at `localhost:1433`
- ✅ Swagger docs at `https://localhost:5001/swagger`

### Option 2: Using Your Existing SQL Server

If you have SQL Server running locally already:

1. Update connection string in `src/backend/Portal.Api/appsettings.Local.json`
2. Run: `dotnet ef database update` from `src/backend`
3. Run: `dotnet run` from `src/backend`
4. Run: `npm start` from `src/frontend/customer-first-portal`

---

## First Day Tasks

### Before You Code

1. **Read the docs** (5 minutes)
   - `SETUP.md` - Full setup guide
   - `IMPLEMENTATION_STATUS.md` - What's ready, what's next
   - `.github/copilot-instructions.md` - Coding conventions

2. **Verify everything works** (2 minutes)
   - Frontend loads at `http://localhost:4200`
   - API is accessible (check `https://localhost:5001/swagger`)
   - Database created and migrated

### Ready to Implement?

**Start with Phase 1a: Domain Design**

Open `src/backend/Portal.Domain/Entities/` and create:

```csharp
// Submission.cs - The core aggregate root
public class Submission
{
    public Guid Id { get; set; }
    public SubmissionType Type { get; set; }
    public SubmissionStatus Status { get; set; }
    public string SubmitterName { get; set; }
    public string NomineeName { get; set; }
    public string Message { get; set; }
    public int Score { get; set; } // 1-5 rating
    public string Department { get; set; }
    public List<Attachment> Attachments { get; set; } = new();
    public DateTime SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ReviewerName { get; set; }
    public string? ReviewNotes { get; set; }
}
```

Then update the DbContext and create a migration.

---

## Key Directories

```
ProjectRoot/
├── src/backend/              ← ASP.NET Core API
│   ├── Portal.Api/           ← HTTP layer (start here)
│   ├── Portal.Application/   ← Business logic
│   ├── Portal.Domain/        ← Entities (Phase 1a)
│   ├── Portal.Infrastructure/← EF Core, Storage
│   └── Portal.Tests/         ← Tests
├── src/frontend/
│   └── customer-first-portal/
│       ├── src/app/core/     ← Auth, guards
│       ├── src/app/features/ ← Forms, admin pages
│       └── src/environments/ ← Config (local/prod)
├── infrastructure/           ← Future: Deployment scripts
├── .github/                  ← CI/CD, templates
├── docker-compose.yml        ← Local dev environment
├── README.md                 ← Overview
├── SETUP.md                  ← Detailed setup
└── IMPLEMENTATION_STATUS.md  ← Phase 1 roadmap
```

---

## Common Commands

### Backend
```bash
cd src/backend

# Run the API
dotnet run

# Run tests
dotnet test

# Create a new migration
dotnet ef migrations add DescriptiveName

# Update database
dotnet ef database update

# View pending migrations
dotnet ef migrations has-pending-updates
```

### Frontend
```bash
cd src/frontend/customer-first-portal

# Start dev server
npm start

# Run tests
npm test

# Build for production
npm run build

# Run linting
npm run lint
```

### Database
```bash
# Via Docker
docker-compose up -d      # Start services
docker-compose down       # Stop services
docker-compose ps         # Show status

# Via SQL Server (replace with your connection)
sqlcmd -S localhost,1433 -U sa -P YourStrong!Password123
```

---

## Debugging

### Backend (VS Code)
1. Install C# extension (Omnisharp)
2. Press `F5` to launch debugger
3. Breakpoints work in `.cs` files

### Frontend (Browser)
1. Open Chrome DevTools (F12)
2. Go to Sources tab
3. Breakpoints work in `.ts` files

---

## Environment Configuration

**Backend** - Create `src/backend/Portal.Api/appsettings.Local.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=CustomerFirstPortal;User Id=sa;Password=YourStrong!Password123;Encrypt=false;"
  },
  "Entra": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id"
  }
}
```

**Frontend** - Create `src/frontend/customer-first-portal/src/environments/environment.local.ts`:
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api',
  auth: {
    authority: 'https://login.microsoftonline.com/YOUR_TENANT_ID',
    clientId: 'YOUR_CLIENT_ID'
  }
};
```

---

## Stuck?

1. Check `SETUP.md` troubleshooting section
2. Verify prerequisites: Node 20+, .NET 10, Docker (optional)
3. Make sure services are running: `docker-compose ps`
4. Review `.github/copilot-instructions.md` for conventions

---

## Next Steps After Everything Runs

1. ✅ Run and verify locally ← **You are here**
2. Create domain entities (Phase 1a)
3. Create EF migrations (Phase 1b)
4. Implement submission endpoints (Phase 1c)
5. Build forms UI (Phase 1c)
6. Admin review workflow (Phase 1d)
7. Email notifications (Phase 1e)
8. Admin pages (Phase 1f)
9. Analytics (Phase 1g)
10. Gerrie integration (Phase 1h)

---

**Ready? Open VS Code and let's build! 🚀**

```bash
code C:/repos/CustomerFirstPortal
```
