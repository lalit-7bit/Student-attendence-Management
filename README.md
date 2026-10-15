# SmartAttend

SmartAttend is a student attendance application built with ASP.NET Core MVC, .NET 10, Razor, Bootstrap, ADO.NET (`Microsoft.Data.SqlClient`), and SQL Server LocalDB.

## Current features

- Student registration, search, editing, and deactivation; unique enrollment numbers and class validation.
- Department, class, teacher, subject, and subject-to-teacher assignment management.
- Attendance by class, assigned subject, date, and period. Active class roster, Present/Absent/Late/Excused marks, remarks, correction of an existing session, duplicate prevention, transaction, and append-only application audit entries.
- Admin dashboard from database aggregates: active students, today’s marks, all-time average, low-attendance count, last-seven-day totals, month-to-date counts, class summaries, and recent sessions. Excused records are excluded from attendance percentages. Present and Late count as attended.
- Attendance register filters and CSV export (quoted/escaped cells and formula-injection prefix handling).
- Role-based cookie authentication for Administrator, Teacher, and Student; profile-scoped teacher/student views; no public privileged registration.
- Administrator account creation for linked teacher and student profiles. Passwords are stored using ASP.NET Core `PasswordHasher`, never as plaintext. Eight login attempts per minute per IP are allowed.
- Admin configurable institution name and low-attendance threshold (1–100%, default 75%).
- `/healthz` checks database connectivity without returning connection details.

## Architecture

- MVC controllers handle HTTP and authorization.
- `Data/` contains parameterized, asynchronous ADO.NET repositories and the connection factory.
- Strongly typed models and Razor pages render forms and reports.
- `Database/Schema.sql` initializes a new database idempotently; `Database/Upgrade_Authentication.sql` adds the account table to an existing SmartAttend database; `Database/DemoSeed.sql` is optional synthetic setup data and never runs automatically.

## Prerequisites

- .NET 10 SDK
- SQL Server Express LocalDB on Windows for local development, or a reachable SQL Server instance
- SQL Server Management Studio or `sqlcmd` to run database scripts

## Local setup

1. For a new database, run `Database/Schema.sql` while connected to `master` (for example, `sqlcmd -S "(localdb)\MSSQLLocalDB" -d master -i Database/Schema.sql`). The script creates/selects `SmartAttend` and skips existing tables/indexes.
2. If SmartAttend was already initialized before account authentication was added, run `Database/Upgrade_Authentication.sql` against `SmartAttend`. It only creates `AppUsers` if missing. Back up any important database before schema changes.
3. Optionally run `Database/DemoSeed.sql` against `SmartAttend` for explicitly labelled demonstration entities. Do not run it against production data.
4. Configure `ConnectionStrings__DefaultConnection` as an environment variable to override the LocalDB default. Do not commit real credentials.
5. Set a development-only initial administrator before the first `dotnet run`:

   ```powershell
   $env:BootstrapAdmin__Email = 'admin@example.edu'
   $env:BootstrapAdmin__Password = 'Use-A-Unique-12+Char-Secret!'
   dotnet run --project attendence.csproj
   ```

   The development seeder requires a password of at least 12 characters with upper/lowercase, a number, and a symbol. It inserts only if the normalized email does not already exist; it never resets an existing account. Remove the environment variables after the first run. Production startup does not seed an administrator.

6. Sign in as Administrator. Use **School setup** to add school structure and assignments; use **Students** to register students; use **Accounts** to create linked Teacher and Student accounts. Teachers only see their assigned classes and subjects. Students only see the student record linked to their account.
7. Run `dotnet restore`, `dotnet build`, and `dotnet run`. The HTTP/HTTPS ports come from `Properties/launchSettings.json`.

## Roles and access

- **Administrator:** dashboard, student and school setup, account administration, attendance, and all reports.
- **Teacher:** assigned attendance sessions and reports for assigned subjects.
- **Student:** their own attendance summary and history only.

Accounts are linked to an existing teacher or student profile. There is no public sign-up. Deactivating a student/teacher also disables their linked account and retains historical attendance.

## Tests

Run `dotnet test SmartAttend.sln`. Unit tests cover attendance percentage rules (Present + Late over Present + Absent + Late), excused exclusion, zero denominator, threshold comparison, and invalid negative totals. SQL Server-specific behavior still needs verification against a configured LocalDB/SQL Server instance.

## Deployment

Never expose LocalDB or a developer’s local SQL Server over the internet. Use a managed SQL Server-compatible database, set the connection string as a hosting secret, apply reviewed SQL scripts before release, use HTTPS, and deploy only after security and database integration testing. `/healthz` returns 503 when the database cannot connect.

As of October 2026, [Azure App Service documents an F1 free web-app tier](https://learn.microsoft.com/en-us/azure/app-service/quickstart-dotnetcore), and [Azure SQL documents a free offer with usage limits](https://github.com/MicrosoftDocs/sql-docs/blob/live/azure-sql/database/free-offer.md). Those are separate resources and SQL usage beyond free limits can be billed; check current eligibility, region, limits, and billing settings before choosing it. Other free web hosts may not support a persistent .NET process and SQL Server database together. There is no public deployment configured by this project.

## Known limitations

- SQL Server integration could not be exercised in the coding environment because its LocalDB automatic instance failed to start. Run the SQL scripts and verify CRUD/attendance against your working LocalDB before using real data.
- The app uses ASP.NET Core cookie authentication with the built-in `PasswordHasher`, not the ASP.NET Core Identity EF store. It has role claims and linked profiles but does not yet implement password reset/change, multi-factor authentication, or account lockout beyond per-IP login rate limiting.
- Reports currently return the full matching result set (no pagination). Database-backed role authorization, exports, and dashboard calculations compile but need integration tests against SQL Server.
- No public website or database is deployed, and hosting may incur charges outside current free quotas.
