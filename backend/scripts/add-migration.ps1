param(
  [Parameter(Mandatory = $true)][string]$Module,
  [Parameter(Mandatory = $true)][string]$Name
)

$map = @{
  Identity = @{ Project = "src/Modules/Identity/Infrastructure/Identity.Infrastructure.csproj"; Context = "IdentityDbContext" }
  Users = @{ Project = "src/Modules/Users/Infrastructure/Users.Infrastructure.csproj"; Context = "UsersDbContext" }
  Roles = @{ Project = "src/Modules/Roles/Infrastructure/Roles.Infrastructure.csproj"; Context = "RolesDbContext" }
  Notifications = @{ Project = "src/Modules/Notifications/Infrastructure/Notifications.Infrastructure.csproj"; Context = "NotificationsDbContext" }
  AuditLogs = @{ Project = "src/Modules/AuditLogs/Infrastructure/AuditLogs.Infrastructure.csproj"; Context = "AuditLogsDbContext" }
  Admissions = @{ Project = "src/Modules/Admissions/Infrastructure/Admissions.Infrastructure.csproj"; Context = "AdmissionsDbContext" }
}

if (-not $map.ContainsKey($Module)) {
  throw "Unknown module. Use: Identity, Users, Roles, Notifications, AuditLogs, Admissions"
}

$info = $map[$Module]
dotnet ef migrations add $Name `
  --project $info.Project `
  --startup-project src/Api/Api.csproj `
  --context $info.Context `
  --output-dir Persistence/Migrations