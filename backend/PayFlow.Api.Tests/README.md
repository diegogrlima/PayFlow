# Behavioral test suite

The `Unit/` tests use xUnit and FluentAssertions. Moq replaces repository and token-service boundaries only; domain entities and request validators are real. Every test creates its own context, accounts, tokens and mocks. There are no shared mutable fixtures, database connections or ordering dependencies. Classes run in parallel under xUnit's default behavior.

## Protected rules

| Area | Regression protected |
| --- | --- |
| Account | Positive deposits/debits, insufficient balance, spending the entire balance, invalid updates preserving state, owner required, nonnegative initial balance and domain name limits |
| Transfer keys | Canonical email/CPF/CNPJ/international phone representation, invalid check digits and formats, key/account compatibility, rejected replacements preserving the previous key, paired removal and masking |
| Transactions | Distinct nonempty account identifiers, positive amounts with two decimal places, completed transfers, normalized destination lookup, source ownership, insufficient balance, self-transfer and missing destination |
| Transfer failures | Invalid requests leave money untouched; persistence failure requests rollback even after cancellation and does not commit. Real database rollback remains an integration concern. |
| Accounts service | Creation tied to the current user with zero initial balance, missing-user rejection, owner-scoped listing, owned deposits, foreign-account rejection, masking, duplicate-key rejection without saving, re-registering the same account's key and validation errors without sensitive input |
| Authentication | Invalid credentials do not issue tokens; token hashes are persisted; rotation keeps the family and revokes its predecessor; replay revokes the family; deleted users and concurrency conflicts reject refresh; revocation is idempotent |
| Registration | Password composition, duplicate email rejection without persistence, password hashing before storage and missing authenticated user |
| Validators | API name boundaries, login requirements, positive monetary amounts and decimal(18,2) storage limits |
| Password hashing | Correct password verification, rejection of a different password and malformed/unsupported stored hashes |
| Queries | Participants can read a transfer; unrelated users cannot. History requires account ownership and applies the existing page/page-size policy. |
| Time and JWT | Exact refresh expiration boundary, revocation of expired tokens, successor lifetime, signed JWT identity/issuer/audience/lifetime and rejection with the wrong key |
| HTTP | Missing/invalid/expired JWT rejection; foreign accounts hidden; string enum binding and numeric/undefined enum rejection; created-resource locations; transfer status codes; sensitive input excluded from validation responses; refresh replay revokes successors |
| SQL Server | Real migrations preserve enums on upgrade/downgrade; ownership queries; whitespace-trimmed email lookup; history direction/order/pagination; multiple null keys; paired key constraints; duplicate writers; real deadlock conflicts and conserved balances; full CPF excluded from EF failure logs |

Several assertions may express one invariant: for example a rejected debit must throw **and** preserve balance. They are not unrelated checks bundled into one test.

## Running and isolation

```powershell
dotnet test PayFlow.sln -c Release
dotnet test PayFlow.sln -c Release --filter 'FullyQualifiedName~PayFlow.Api.Tests.Unit'
dotnet test PayFlow.sln -c Release --filter 'Category!=SqlServer'
dotnet test PayFlow.sln -c Release --filter 'Category=SqlServer'
& backend/PayFlow.Api.Tests/verify-isolation.ps1 -Seed 20261001
$env:TEST_ORDER_SEED = '42'
dotnet test PayFlow.sln -c Release --no-build
Remove-Item Env:TEST_ORDER_SEED
```

Release avoids the Debug executable lock when the API is running in Visual Studio. The test orderer shuffles test cases within each class with a configurable seed; it does not serialize classes or share mutable test state. `verify-isolation.ps1` runs every unit test case in a separate process in shuffled order, including individual theory rows, and verifies exactly one case executed in each process.

## Integration environments and remaining limits

`TransferKeyTests.cs` is retained unchanged. Its SQLite cases validate actual persistence, unique keys, enum mapping and transaction rollback; they are not pure unit tests and do not prove SQL Server behavior. Its original mixed domain cases are retained to avoid removing an existing safety net.

`Integration/ApiBehaviorTests.cs` hosts the real application pipeline using WebApplicationFactory, real JWT validation and a fresh in-memory SQLite database per factory/test. It never reads or connects to the application database. `Integration/SqlServerBehaviorTests.cs` uses real SQL Server; each case creates and deletes only its own `PayFlowTests_<GUID>` database. Cleanup checks ownership of the generated database name. No database or factory is shared between tests.

On Windows SQL tests default to `(localdb)\MSSQLLocalDB`. For another test SQL Server set `PAYFLOW_TEST_SQLSERVER` to a connection string whose account can create/drop test databases. The fixture overrides the catalog with its own generated name. Do not use application configuration or production credentials. When SQL Server is unavailable, explicitly run `Category!=SqlServer`; SQL tests are not silently skipped and the full suite requires SQL Server. No Docker container or shared schema is required.

Deadlock tests synchronize two actual serializable transactions at their save boundary through a per-test interceptor, without sleeps or privately constructed SqlExceptions. Exactly one transfer must commit, one must return ConflictException, and the persisted debit/credit/transaction must agree. Timeouts bound failures, not schedule the race. SQL diagnostics tests exercise the application's real database logging configuration.

RefreshToken, AuthService and JwtTokenService accept an optional TimeProvider and default to TimeProvider.System. This preserves runtime rules and permits exact boundary tests without sleeps or reflection. The private persistence constructor uses the system clock; no clock is stored in the database. Program's public partial declaration exposes the entry point to the integration test host.

Remaining limits: email case sensitivity is determined by server/database collation, so these tests deliberately protect trimming without inventing a case-insensitive rule. SQL tests cover the added enum migrations with existing data and the complete migration chain on an empty database; they do not prove every historical migration is safe for every legacy data shape. Stress/load tests, retry policies and deployed-network behavior are outside this suite. No automatic deadlock retry is claimed: the current application reports a retryable conflict.

Observed rules that are intentionally preserved: domain holder names allow 150 characters, while the API validator allows 120; email transfer keys are returned unmasked; the account domain does not enforce two-decimal deposit precision, while the request validator does. On a duplicate-key error, `AccountService` mutates the tracked entity before checking uniqueness but does not save it; the suite does not invent an invariant that this in-memory instance is restored.

Tests do not assert generated GUIDs, exact wall-clock timestamps, random salts or implementation-specific method sequences. Exceptions are checked by type and relevant input property, not translated message wording.

## Verification performed

- The first iteration passed 138 cases, including 114 new unit cases checked individually (seed 42) and 24 preserved existing cases.
- The expanded suite contains 186 cases: 137 dedicated unit cases, 15 HTTP integration cases, 10 real SQL Server cases and the 24 preserved existing cases.
- The full expanded suite passed with no skipped tests, using xUnit's default test-class parallelism.
- All 48 added cases in the expanded iteration (23 unit, 15 HTTP, 10 SQL Server) passed individually in separate processes in shuffled order, including individual theory rows (seed 20261001).
- All five deliberate regressions were detected. After byte-for-byte restoration of the temporarily mutated files, the 186-case suite passed again, including with ordering seed 42.
- `verify-isolation.ps1 -Filter '<VSTest filter>'` permits individual-process checks for any selected unit or integration cases, including individual theory rows.
- Local mutation experiments verified exact token expiration and SQL history direction in addition to balance/email/rotation regressions. The experimental script that rewrites production source is intentionally not distributed with the repository; only the test suite and reproducible isolation runner are versioned.
