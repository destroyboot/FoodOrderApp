using API.Support;
using Core.Data.Enums;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using System.Security.Claims;

namespace API.Controllers;

[Authorize(Roles = "Admin,RestaurantAdmin")]
[ApiController]
[Route("api/admin/reports")]
public class AdminReportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminReportsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("meta")]
    public async Task<ActionResult<ReportMetaDto>> GetMeta(CancellationToken ct)
    {
        var restaurantIds = await GetEffectiveRestaurantIdsAsync(null, ct);
        var query = _db.Restaurants
            .AsNoTracking()
            .Where(r => r.IsActive);

        if (restaurantIds is not null)
        {
            query = query.Where(r => restaurantIds.Contains(r.Id));
        }

        var restaurants = await query
            .OrderBy(r => r.Name)
            .Select(r => new ReportRestaurantOptionDto(r.Id, r.Name))
            .ToListAsync(ct);

        return Ok(new ReportMetaDto(
            CanChooseRestaurants: User.IsInRole("Admin"),
            CanSeeUserActivity: User.IsInRole("Admin"),
            Restaurants: restaurants));
    }

    [HttpGet("{reportKey}")]
    public async Task<ActionResult<ReportResultDto>> GetReport(
        string reportKey,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int[]? restaurantIds,
        CancellationToken ct)
    {
        var payload = await BuildReportAsync(reportKey, from, to, restaurantIds, ct);
        return Ok(ToDto(payload));
    }

    [HttpGet("{reportKey}/export")]
    public async Task<IActionResult> ExportReport(
        string reportKey,
        [FromQuery] string format,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int[]? restaurantIds,
        CancellationToken ct)
    {
        var payload = await BuildReportAsync(reportKey, from, to, restaurantIds, ct);
        var normalizedFormat = (format ?? "csv").Trim().ToLowerInvariant();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var baseFileName = $"{payload.FileName}-{timestamp}";

        return normalizedFormat switch
        {
            "xlsx" or "excel" => File(
                ReportExportBuilder.BuildExcel(payload.Table, payload.SheetName),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{baseFileName}.xlsx"),
            _ => File(
                ReportExportBuilder.BuildCsv(payload.Table),
                "text/csv; charset=utf-8",
                $"{baseFileName}.csv")
        };
    }

    private async Task<ReportPayload> BuildReportAsync(
        string reportKey,
        DateTime? from,
        DateTime? to,
        int[]? requestedRestaurantIds,
        CancellationToken ct)
    {
        var (fromUtc, toUtc) = NormalizeDateRange(from, to);
        var effectiveRestaurantIds = await GetEffectiveRestaurantIdsAsync(requestedRestaurantIds, ct);
        var restaurantCsv = ToRestaurantCsv(effectiveRestaurantIds);

        return reportKey.Trim().ToLowerInvariant() switch
        {
            "sales-summary" => await BuildSalesSummaryReportAsync(fromUtc, toUtc, restaurantCsv, ct),
            "customer-frequency" => await BuildCustomerFrequencyReportAsync(fromUtc, toUtc, restaurantCsv, ct),
            "customer-mix" => await BuildCustomerMixReportAsync(fromUtc, toUtc, effectiveRestaurantIds, ct),
            "menu-popularity" => await BuildMenuPopularityReportAsync(fromUtc, toUtc, restaurantCsv, ct),
            "user-activity" when User.IsInRole("Admin") => await BuildUserActivityReportAsync(fromUtc, toUtc, effectiveRestaurantIds, ct),
            "user-activity" => throw new InvalidOperationException("Only application admins can access user activity reports."),
            _ => throw new InvalidOperationException("Unknown report type.")
        };
    }

    private async Task<ReportPayload> BuildSalesSummaryReportAsync(DateTime fromUtc, DateTime toUtc, string? restaurantCsv, CancellationToken ct)
    {
        var table = await ExecuteTableAsync(
            "dbo.usp_ReportSalesSummary",
            CommandType.StoredProcedure,
            ct,
            new SqlParameter("@FromUtc", SqlDbType.DateTime2) { Value = fromUtc },
            new SqlParameter("@ToUtc", SqlDbType.DateTime2) { Value = toUtc },
            new SqlParameter("@RestaurantIdsCsv", SqlDbType.NVarChar) { Value = (object?)restaurantCsv ?? DBNull.Value });

        var totalRevenue = table.AsEnumerable().Sum(row => row.Field<decimal?>("GrossRevenue") ?? 0m);
        var totalOrders = table.AsEnumerable().Sum(row => row.Field<int?>("OrderCount") ?? 0);
        var completedOrders = table.AsEnumerable().Sum(row => row.Field<int?>("CompletedOrders") ?? 0);
        var cancelledOrders = table.AsEnumerable().Sum(row => row.Field<int?>("CancelledOrders") ?? 0);

        return new ReportPayload(
            "reports.salesSummaryTitle",
            "Sales summary",
            "sales-summary",
            "SalesSummary",
            table,
            new List<ReportSummaryMetric>
            {
                new("reports.totalRevenue", "Total revenue", totalRevenue.ToString("0.00", CultureInfo.InvariantCulture)),
                new("reports.totalOrders", "Orders", totalOrders.ToString(CultureInfo.InvariantCulture)),
                new("reports.completedOrders", "Completed", completedOrders.ToString(CultureInfo.InvariantCulture)),
                new("reports.cancelledOrders", "Cancelled", cancelledOrders.ToString(CultureInfo.InvariantCulture))
            });
    }

    private async Task<ReportPayload> BuildCustomerFrequencyReportAsync(DateTime fromUtc, DateTime toUtc, string? restaurantCsv, CancellationToken ct)
    {
        var table = await ExecuteTableAsync(
            """
            SELECT
                CustomerLabel,
                IsAnonymousCustomer,
                OrderCount,
                CompletedOrderCount,
                CancelledOrderCount,
                TotalSpent,
                LastOrderAtUtc
            FROM dbo.ufn_ReportCustomerStats(@RestaurantIdsCsv, @FromUtc, @ToUtc)
            ORDER BY OrderCount DESC, TotalSpent DESC, CustomerLabel
            """,
            CommandType.Text,
            ct,
            new SqlParameter("@RestaurantIdsCsv", SqlDbType.NVarChar) { Value = (object?)restaurantCsv ?? DBNull.Value },
            new SqlParameter("@FromUtc", SqlDbType.DateTime2) { Value = fromUtc },
            new SqlParameter("@ToUtc", SqlDbType.DateTime2) { Value = toUtc });

        var uniqueCustomers = table.Rows.Count;
        var anonymousCustomers = table.AsEnumerable().Count(row => row.Field<bool?>("IsAnonymousCustomer") == true);
        var registeredCustomers = uniqueCustomers - anonymousCustomers;
        var totalOrders = table.AsEnumerable().Sum(row => row.Field<int?>("OrderCount") ?? 0);
        var averageOrders = uniqueCustomers == 0 ? 0m : decimal.Round((decimal)totalOrders / uniqueCustomers, 2);

        return new ReportPayload(
            "reports.customerFrequencyTitle",
            "Customer frequency",
            "customer-frequency",
            "CustomerFrequency",
            table,
            new List<ReportSummaryMetric>
            {
                new("reports.uniqueCustomers", "Unique customers", uniqueCustomers.ToString(CultureInfo.InvariantCulture)),
                new("reports.registeredCustomers", "Registered", registeredCustomers.ToString(CultureInfo.InvariantCulture)),
                new("reports.anonymousCustomers", "Anonymous", anonymousCustomers.ToString(CultureInfo.InvariantCulture)),
                new("reports.averageOrdersPerCustomer", "Avg orders / customer", averageOrders.ToString("0.00", CultureInfo.InvariantCulture))
            });
    }

    private async Task<ReportPayload> BuildCustomerMixReportAsync(DateTime fromUtc, DateTime toUtc, List<int>? effectiveRestaurantIds, CancellationToken ct)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Restaurant)
            .Where(o => o.Status != OrderStatus.Draft && o.CreatedAt >= fromUtc && o.CreatedAt <= toUtc)
            .Where(o => effectiveRestaurantIds == null || (o.RestaurantId.HasValue && effectiveRestaurantIds.Contains(o.RestaurantId.Value)))
            .Select(o => new
            {
                o.RestaurantId,
                RestaurantName = o.Restaurant != null ? o.Restaurant.Name : "-",
                IsAnonymousCustomer = !_db.Users.Any(u => u.Id == o.CustomerId),
                o.Total
            })
            .ToListAsync(ct);

        var table = new DataTable("CustomerMix");
        table.Columns.Add("RestaurantId", typeof(int));
        table.Columns.Add("RestaurantName", typeof(string));
        table.Columns.Add("RegisteredOrders", typeof(int));
        table.Columns.Add("AnonymousOrders", typeof(int));
        table.Columns.Add("RegisteredRevenue", typeof(decimal));
        table.Columns.Add("AnonymousRevenue", typeof(decimal));
        table.Columns.Add("AnonymousSharePercent", typeof(decimal));

        foreach (var group in orders.GroupBy(x => new { x.RestaurantId, x.RestaurantName }).OrderBy(x => x.Key.RestaurantName))
        {
            var registeredOrders = group.Count(x => !x.IsAnonymousCustomer);
            var anonymousOrders = group.Count(x => x.IsAnonymousCustomer);
            var totalOrders = registeredOrders + anonymousOrders;
            var anonymousShare = totalOrders == 0 ? 0m : decimal.Round((decimal)anonymousOrders * 100m / totalOrders, 2);

            table.Rows.Add(
                group.Key.RestaurantId ?? 0,
                group.Key.RestaurantName,
                registeredOrders,
                anonymousOrders,
                group.Where(x => !x.IsAnonymousCustomer).Sum(x => x.Total),
                group.Where(x => x.IsAnonymousCustomer).Sum(x => x.Total),
                anonymousShare);
        }

        var totalRegistered = orders.Count(x => !x.IsAnonymousCustomer);
        var totalAnonymous = orders.Count(x => x.IsAnonymousCustomer);
        var totalMixOrders = totalRegistered + totalAnonymous;

        return new ReportPayload(
            "reports.customerMixTitle",
            "Registered vs anonymous orders",
            "customer-mix",
            "CustomerMix",
            table,
            new List<ReportSummaryMetric>
            {
                new("reports.registeredOrders", "Registered orders", totalRegistered.ToString(CultureInfo.InvariantCulture)),
                new("reports.anonymousOrders", "Anonymous orders", totalAnonymous.ToString(CultureInfo.InvariantCulture)),
                new(
                    "reports.anonymousShare",
                    "Anonymous share",
                    (totalMixOrders == 0 ? 0m : decimal.Round((decimal)totalAnonymous * 100m / totalMixOrders, 2)).ToString("0.00", CultureInfo.InvariantCulture) + "%")
            });
    }

    private async Task<ReportPayload> BuildMenuPopularityReportAsync(DateTime fromUtc, DateTime toUtc, string? restaurantCsv, CancellationToken ct)
    {
        var table = await ExecuteTableAsync(
            """
            SELECT
                RestaurantId,
                RestaurantName,
                MenuItemId,
                MenuItemName,
                SUM(Quantity) AS QuantitySold,
                COUNT(DISTINCT OrderId) AS OrderCount,
                CAST(SUM(LineTotal) AS decimal(18, 2)) AS Revenue
            FROM dbo.vw_ReportOrderFacts
            WHERE CreatedAtUtc >= @FromUtc
              AND CreatedAtUtc <= @ToUtc
              AND Status NOT IN (0, 6)
              AND (@RestaurantIdsCsv IS NULL OR RestaurantId IN (
                    SELECT TRY_CAST(value AS int)
                    FROM STRING_SPLIT(@RestaurantIdsCsv, ',')
                    WHERE TRY_CAST(value AS int) IS NOT NULL))
            GROUP BY RestaurantId, RestaurantName, MenuItemId, MenuItemName
            ORDER BY QuantitySold DESC, Revenue DESC, MenuItemName
            """,
            CommandType.Text,
            ct,
            new SqlParameter("@FromUtc", SqlDbType.DateTime2) { Value = fromUtc },
            new SqlParameter("@ToUtc", SqlDbType.DateTime2) { Value = toUtc },
            new SqlParameter("@RestaurantIdsCsv", SqlDbType.NVarChar) { Value = (object?)restaurantCsv ?? DBNull.Value });

        var totalQuantity = table.AsEnumerable().Sum(row => row.Field<int?>("QuantitySold") ?? 0);
        var distinctItems = table.Rows.Count;
        var topDish = table.Rows.Count == 0
            ? "-"
            : Convert.ToString(table.Rows[0]["MenuItemName"], CultureInfo.InvariantCulture) ?? "-";

        return new ReportPayload(
            "reports.menuPopularityTitle",
            "Menu popularity",
            "menu-popularity",
            "MenuPopularity",
            table,
            new List<ReportSummaryMetric>
            {
                new("reports.totalItemsSold", "Items sold", totalQuantity.ToString(CultureInfo.InvariantCulture)),
                new("reports.uniqueMenuItems", "Unique dishes", distinctItems.ToString(CultureInfo.InvariantCulture)),
                new("reports.topDish", "Top dish", topDish)
            });
    }

    private async Task<ReportPayload> BuildUserActivityReportAsync(DateTime fromUtc, DateTime toUtc, List<int>? effectiveRestaurantIds, CancellationToken ct)
    {
        var users = await _db.Users
            .AsNoTracking()
            .Select(user => new
            {
                user.Id,
                user.Email,
                user.UserName,
                user.EmailConfirmed,
                user.RegisteredAtUtc,
                user.LastLoginAtUtc,
                user.SuccessfulLoginCount
            })
            .ToListAsync(ct);

        var restaurantAssignments = await _db.RestaurantUserRoles
            .AsNoTracking()
            .Where(x => effectiveRestaurantIds == null || effectiveRestaurantIds.Contains(x.RestaurantId))
            .ToListAsync(ct);

        var orderSummaries = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Status != OrderStatus.Draft)
            .Where(o => effectiveRestaurantIds == null || (o.RestaurantId.HasValue && effectiveRestaurantIds.Contains(o.RestaurantId.Value)))
            .GroupBy(o => o.CustomerId)
            .Select(group => new
            {
                CustomerId = group.Key,
                TotalOrders = group.Count(),
                CompletedOrders = group.Count(x => x.Status == OrderStatus.Completed),
                LastOrderAtUtc = group.Max(x => (DateTime?)x.CreatedAt)
            })
            .ToListAsync(ct);

        var assignmentLookup = restaurantAssignments
            .GroupBy(x => x.UserId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Count = group.Select(x => x.RestaurantId).Distinct().Count(),
                    Roles = string.Join(", ", group.Select(x => x.Role).Distinct().OrderBy(x => x))
                });

        var orderLookup = orderSummaries.ToDictionary(x => x.CustomerId ?? string.Empty, x => x);
        var rows = users
            .Where(user =>
            {
                var hasAssignment = assignmentLookup.ContainsKey(user.Id);
                var hasOrders = orderLookup.ContainsKey(user.Id);
                return effectiveRestaurantIds == null || hasAssignment || hasOrders;
            })
            .OrderByDescending(user => user.RegisteredAtUtc)
            .ToList();

        var table = new DataTable("UserActivity");
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("Confirmed", typeof(string));
        table.Columns.Add("RegisteredAtUtc", typeof(DateTime));
        table.Columns.Add("LastLoginAtUtc", typeof(DateTime));
        table.Columns.Add("SuccessfulLoginCount", typeof(int));
        table.Columns.Add("AssignedRestaurantCount", typeof(int));
        table.Columns.Add("AssignedRoles", typeof(string));
        table.Columns.Add("TotalOrders", typeof(int));
        table.Columns.Add("CompletedOrders", typeof(int));
        table.Columns.Add("LastOrderAtUtc", typeof(DateTime));

        foreach (var user in rows)
        {
            assignmentLookup.TryGetValue(user.Id, out var assignment);
            orderLookup.TryGetValue(user.Id, out var orders);

            table.Rows.Add(
                user.Email ?? user.UserName ?? user.Id,
                user.EmailConfirmed ? "Yes" : "No",
                user.RegisteredAtUtc,
                user.LastLoginAtUtc.HasValue ? user.LastLoginAtUtc.Value : DBNull.Value,
                user.SuccessfulLoginCount,
                assignment?.Count ?? 0,
                assignment?.Roles ?? "-",
                orders?.TotalOrders ?? 0,
                orders?.CompletedOrders ?? 0,
                orders?.LastOrderAtUtc.HasValue == true ? orders.LastOrderAtUtc.Value : DBNull.Value);
        }

        var regularLoginThreshold = DateTime.UtcNow.AddDays(-30);
        var regularLogins = rows.Count(user => user.SuccessfulLoginCount >= 3 && user.LastLoginAtUtc.HasValue && user.LastLoginAtUtc.Value >= regularLoginThreshold);
        var recentlyActive = rows.Count(user => user.LastLoginAtUtc.HasValue && user.LastLoginAtUtc.Value >= regularLoginThreshold);
        var newAccounts = rows.Count(user => user.RegisteredAtUtc >= fromUtc && user.RegisteredAtUtc <= toUtc);
        var orderingUsers = rows.Count(user => orderLookup.ContainsKey(user.Id));

        return new ReportPayload(
            "reports.userActivityTitle",
            "User activity",
            "user-activity",
            "UserActivity",
            table,
            new List<ReportSummaryMetric>
            {
                new("reports.newAccounts", "New accounts", newAccounts.ToString(CultureInfo.InvariantCulture)),
                new("reports.recentlyActiveUsers", "Active in last 30 days", recentlyActive.ToString(CultureInfo.InvariantCulture)),
                new("reports.regularLogins", "Regular logins", regularLogins.ToString(CultureInfo.InvariantCulture)),
                new("reports.usersWithOrders", "Users with orders", orderingUsers.ToString(CultureInfo.InvariantCulture))
            });
    }

    private ReportResultDto ToDto(ReportPayload payload)
    {
        var columns = payload.Table.Columns
            .Cast<DataColumn>()
            .Select(column => new ReportColumnDto(
                column.ColumnName,
                $"reports.columns.{column.ColumnName}",
                payload.ColumnLabels.TryGetValue(column.ColumnName, out var label) ? label : column.ColumnName))
            .ToList();

        var rows = payload.Table.Rows
            .Cast<DataRow>()
            .Select(row =>
            {
                var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (DataColumn column in payload.Table.Columns)
                {
                    dictionary[column.ColumnName] = NormalizeValue(row[column]);
                }

                    return dictionary;
            })
            .ToList();

        return new ReportResultDto(
            TitleKey: payload.TitleKey,
            Title: payload.Title,
            Columns: columns,
            Rows: rows,
            Summary: payload.Summary.Select(metric => new ReportSummaryMetricDto(metric.LabelKey, metric.Label, metric.Value)).ToList());
    }

    private async Task<List<int>?> GetEffectiveRestaurantIdsAsync(int[]? requestedRestaurantIds, CancellationToken ct)
    {
        var normalizedRequested = requestedRestaurantIds?
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (User.IsInRole("Admin"))
        {
            return normalizedRequested is { Count: > 0 } ? normalizedRequested : null;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing user id.");

        var allowed = await _db.RestaurantUserRoles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.RestaurantId)
            .Distinct()
            .ToListAsync(ct);

        if (normalizedRequested is not { Count: > 0 })
        {
            return allowed;
        }

        var intersection = allowed.Intersect(normalizedRequested).ToList();
        if (intersection.Count == 0)
        {
            throw new InvalidOperationException("No accessible restaurants selected.");
        }

        return intersection;
    }

    private static (DateTime fromUtc, DateTime toUtc) NormalizeDateRange(DateTime? from, DateTime? to)
    {
        var fromUtc = (from?.Date ?? DateTime.UtcNow.Date.AddDays(-30)).ToUniversalTime();
        var toUtc = ((to?.Date ?? DateTime.UtcNow.Date).AddDays(1).AddTicks(-1)).ToUniversalTime();
        if (toUtc < fromUtc)
        {
            throw new InvalidOperationException("Invalid date range.");
        }

        return (fromUtc, toUtc);
    }

    private static string? ToRestaurantCsv(List<int>? restaurantIds)
    {
        return restaurantIds is { Count: > 0 }
            ? string.Join(",", restaurantIds.OrderBy(id => id))
            : null;
    }

    private async Task<DataTable> ExecuteTableAsync(string commandText, CommandType commandType, CancellationToken ct, params SqlParameter[] parameters)
    {
        var connection = _db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(ct);
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = commandText;
            command.CommandType = commandType;
            command.CommandTimeout = 60;

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            using var reader = await command.ExecuteReaderAsync(ct);
            var table = new DataTable();
            table.Load(reader);
            return table;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static object? NormalizeValue(object? value)
    {
        return value switch
        {
            null => null,
            DBNull => null,
            DateTime dateTime => dateTime,
            decimal dec => dec,
            double dbl => dbl,
            float flt => flt,
            byte b => (int)b,
            short s => (int)s,
            _ => value
        };
    }

    private sealed record ReportPayload(
        string TitleKey,
        string Title,
        string FileName,
        string SheetName,
        DataTable Table,
        IReadOnlyList<ReportSummaryMetric> Summary)
    {
        public Dictionary<string, string> ColumnLabels { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            ["BusinessDate"] = "Date",
            ["RestaurantId"] = "Restaurant ID",
            ["RestaurantName"] = "Restaurant",
            ["OrderType"] = "Order type",
            ["OrderCount"] = "Orders",
            ["CompletedOrders"] = "Completed",
            ["CancelledOrders"] = "Cancelled",
            ["AnonymousOrders"] = "Anonymous",
            ["RegisteredOrders"] = "Registered",
            ["GrossRevenue"] = "Revenue",
            ["AverageOrderValue"] = "Average order",
            ["CustomerLabel"] = "Customer",
            ["IsAnonymousCustomer"] = "Anonymous",
            ["CompletedOrderCount"] = "Completed orders",
            ["CancelledOrderCount"] = "Cancelled orders",
            ["TotalSpent"] = "Spend",
            ["LastOrderAtUtc"] = "Last order",
            ["MenuItemId"] = "Menu item ID",
            ["MenuItemName"] = "Dish",
            ["QuantitySold"] = "Quantity sold",
            ["Revenue"] = "Revenue",
            ["RegisteredRevenue"] = "Registered revenue",
            ["AnonymousRevenue"] = "Anonymous revenue",
            ["AnonymousSharePercent"] = "Anonymous share %",
            ["Email"] = "Email",
            ["Confirmed"] = "Confirmed",
            ["RegisteredAtUtc"] = "Registered at",
            ["LastLoginAtUtc"] = "Last login",
            ["SuccessfulLoginCount"] = "Login count",
            ["AssignedRestaurantCount"] = "Restaurant assignments",
            ["AssignedRoles"] = "Roles",
            ["TotalOrders"] = "Total orders"
        };
    }

    private sealed record ReportSummaryMetric(string LabelKey, string Label, string Value);

    public sealed record ReportMetaDto(bool CanChooseRestaurants, bool CanSeeUserActivity, IReadOnlyList<ReportRestaurantOptionDto> Restaurants);
    public sealed record ReportRestaurantOptionDto(int Id, string Name);
    public sealed record ReportResultDto(string TitleKey, string Title, IReadOnlyList<ReportColumnDto> Columns, IReadOnlyList<Dictionary<string, object?>> Rows, IReadOnlyList<ReportSummaryMetricDto> Summary);
    public sealed record ReportColumnDto(string Key, string LabelKey, string Label);
    public sealed record ReportSummaryMetricDto(string LabelKey, string Label, string Value);
}
