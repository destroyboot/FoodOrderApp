using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReportsAndUserLoginMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAtUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAtUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "SuccessfulLoginCount",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                CREATE OR ALTER VIEW dbo.vw_ReportOrderFacts
                AS
                SELECT
                    o.Id AS OrderId,
                    o.DailyRestaurantOrderNumber,
                    o.RestaurantId,
                    ISNULL(r.Name, '-') AS RestaurantName,
                    o.CreatedAt AS CreatedAtUtc,
                    CAST(o.CreatedAt AS date) AS BusinessDate,
                    CAST(o.OrderType AS int) AS OrderType,
                    CAST(o.Status AS int) AS Status,
                    CAST(o.PaymentMethod AS int) AS PaymentMethod,
                    CAST(o.PaymentStatus AS int) AS PaymentStatus,
                    o.CustomerId,
                    CASE WHEN u.Id IS NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsAnonymousCustomer,
                    COALESCE(u.Email, u.UserName, 'Anonymous') AS CustomerLabel,
                    oi.MenuItemId,
                    COALESCE(translatedItem.Name, CONCAT('#', oi.MenuItemId)) AS MenuItemName,
                    oi.Quantity,
                    oi.UnitPrice,
                    oi.ExtraCharge,
                    CAST((oi.UnitPrice * oi.Quantity) + ISNULL(oi.ExtraCharge, 0) AS decimal(18, 2)) AS LineTotal,
                    o.Total AS OrderTotal
                FROM Orders o
                INNER JOIN OrderItems oi ON oi.OrderId = o.Id
                LEFT JOIN Restaurants r ON r.Id = o.RestaurantId
                LEFT JOIN AspNetUsers u ON u.Id = o.CustomerId
                OUTER APPLY (
                    SELECT TOP (1) mit.Name
                    FROM MenuItemTranslations mit
                    WHERE mit.MenuItemId = oi.MenuItemId
                    ORDER BY CASE WHEN mit.Culture = 'pl-PL' THEN 0 ELSE 1 END, mit.Id
                ) translatedItem;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER FUNCTION dbo.ufn_ReportCustomerStats
                (
                    @RestaurantIdsCsv nvarchar(max) = NULL,
                    @FromUtc datetime2,
                    @ToUtc datetime2
                )
                RETURNS TABLE
                AS
                RETURN
                (
                    WITH AllowedRestaurants AS (
                        SELECT TRY_CAST(value AS int) AS RestaurantId
                        FROM STRING_SPLIT(ISNULL(@RestaurantIdsCsv, ''), ',')
                        WHERE TRY_CAST(value AS int) IS NOT NULL
                    )
                    SELECT
                        COALESCE(u.Email, u.UserName, 'Anonymous') AS CustomerLabel,
                        CASE WHEN u.Id IS NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsAnonymousCustomer,
                        COUNT(*) AS OrderCount,
                        SUM(CASE WHEN o.Status = 5 THEN 1 ELSE 0 END) AS CompletedOrderCount,
                        SUM(CASE WHEN o.Status = 6 THEN 1 ELSE 0 END) AS CancelledOrderCount,
                        CAST(SUM(CASE WHEN o.Status <> 6 THEN o.Total ELSE 0 END) AS decimal(18, 2)) AS TotalSpent,
                        MAX(o.CreatedAt) AS LastOrderAtUtc
                    FROM Orders o
                    LEFT JOIN AspNetUsers u ON u.Id = o.CustomerId
                    WHERE o.Status <> 0
                      AND o.CreatedAt >= @FromUtc
                      AND o.CreatedAt <= @ToUtc
                      AND (
                            @RestaurantIdsCsv IS NULL
                            OR LTRIM(RTRIM(@RestaurantIdsCsv)) = ''
                            OR EXISTS (
                                SELECT 1
                                FROM AllowedRestaurants ar
                                WHERE ar.RestaurantId = o.RestaurantId
                            )
                      )
                    GROUP BY
                        COALESCE(u.Email, u.UserName, 'Anonymous'),
                        CASE WHEN u.Id IS NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
                );
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.usp_ReportSalesSummary
                    @FromUtc datetime2,
                    @ToUtc datetime2,
                    @RestaurantIdsCsv nvarchar(max) = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    WITH AllowedRestaurants AS (
                        SELECT TRY_CAST(value AS int) AS RestaurantId
                        FROM STRING_SPLIT(ISNULL(@RestaurantIdsCsv, ''), ',')
                        WHERE TRY_CAST(value AS int) IS NOT NULL
                    )
                    SELECT
                        CAST(o.CreatedAt AS date) AS BusinessDate,
                        o.RestaurantId,
                        ISNULL(r.Name, '-') AS RestaurantName,
                        CAST(o.OrderType AS int) AS OrderType,
                        COUNT(*) AS OrderCount,
                        SUM(CASE WHEN o.Status = 5 THEN 1 ELSE 0 END) AS CompletedOrders,
                        SUM(CASE WHEN o.Status = 6 THEN 1 ELSE 0 END) AS CancelledOrders,
                        SUM(CASE WHEN u.Id IS NULL THEN 1 ELSE 0 END) AS AnonymousOrders,
                        SUM(CASE WHEN u.Id IS NOT NULL THEN 1 ELSE 0 END) AS RegisteredOrders,
                        CAST(SUM(CASE WHEN o.Status <> 6 THEN o.Total ELSE 0 END) AS decimal(18, 2)) AS GrossRevenue,
                        CAST(
                            CASE
                                WHEN SUM(CASE WHEN o.Status <> 6 THEN 1 ELSE 0 END) = 0 THEN 0
                                ELSE SUM(CASE WHEN o.Status <> 6 THEN o.Total ELSE 0 END)
                                    / SUM(CASE WHEN o.Status <> 6 THEN 1 ELSE 0 END)
                            END
                            AS decimal(18, 2)
                        ) AS AverageOrderValue
                    FROM Orders o
                    LEFT JOIN Restaurants r ON r.Id = o.RestaurantId
                    LEFT JOIN AspNetUsers u ON u.Id = o.CustomerId
                    WHERE o.Status <> 0
                      AND o.CreatedAt >= @FromUtc
                      AND o.CreatedAt <= @ToUtc
                      AND (
                            @RestaurantIdsCsv IS NULL
                            OR LTRIM(RTRIM(@RestaurantIdsCsv)) = ''
                            OR EXISTS (
                                SELECT 1
                                FROM AllowedRestaurants ar
                                WHERE ar.RestaurantId = o.RestaurantId
                            )
                      )
                    GROUP BY
                        CAST(o.CreatedAt AS date),
                        o.RestaurantId,
                        ISNULL(r.Name, '-'),
                        CAST(o.OrderType AS int)
                    ORDER BY
                        BusinessDate DESC,
                        RestaurantName,
                        OrderType;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_ReportSalesSummary;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS dbo.ufn_ReportCustomerStats;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_ReportOrderFacts;");

            migrationBuilder.DropColumn(
                name: "LastLoginAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RegisteredAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SuccessfulLoginCount",
                table: "AspNetUsers");
        }
    }
}
