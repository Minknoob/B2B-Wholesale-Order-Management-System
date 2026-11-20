param(
    [string]$ServerInstance = ".\SQLEXPRESS",
    [string]$DatabaseName = "B2BWholesale"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$ConnectionString = "Server=$ServerInstance;Database=$DatabaseName;Trusted_Connection=True;TrustServerCertificate=True;"
$TempHarnessDir = Join-Path ([System.IO.Path]::GetTempPath()) "B2B_M3_TxHarness"
$LogFile = Join-Path $PSScriptRoot "M3_TRANSACTION_TEST_LOG.txt"

function Reset-SeedDatabase {
    $sqlCreate = Get-Content -Raw -Encoding UTF8 (Join-Path $RepoRoot "Database\CreateDatabase.sql")
    $sqlSeed   = Get-Content -Raw -Encoding UTF8 (Join-Path $RepoRoot "Database\SeedData.sql")
    $batches   = ($sqlCreate + "`r`nGO`r`n" + $sqlSeed) -split '(?m)^\s*GO\s*$'

    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=$ServerInstance;Database=master;Integrated Security=True;TrustServerCertificate=True;")
    $conn.Open()
    try {
        foreach ($batch in $batches) {
            if (-not [string]::IsNullOrWhiteSpace($batch)) {
                $cmd = $conn.CreateCommand()
                $cmd.CommandText = $batch
                [void]$cmd.ExecuteNonQuery()
            }
        }
    } finally {
        $conn.Close()
    }
}

New-Item -ItemType Directory -Force -Path $TempHarnessDir | Out-Null

$PortalProj = Join-Path $RepoRoot "B2B.AgentPortal\B2B.AgentPortal.csproj"
$CoreProj   = Join-Path $RepoRoot "B2B.Core\B2B.Core.csproj"

@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$CoreProj" />
    <ProjectReference Include="$PortalProj" />
  </ItemGroup>
</Project>
"@ | Set-Content -Encoding UTF8 (Join-Path $TempHarnessDir "M3TxHarness.csproj")

@"
using B2B.AgentPortal.Data;
using B2B.AgentPortal.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

string connectionString = Environment.GetEnvironmentVariable("B2B_TEST_CONN")
    ?? throw new InvalidOperationException("Missing B2B_TEST_CONN");

B2BDbContext CreateDb()
{
    var opts = new DbContextOptionsBuilder<B2BDbContext>()
        .UseSqlServer(connectionString)
        .Options;
    return new B2BDbContext(opts);
}

OrderService CreateOrderService(B2BDbContext db, Func<CancellationToken, Task>? hook = null) =>
    new(db, NullLogger<OrderService>.Instance, TimeProvider.System, hook);

AgentAuthenticationService CreateAuthService(B2BDbContext db) =>
    new(db, NullLogger<AgentAuthenticationService>.Instance);

async Task<(int Orders, int Details)> CountOrdersAsync()
{
    await using var db = CreateDb();
    return (await db.Orders.CountAsync(), await db.OrderDetails.CountAsync());
}

async Task<int> GetStockAsync(int itemId)
{
    await using var db = CreateDb();
    return await db.Items.AsNoTracking().Where(i => i.ItemID == itemId).Select(i => i.StockQuantity).SingleAsync();
}

Console.WriteLine($"B2B WHOLESALE M3 - AUTOMATED SQL SERVER VERIFICATION LOG");
Console.WriteLine($"Executed At: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
Console.WriteLine($"Target Database: {connectionString}");
Console.WriteLine(new string('=', 78));

Console.WriteLine("\n[PART 1] AGENT LOGIN & LOCKOUT VERIFICATION (B2B.Core + AgentAuthenticationService)");
{
    await using (var db = CreateDb())
    {
        var res = await CreateAuthService(db).AuthenticateAgentAsync("agent01", "Demo@123");
        Console.WriteLine($"AUTH-01 | Agent01 Valid Login  -> Succeeded={res.Succeeded}, AgentId={res.AgentId}, AgentName={res.AgentName}");
    }

    await using (var db = CreateDb())
    {
        var res = await CreateAuthService(db).AuthenticateAgentAsync("admin", "Demo@123");
        Console.WriteLine($"AUTH-02 | Admin Web Rejected   -> Succeeded={res.Succeeded}, Reason={res.FailureReason}");
    }

    for (int i = 1; i <= 3; i++)
    {
        await using var db = CreateDb();
        var res = await CreateAuthService(db).AuthenticateAgentAsync("agent14", "WrongPassword!");
        var u = await db.Users.AsNoTracking().SingleAsync(x => x.UserName == "agent14");
        Console.WriteLine($"AUTH-03 | Wrong Password #{i}    -> Succeeded={res.Succeeded}, Reason={res.FailureReason}, FailedLoginCount={u.FailedLoginCount}, IsLocked={u.IsLocked}");
    }

    await using (var db = CreateDb())
    {
        var resLocked = await CreateAuthService(db).AuthenticateAgentAsync("agent14", "Demo@123");
        Console.WriteLine($"AUTH-04 | Locked + Valid Pass  -> Succeeded={resLocked.Succeeded}, Reason={resLocked.FailureReason}");
        await db.Database.ExecuteSqlRawAsync("UPDATE dbo.Users SET IsLocked = 0, FailedLoginCount = 0 WHERE UserName = N'agent14'");
    }
}

Console.WriteLine("\n[PART 2] 15 ORDER TRANSACTION SCENARIOS (OrderService.PlaceOrderAsync)");

// TC-01
{
    var (oBefore, dBefore) = await CountOrdersAsync();
    int sBefore = await GetStockAsync(1);
    await using var db = CreateDb();
    var res = await CreateOrderService(db).PlaceOrderAsync(1, [new OrderLineRequest(1, 5)]);
    var (oAfter, dAfter) = await CountOrdersAsync();
    int sAfter = await GetStockAsync(1);
    Console.WriteLine($"TC-01 | PASS={(res.Success && sAfter == 115 && oAfter == oBefore + 1)} | Stock#1: {sBefore}->{sAfter} | Orders: {oBefore}->{oAfter} | Details: {dBefore}->{dAfter} | OrderId={res.OrderId}, Total={res.TotalAmount:N2}");
}

// TC-02
{
    var (oBefore, dBefore) = await CountOrdersAsync();
    int s2Before = await GetStockAsync(2);
    int s6Before = await GetStockAsync(6);
    await using var db = CreateDb();
    var res = await CreateOrderService(db).PlaceOrderAsync(1, [new OrderLineRequest(2, 4), new OrderLineRequest(6, 2)]);
    var (oAfter, dAfter) = await CountOrdersAsync();
    int s2After = await GetStockAsync(2);
    int s6After = await GetStockAsync(6);
    Console.WriteLine($"TC-02 | PASS={(res.Success && s2After == 81 && s6After == 43)} | Stock#2: {s2Before}->{s2After}, Stock#6: {s6Before}->{s6After} | Orders: {oBefore}->{oAfter} | Details: {dBefore}->{dAfter} | OrderId={res.OrderId}, Total={res.TotalAmount:N2}");
}

// TC-03
{
    var (oBefore, dBefore) = await CountOrdersAsync();
    int sBefore = await GetStockAsync(11);
    await using var db = CreateDb();
    var res = await CreateOrderService(db).PlaceOrderAsync(2, [new OrderLineRequest(11, sBefore)]);
    var (oAfter, dAfter) = await CountOrdersAsync();
    int sAfter = await GetStockAsync(11);
    Console.WriteLine($"TC-03 | PASS={(res.Success && sAfter == 0)} | Stock#11: {sBefore}->{sAfter} | Orders: {oBefore}->{oAfter} | Details: {dBefore}->{dAfter} | OrderId={res.OrderId}, Total={res.TotalAmount:N2}");
}

// TC-04
{
    var (oBefore, dBefore) = await CountOrdersAsync();
    int sBefore = await GetStockAsync(15);
    await using var db = CreateDb();
    var res = await CreateOrderService(db).PlaceOrderAsync(2, [new OrderLineRequest(15, 45)]);
    var (oAfter, dAfter) = await CountOrdersAsync();
    int sAfter = await GetStockAsync(15);
    Console.WriteLine($"TC-04 | PASS={(!res.Success && res.ErrorCode == OrderErrorCode.InsufficientStock && sAfter == sBefore && oAfter == oBefore)} | Stock#15: {sBefore}->{sAfter} | Orders: {oBefore}->{oAfter} | Err={res.ErrorCode}");
}

// TC-05
{
    var (oBefore, dBefore) = await CountOrdersAsync();
    int sBefore = await GetStockAsync(3);
    await using var db = CreateDb();
    var res = await CreateOrderService(db).PlaceOrderAsync(2, [new OrderLineRequest(3, 0)]);
    var (oAfter, dAfter) = await CountOrdersAsync();
    int sAfter = await GetStockAsync(3);
    Console.WriteLine($"TC-05 | PASS={(!res.Success && res.ErrorCode == OrderErrorCode.InvalidQuantity && sAfter == sBefore && oAfter == oBefore)} | Stock#3: {sBefore}->{sAfter} | Orders: {oBefore}->{oAfter} | Err={res.ErrorCode}");
}

// TC-06
{
    var (oBefore, dBefore) = await CountOrdersAsync();
    int sBefore = await GetStockAsync(3);
    await using var db = CreateDb();
    var res = await CreateOrderService(db).PlaceOrderAsync(2, [new OrderLineRequest(3, -5)]);
    var (oAfter, dAfter) = await CountOrdersAsync();
    int sAfter = await GetStockAsync(3);
    Console.WriteLine($"TC-06 | PASS={(!res.Success && res.ErrorCode == OrderErrorCode.InvalidQuantity && sAfter == sBefore && oAfter == oBefore)} | Stock#3: {sBefore}->{sAfter} | Orders: {oBefore}->{oAfter} | Err={res.ErrorCode}");
}

// TC-07
{
    var (oBefore, dBefore) = await CountOrdersAsync();
    int sBefore = await GetStockAsync(3);
    await using var db = CreateDb();
    var res = await CreateOrderService(db).PlaceOrderAsync(3, [new OrderLineRequest(3, 10), new OrderLineRequest(3, 15)]);
    var (oAfter, dAfter) = await CountOrdersAsync();
    int sAfter = await GetStockAsync(3);
    Console.WriteLine($"TC-07 | PASS={(res.Success && sAfter == 45 && res.Lines.Count == 1)} | Stock#3: {sBefore}->{sAfter} | Orders: {oBefore}->{oAfter} | Details: {dBefore}->{dAfter} | OrderId={res.OrderId}, MergedLines={res.Lines.Count}, Total={res.TotalAmount:N2}");
}

// TC-08
{
    var (oBefore, dBefore) = await CountOrdersAsync();
    int sBefore = await GetStockAsync(3);
    await using var db = CreateDb();
    var res = await CreateOrderService(db).PlaceOrderAsync(3, [new OrderLineRequest(3, 30), new OrderLineRequest(3, 25)]);
    var (oAfter, dAfter) = await CountOrdersAsync();
    int sAfter = await GetStockAsync(3);
    Console.WriteLine($"TC-08 | PASS={(!res.Success && res.ErrorCode == OrderErrorCode.InsufficientStock && sAfter == sBefore && oAfter == oBefore)} | Stock#3: {sBefore}->{sAfter} | Orders: {oBefore}->{oAfter} | Err={res.ErrorCode}");
}

// TC-09
{
    var (oBefore, dBefore) = await CountOrdersAsync();
    int s4Before = await GetStockAsync(4);
    await using var db = CreateDb();
    var res = await CreateOrderService(db).PlaceOrderAsync(3, [new OrderLineRequest(4, 2), new OrderLineRequest(99999, 1)]);
    var (oAfter, dAfter) = await CountOrdersAsync();
    int s4After = await GetStockAsync(4);
    Console.WriteLine($"TC-09 | PASS={(!res.Success && res.ErrorCode == OrderErrorCode.InvalidItem && s4After == s4Before && oAfter == oBefore)} | Stock#4: {s4Before}->{s4After} | Orders: {oBefore}->{oAfter} | Err={res.ErrorCode}");
}

// TC-10
{
    var (oBefore, dBefore) = await CountOrdersAsync();
    int s4Before = await GetStockAsync(4);
    int s15Before = await GetStockAsync(15);
    await using var db = CreateDb();
    var res = await CreateOrderService(db).PlaceOrderAsync(4, [new OrderLineRequest(4, 5), new OrderLineRequest(15, 100)]);
    var (oAfter, dAfter) = await CountOrdersAsync();
    int s4After = await GetStockAsync(4);
    int s15After = await GetStockAsync(15);
    Console.WriteLine($"TC-10 | PASS={(!res.Success && s4After == s4Before && s15After == s15Before && oAfter == oBefore && dAfter == dBefore)} | Stock#4: {s4Before}->{s4After}, Stock#15: {s15Before}->{s15After} | Orders: {oBefore}->{oAfter} | Details: {dBefore}->{dAfter} | Err={res.ErrorCode}");
}

// TC-11
{
    var (oBefore, dBefore) = await CountOrdersAsync();
    int s5Before = await GetStockAsync(5);
    await using var db = CreateDb();
    var svc = CreateOrderService(db, _ => throw new InvalidOperationException("Simulated failure right before CommitAsync"));
    var res = await svc.PlaceOrderAsync(4, [new OrderLineRequest(5, 12)]);
    var (oAfter, dAfter) = await CountOrdersAsync();
    int s5After = await GetStockAsync(5);
    Console.WriteLine($"TC-11 | PASS={(!res.Success && res.ErrorCode == OrderErrorCode.UnexpectedFailure && s5After == s5Before && oAfter == oBefore && dAfter == dBefore)} | Stock#5: {s5Before}->{s5After} | Orders: {oBefore}->{oAfter} | Details: {dBefore}->{dAfter} | Err={res.ErrorCode}");
}

// TC-12
{
    var (oBefore, dBefore) = await CountOrdersAsync();
    int s15Before = await GetStockAsync(15);
    var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    async Task<PlaceOrderResult> BuyAsync(int agentId)
    {
        await barrier.Task;
        await using var db = CreateDb();
        return await CreateOrderService(db).PlaceOrderAsync(agentId, [new OrderLineRequest(15, 30)]);
    }
    var t1 = Task.Run(() => BuyAsync(5));
    var t2 = Task.Run(() => BuyAsync(6));
    barrier.SetResult();
    var results = await Task.WhenAll(t1, t2);
    int ok = results.Count(r => r.Success);
    int fail = results.Count(r => !r.Success);
    var (oAfter, dAfter) = await CountOrdersAsync();
    int s15After = await GetStockAsync(15);
    Console.WriteLine($"TC-12 | PASS={(ok == 1 && fail == 1 && s15After == 10 && oAfter == oBefore + 1)} | Stock#15: {s15Before}->{s15After} | Orders: {oBefore}->{oAfter} | Success={ok}, Blocked={fail}");
}

// TC-13
{
    var (oBefore, dBefore) = await CountOrdersAsync();
    int s8Before = await GetStockAsync(8);
    await using (var setupDb = CreateDb())
    {
        await setupDb.Database.ExecuteSqlRawAsync("UPDATE dbo.Item SET Price = 61000 WHERE ItemID = 8");
    }
    await using var db = CreateDb();
    var res = await CreateOrderService(db).PlaceOrderAsync(7, [new OrderLineRequest(8, 3)]);
    var (oAfter, dAfter) = await CountOrdersAsync();
    int s8After = await GetStockAsync(8);
    Console.WriteLine($"TC-13 | PASS={(res.Success && res.Lines[0].UnitAmount == 61000m && res.TotalAmount == 183000m)} | Stock#8: {s8Before}->{s8After} | Orders: {oBefore}->{oAfter} | OrderId={res.OrderId}, UnitAmount={res.Lines[0].UnitAmount:N2}, Total={res.TotalAmount:N2}");
}

// TC-14
{
    var (oBefore, dBefore) = await CountOrdersAsync();
    int s7Before = await GetStockAsync(7);
    await using var db = CreateDb();
    var r1 = await CreateOrderService(db).PlaceOrderAsync(9999, [new OrderLineRequest(7, 2)]);
    var r2 = await CreateOrderService(db).PlaceOrderAsync(0, [new OrderLineRequest(7, 2)]);
    var (oAfter, dAfter) = await CountOrdersAsync();
    int s7After = await GetStockAsync(7);
    Console.WriteLine($"TC-14 | PASS={(!r1.Success && !r2.Success && r1.ErrorCode == OrderErrorCode.InvalidAgent && r2.ErrorCode == OrderErrorCode.InvalidAgent && oAfter == oBefore)} | Stock#7: {s7Before}->{s7After} | Orders: {oBefore}->{oAfter}");
}

// TC-15
{
    var (oBefore, dBefore) = await CountOrdersAsync();
    await using var db = CreateDb();
    var res = await CreateOrderService(db).PlaceOrderAsync(1, Array.Empty<OrderLineRequest>());
    var (oAfter, dAfter) = await CountOrdersAsync();
    Console.WriteLine($"TC-15 | PASS={(!res.Success && res.ErrorCode == OrderErrorCode.EmptyOrder && oAfter == oBefore)} | Orders: {oBefore}->{oAfter} | Err={res.ErrorCode}");
}
"@ | Set-Content -Encoding UTF8 (Join-Path $TempHarnessDir "Program.cs")

Write-Host "1. Resetting seed database on $ServerInstance/$DatabaseName ..."
Reset-SeedDatabase

Write-Host "2. Running 15 automated transaction scenarios ..."
$env:DOTNET_ROLL_FORWARD = "Major"
$env:B2B_TEST_CONN = $ConnectionString
$output = dotnet run --project (Join-Path $TempHarnessDir "M3TxHarness.csproj") 2>&1 | Out-String
$output | Set-Content -Encoding UTF8 $LogFile
Write-Host $output

Write-Host "3. Restoring clean M1 seed data after test run ..."
Reset-SeedDatabase
Remove-Item -Recurse -Force $TempHarnessDir -ErrorAction SilentlyContinue
Write-Host "Saved full log to: $LogFile"
