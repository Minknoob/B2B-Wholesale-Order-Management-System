# B2B Wholesale Order Management System

Software Engineering Lab Assignment 2 - Semester 1, 2026-2027
Team: **The Five Super Brothers**

Repository: <https://github.com/Minknoob/B2B-Wholesale-Order-Management-System>

## 1. Project overview

The project is a shared-database B2B wholesale ordering system with two user-facing applications:

- **Back-office desktop application:** Windows Forms for Admin users.
- **Agent Portal:** ASP.NET Core MVC for Agent users.

Both applications use the same SQL Server database and the authentication rules in `B2B.Core`.

## 2. Main features

### Admin WinForms application

- Admin-only login.
- PBKDF2-SHA256 password verification.
- Account lockout after three consecutive failed attempts.
- Item and stock management.
- Agent management.
- Viewing web orders, order details and updating order status.
- Exporting a selected order as a PDF shipping manifest.
- Sales dashboard with best-selling items, low-stock warnings and order value by agent/date.

### ASP.NET Core Agent Portal

- Agent-only cookie authentication.
- Product catalog showing price and available stock.
- Multi-line master-detail order creation.
- Server-side price validation.
- Atomic stock deduction inside one database transaction.
- Insufficient-stock rejection and transaction rollback.
- Agent-owned order history and order details.
- Printable invoice.
- Responsive Bootstrap interface for desktop and mobile.

### Testing

- xUnit test project for core business logic.
- Seven recorded passing test cases covering stock deduction, insufficient stock, multi-item totals, duplicate lines, invalid quantities and account lockout.
- SQL integrity and transaction verification scripts.

## 3. Solution structure

| Path | Purpose |
|---|---|
| `B2B.Core` | Shared password hashing and account-lockout rules |
| `B2B.WinForms` | Admin back-office Windows Forms application |
| `B2B.AgentPortal` | ASP.NET Core MVC Agent Portal |
| `B2B.Tests` | xUnit business-logic tests |
| `Database` | Database creation, seed, integrity and transaction test scripts |
| `Screenshots_M1`–`Screenshots_M5` | Implementation and test evidence |
| `ERD.md` | Database entity-relationship diagram |
| `M3_TRANSACTION_TESTS.md` | Detailed SQL Server transaction test evidence |

## 4. Technology

- .NET 8
- C# 12
- Windows Forms
- ASP.NET Core MVC
- Entity Framework Core 8
- Microsoft SQL Server or SQL Server LocalDB
- Microsoft.Data.SqlClient
- Bootstrap 5
- xUnit and EF Core InMemory

## 5. Requirements

- Windows 10 or Windows 11
- Visual Studio with the **.NET Desktop Development** and **ASP.NET and web development** workloads
- .NET 8 SDK
- SQL Server, SQL Server Express or SQL Server LocalDB
- SQL Server Management Studio (SSMS)

Open `B2B-M1.sln` and restore the NuGet packages before building.

## 6. Database setup

For a clean database, run these scripts in SSMS in the following order:

1. `Database/CreateDatabase.sql`
2. `Database/SeedData.sql`
3. `Database/TestIntegrity.sql`

`CreateDatabase.sql` drops the existing application tables. Do not run it on a database containing data that must be retained.

The scripts create and seed these five tables:

- `Item`
- `Agent`
- `Order`
- `OrderDetail`
- `Users`

Each table contains 15–30 seed rows. The schema includes primary keys, foreign keys, `CHECK`, `UNIQUE` and `DEFAULT` constraints.

### Entity Framework migration alternative

The Agent Portal includes an initial EF Core migration for creating a new empty database:

```powershell
dotnet ef database update --project B2B.AgentPortal --startup-project B2B.AgentPortal
```

Do not apply the initial migration to a database that was already created with `Database/CreateDatabase.sql`.

## 7. Connection configuration

Configure both applications to use the same `B2BWholesale` database.

### WinForms

Edit `B2B.WinForms/App.config` and set the `B2BWholesale` connection string.

```xml
<add name="B2BWholesale"
     connectionString="Server=YOUR_SERVER;Database=B2BWholesale;Trusted_Connection=True;TrustServerCertificate=True;"
     providerName="Microsoft.Data.SqlClient" />
```

### Agent Portal

Edit `B2B.AgentPortal/appsettings.json` and set `ConnectionStrings:DefaultConnection`.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=B2BWholesale;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

LocalDB example:

```text
Server=(localdb)\SQLEXPRESS;Database=B2BWholesale;Trusted_Connection=True;TrustServerCertificate=True;
```

When writing the LocalDB example in JSON, escape the backslash as `\\`.

Do not commit real SQL usernames, passwords or machine-specific connection strings. Use Windows authentication, .NET User Secrets or environment variables for local secrets.

## 8. Build and run

From the repository root:

```powershell
dotnet restore B2B-M1.sln
dotnet build B2B-M1.sln
```

### Run the Admin application

```powershell
dotnet run --project B2B.WinForms/B2B.WinForms.csproj
```

### Run the Agent Portal

```powershell
dotnet run --project B2B.AgentPortal/B2B.AgentPortal.csproj
```

Default addresses:

- HTTP: <http://localhost:5180>
- HTTPS: <https://localhost:7180>

## 9. Demo accounts

The seed data uses dummy demonstration credentials only.

| Application | Username | Password | Expected access |
|---|---|---|---|
| WinForms | `admin` | `Demo@123` | Allowed as Admin |
| Agent Portal | `agent01` | `Demo@123` | Allowed as Agent |
| WinForms | `agent01` | `Demo@123` | Rejected because the role is Agent |
| Agent Portal | `admin` | `Demo@123` | Rejected because the role is Admin |

The seed includes one Admin and fourteen Agent accounts (`agent01`–`agent14`). Passwords are stored as PBKDF2-SHA256 hashes, not plaintext.

## 10. Run the tests

```powershell
dotnet test B2B.Tests/B2B.Tests.csproj
```

Recorded result:

```text
Passed: 7
Failed: 0
Skipped: 0
```

Additional database transaction checks are documented in `M3_TRANSACTION_TESTS.md` and can be reproduced with:

- `Database/TestM3Transactions.sql`
- `Screenshots_M3/RunM3TransactionTests.ps1`

## 11. Team contributions

| Member | Student ID | Main responsibility | Contribution |
|---|---|---|---:|
| Nguyen Trong Hoang Linh | 524H0166 | Database, seed data, integrity and Admin login | 20% |
| Pham Cong Quoc Huy | 524H0161 | WinForms inventory, agents, orders, manifest and dashboard | 20% |
| Le Hoang Tri | 524H0199 | Agent login, catalog and transactional ordering | 20% |
| Dang Hieu Minh | 524H0168 | My Orders, invoice, responsive UI and Git coordination | 20% |
| Hua Nguyen Thanh Trong | 524H0200 | Business-logic tests, integration support and final report | 20% |

Total contribution: **100%**.

## 12. Submission notes

The final submission contains:

- `Report.docx`
- `SourceCode.zip`
- `Readme.txt`
