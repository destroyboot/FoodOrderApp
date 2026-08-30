using Core.Contracts.AdminData;
using Core.Data.Entities;
using Core.Utilities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/data")]
    [Authorize(Roles = "Admin")]
    public class AdminDataController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminDataController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("tables")]
        public async Task<ActionResult<IReadOnlyList<AdminDataTableDto>>> GetTables(CancellationToken ct)
        {
            var tables = await GetAccessibleTablesAsync(ct);
            return Ok(tables.Select(MapTableDto).OrderBy(x => x.TableName).ToList());
        }

        [HttpGet("tables/{tableName}/rows")]
        public async Task<ActionResult<IReadOnlyList<Dictionary<string, object?>>>> GetRows(
            string tableName,
            [FromQuery] string? search,
            CancellationToken ct)
        {
            var table = await RequireTableAsync(tableName, "read", ct);
            using var cmd = await BuildSelectCommandAsync(table, search, ct);
            using var reader = await cmd.ExecuteReaderAsync(ct);

            var rows = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync(ct))
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var column in table.Columns)
                {
                    var value = reader[column.ColumnName];
                    row[column.Property.Name] = value == DBNull.Value ? null : value;
                }
                rows.Add(row);
            }

            return Ok(rows);
        }

        [HttpGet("tables/{tableName}/editor-options")]
        public async Task<ActionResult<IReadOnlyList<AdminDataColumnOptionsDto>>> GetEditorOptions(string tableName, CancellationToken ct)
        {
            var table = await ResolveAccessibleTableAsync(tableName, ct);
            if (table is null)
                throw new InvalidOperationException("You do not have access to this table.");

            var result = new List<AdminDataColumnOptionsDto>();
            foreach (var column in table.Columns.Where(x => x.ForeignKey is not null))
            {
                var options = await BuildForeignKeyOptionsAsync(column, ct);
                result.Add(new AdminDataColumnOptionsDto
                {
                    ColumnName = column.Property.Name,
                    Options = options
                });
            }

            return Ok(result);
        }

        [HttpPost("tables/{tableName}/rows")]
        public async Task<IActionResult> CreateRow(string tableName, [FromBody] AdminDataRowUpsertDto dto, CancellationToken ct)
        {
            var table = await RequireTableAsync(tableName, "create", ct);
            await ExecuteInsertAsync(table, dto, ct);
            return NoContent();
        }

        [HttpPut("tables/{tableName}/rows/{key}")]
        public async Task<IActionResult> UpdateRow(string tableName, string key, [FromBody] AdminDataRowUpsertDto dto, CancellationToken ct)
        {
            var table = await RequireTableAsync(tableName, "update", ct);
            var affected = await ExecuteUpdateAsync(table, key, dto, ct);
            if (affected == 0)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("tables/{tableName}/rows/{key}")]
        public async Task<IActionResult> DeleteRow(string tableName, string key, CancellationToken ct)
        {
            var table = await RequireTableAsync(tableName, "delete", ct);
            var affected = await ExecuteDeleteAsync(table, key, ct);
            if (affected == 0)
                return NotFound();

            return NoContent();
        }

        [HttpPost("tables/{tableName}/rows/bulk-delete")]
        public async Task<IActionResult> BulkDeleteRows(string tableName, [FromBody] AdminDataBulkDeleteDto dto, CancellationToken ct)
        {
            var table = await RequireTableAsync(tableName, "delete", ct);
            var keys = dto.Keys
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (keys.Count == 0)
                return NoContent();

            var affected = await ExecuteBulkDeleteAsync(table, keys, ct);
            return Ok(new { deleted = affected });
        }

        private Task<List<TableAccessDefinition>> GetAccessibleTablesAsync(CancellationToken ct)
        {
            var tables = GetTableDefinitions()
                .Select(x => x with
                {
                    Permissions = new AdminDataTablePermissionsDto
                    {
                        CanRead = true,
                        CanCreate = true,
                        CanUpdate = true,
                        CanDelete = true
                    }
                })
                .ToList();
            return Task.FromResult(tables);
        }

        private async Task<TableAccessDefinition> RequireTableAsync(string tableName, string operation, CancellationToken ct)
        {
            var table = await ResolveAccessibleTableAsync(tableName, ct);

            if (table is null)
                throw new InvalidOperationException("You do not have access to this table.");

            var allowed = operation switch
            {
                "read" => table.Permissions.CanRead,
                "create" => table.Permissions.CanCreate,
                "update" => table.Permissions.CanUpdate,
                "delete" => table.Permissions.CanDelete,
                _ => false
            };

            if (!allowed)
                throw new InvalidOperationException($"You do not have {operation} access to this table.");

            return table;
        }

        private async Task<TableAccessDefinition?> ResolveAccessibleTableAsync(string tableName, CancellationToken ct)
        {
            return (await GetAccessibleTablesAsync(ct))
                .FirstOrDefault(x => string.Equals(x.TableName, tableName, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<SqlCommand> BuildSelectCommandAsync(TableAccessDefinition table, string? search, CancellationToken ct)
        {
            var connection = (SqlConnection)_db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(ct);

            var columns = string.Join(", ", table.Columns.Select(x => $"[{x.ColumnName}]"));
            var sql = $"SELECT TOP 100 {columns} FROM [{table.TableName}]";

            var command = new SqlCommand();
            command.Connection = connection;
            var hasWhere = false;
            hasWhere = ApplyScopeFilter(command, table, ref sql, hasWhere);
            hasWhere = ApplySearchFilter(command, table, ref sql, search, hasWhere);

            sql += $" ORDER BY [{table.PrimaryKey.ColumnName}] DESC";
            command.CommandText = sql;
            return command;
        }

        private async Task ExecuteInsertAsync(TableAccessDefinition table, AdminDataRowUpsertDto dto, CancellationToken ct)
        {
            var editableColumns = table.Columns.Where(x => x.IsEditable && !x.IsPrimaryKey).ToList();
            ValidateScopeForWrite(table, dto.Values);

            var connection = (SqlConnection)_db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(ct);

            var command = new SqlCommand { Connection = connection };
            var columnNames = new List<string>();
            var valueNames = new List<string>();
            var index = 0;

            foreach (var column in editableColumns)
            {
                columnNames.Add($"[{column.ColumnName}]");
                var parameterName = $"@p{index++}";
                valueNames.Add(parameterName);
                command.Parameters.AddWithValue(parameterName, ConvertIncomingValue(dto.Values, column.Property) ?? DBNull.Value);
            }

            command.CommandText = $"INSERT INTO [{table.TableName}] ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", valueNames)})";
            await command.ExecuteNonQueryAsync(ct);
        }

        private async Task<int> ExecuteUpdateAsync(TableAccessDefinition table, string key, AdminDataRowUpsertDto dto, CancellationToken ct)
        {
            var editableColumns = table.Columns.Where(x => x.IsEditable && !x.IsPrimaryKey).ToList();
            ValidateScopeForWrite(table, dto.Values);

            var connection = (SqlConnection)_db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(ct);

            var command = new SqlCommand { Connection = connection };
            var sets = new List<string>();
            var index = 0;

            foreach (var column in editableColumns)
            {
                var parameterName = $"@p{index++}";
                sets.Add($"[{column.ColumnName}] = {parameterName}");
                command.Parameters.AddWithValue(parameterName, ConvertIncomingValue(dto.Values, column.Property) ?? DBNull.Value);
            }

            var keyParameter = "@key";
            command.Parameters.AddWithValue(keyParameter, ConvertKeyValue(key, table.PrimaryKey.Property));

            var sql = $"UPDATE [{table.TableName}] SET {string.Join(", ", sets)} WHERE [{table.PrimaryKey.ColumnName}] = {keyParameter}";
            ApplyScopeFilter(command, table, ref sql, true);
            command.CommandText = sql;
            return await command.ExecuteNonQueryAsync(ct);
        }

        private async Task<int> ExecuteDeleteAsync(TableAccessDefinition table, string key, CancellationToken ct)
        {
            var connection = (SqlConnection)_db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(ct);

            var command = new SqlCommand { Connection = connection };
            command.Parameters.AddWithValue("@key", ConvertKeyValue(key, table.PrimaryKey.Property));

            var sql = $"DELETE FROM [{table.TableName}] WHERE [{table.PrimaryKey.ColumnName}] = @key";
            ApplyScopeFilter(command, table, ref sql, true);
            command.CommandText = sql;
            return await command.ExecuteNonQueryAsync(ct);
        }

        private async Task<int> ExecuteBulkDeleteAsync(TableAccessDefinition table, IReadOnlyList<string> keys, CancellationToken ct)
        {
            var connection = (SqlConnection)_db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(ct);

            var command = new SqlCommand { Connection = connection };
            var keyNames = new List<string>();
            for (var i = 0; i < keys.Count; i++)
            {
                var parameterName = $"@key{i}";
                keyNames.Add(parameterName);
                command.Parameters.AddWithValue(parameterName, ConvertKeyValue(keys[i], table.PrimaryKey.Property));
            }

            var sql = $"DELETE FROM [{table.TableName}] WHERE [{table.PrimaryKey.ColumnName}] IN ({string.Join(", ", keyNames)})";
            ApplyScopeFilter(command, table, ref sql, true);
            command.CommandText = sql;
            return await command.ExecuteNonQueryAsync(ct);
        }

        private bool ApplyScopeFilter(SqlCommand command, TableAccessDefinition table, ref string sql, bool hasWhere)
        {
            if (User.IsInRole("Admin") || !table.IsRestaurantScoped || table.RestaurantIdColumn is null)
                return hasWhere;

            var allowedRestaurantIds = GetAllowedRestaurantIdsAsync(CancellationToken.None).GetAwaiter().GetResult();
            if (allowedRestaurantIds.Count == 0)
            {
                sql += hasWhere ? " AND 1 = 0" : " WHERE 1 = 0";
                return true;
            }

            var parameterNames = new List<string>();
            for (var i = 0; i < allowedRestaurantIds.Count; i++)
            {
                var name = $"@restaurantId{i}";
                parameterNames.Add(name);
                command.Parameters.AddWithValue(name, allowedRestaurantIds[i]);
            }

            sql += hasWhere
                ? $" AND [{table.RestaurantIdColumn.ColumnName}] IN ({string.Join(", ", parameterNames)})"
                : $" WHERE [{table.RestaurantIdColumn.ColumnName}] IN ({string.Join(", ", parameterNames)})";
            return true;
        }

        private bool ApplySearchFilter(SqlCommand command, TableAccessDefinition table, ref string sql, string? search, bool hasWhere)
        {
            var terms = TokenizedSearch.SplitTerms(search);
            if (terms.Count == 0)
                return hasWhere;

            var searchableColumns = table.Columns
                .Where(x => x.Property.ClrType != typeof(byte[]))
                .ToList();

            if (searchableColumns.Count == 0)
                return hasWhere;

            var tokenClauses = new List<string>();
            for (var i = 0; i < terms.Count; i++)
            {
                var parameterName = $"@search{i}";
                command.Parameters.AddWithValue(parameterName, $"%{terms[i].ToLowerInvariant()}%");
                var columnClause = string.Join(" OR ", searchableColumns.Select(column =>
                    $"LOWER(COALESCE(CONVERT(nvarchar(max), [{column.ColumnName}]), '')) LIKE {parameterName}"));
                tokenClauses.Add($"({columnClause})");
            }

            var combinedClause = string.Join(" AND ", tokenClauses);
            sql += hasWhere ? $" AND {combinedClause}" : $" WHERE {combinedClause}";
            return true;
        }

        private void ValidateScopeForWrite(TableAccessDefinition table, IReadOnlyDictionary<string, JsonElement?> values)
        {
            if (User.IsInRole("Admin") || !table.IsRestaurantScoped || table.RestaurantIdColumn is null)
                return;

            if (!values.TryGetValue(table.RestaurantIdColumn.Property.Name, out var restaurantIdElement))
                throw new InvalidOperationException("RestaurantId is required for this table.");

            var restaurantId = Convert.ToInt32(ConvertJsonElementToType(restaurantIdElement, typeof(int)), CultureInfo.InvariantCulture);
            var allowed = GetAllowedRestaurantIdsAsync(CancellationToken.None).GetAwaiter().GetResult();
            if (!allowed.Contains(restaurantId))
                throw new InvalidOperationException("You are not allowed to modify rows for this restaurant.");
        }

        private async Task<List<int>> GetAllowedRestaurantIdsAsync(CancellationToken ct)
        {
            if (User.IsInRole("Admin"))
                return new List<int>();

            var userId = GetCurrentUserId();
            return await _db.RestaurantUserRoles
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.Role == "RestaurantAdmin")
                .Select(x => x.RestaurantId)
                .Distinct()
                .ToListAsync(ct);
        }

        private static object ConvertKeyValue(string value, IProperty property)
        {
            return ConvertStringToType(value, Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType)
                ?? throw new InvalidOperationException("Primary key value is required.");
        }

        private static object? ConvertIncomingValue(IReadOnlyDictionary<string, JsonElement?> values, IProperty property)
        {
            if (!values.TryGetValue(property.Name, out var element))
            {
                if (!property.IsNullable && Nullable.GetUnderlyingType(property.ClrType) is null && property.ClrType != typeof(string))
                    return GetDefault(property.ClrType);

                return null;
            }

            return ConvertJsonElementToType(element, property.ClrType);
        }

        private static object? ConvertJsonElementToType(JsonElement? element, Type targetType)
        {
            if (element is null)
                return null;

            var effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (element.Value.ValueKind == JsonValueKind.Null || element.Value.ValueKind == JsonValueKind.Undefined)
                return null;

            if (effectiveType == typeof(string))
                return element.Value.GetString();

            if (effectiveType == typeof(int))
                return element.Value.ValueKind == JsonValueKind.String
                    ? int.Parse(element.Value.GetString()!, CultureInfo.InvariantCulture)
                    : element.Value.GetInt32();

            if (effectiveType == typeof(long))
                return element.Value.ValueKind == JsonValueKind.String
                    ? long.Parse(element.Value.GetString()!, CultureInfo.InvariantCulture)
                    : element.Value.GetInt64();

            if (effectiveType == typeof(decimal))
                return element.Value.ValueKind == JsonValueKind.String
                    ? decimal.Parse(element.Value.GetString()!, CultureInfo.InvariantCulture)
                    : element.Value.GetDecimal();

            if (effectiveType == typeof(double))
                return element.Value.ValueKind == JsonValueKind.String
                    ? double.Parse(element.Value.GetString()!, CultureInfo.InvariantCulture)
                    : element.Value.GetDouble();

            if (effectiveType == typeof(bool))
                return element.Value.ValueKind == JsonValueKind.String
                    ? bool.Parse(element.Value.GetString()!)
                    : element.Value.GetBoolean();

            if (effectiveType == typeof(DateTime))
                return element.Value.ValueKind == JsonValueKind.String
                    ? DateTime.Parse(element.Value.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                    : element.Value.GetDateTime();

            if (effectiveType.IsEnum)
            {
                if (element.Value.ValueKind == JsonValueKind.Number)
                    return Enum.ToObject(effectiveType, element.Value.GetInt32());

                return Enum.Parse(effectiveType, element.Value.GetString()!, true);
            }

            return JsonSerializer.Deserialize(element.Value.GetRawText(), effectiveType);
        }

        private static object? ConvertStringToType(string value, Type targetType)
        {
            if (targetType == typeof(string))
                return value;

            if (targetType == typeof(int))
                return int.Parse(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(long))
                return long.Parse(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(Guid))
                return Guid.Parse(value);

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        private string GetCurrentUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User id is missing");

        private List<TableAccessDefinition> GetTableDefinitions()
        {
            return _db.Model.GetEntityTypes()
                .Where(x => x.ClrType.Namespace == "Core.Data.Entities")
                .Where(x => x.GetTableName() is not null)
                .Select(entityType =>
                {
                    var tableName = entityType.GetTableName()!;
                    var tableIdentifier = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());
                    var primaryKey = entityType.FindPrimaryKey();
                    if (primaryKey is null || primaryKey.Properties.Count != 1)
                        return null;

                    var columns = entityType.GetProperties()
                        .Where(x => !x.IsShadowProperty())
                        .Select(property => new TableColumnDefinition(
                            property,
                            property.GetColumnName(tableIdentifier) ?? property.Name,
                            property == primaryKey.Properties[0],
                            IsEditable(property, primaryKey.Properties[0]),
                            entityType.GetForeignKeys().FirstOrDefault(foreignKey => foreignKey.Properties.Count == 1 && foreignKey.Properties[0] == property),
                            GetPreferredLabelPropertyName(entityType.GetForeignKeys().FirstOrDefault(foreignKey => foreignKey.Properties.Count == 1 && foreignKey.Properties[0] == property)?.PrincipalEntityType)))
                        .ToList();

                    var restaurantIdColumn = columns.FirstOrDefault(x =>
                        string.Equals(x.Property.Name, "RestaurantId", StringComparison.OrdinalIgnoreCase) &&
                        (x.Property.ClrType == typeof(int) || x.Property.ClrType == typeof(int?)));

                    return new TableAccessDefinition(
                        entityType,
                        tableName,
                        entityType.ClrType.Name,
                        columns.First(x => x.IsPrimaryKey),
                        columns,
                        restaurantIdColumn is not null,
                        restaurantIdColumn,
                        new AdminDataTablePermissionsDto());
                })
                .Where(x => x is not null)
                .Cast<TableAccessDefinition>()
                .ToList();
        }

        private static bool IsEditable(IProperty property, IProperty primaryKey)
        {
            if (property == primaryKey)
                return false;

            if (property.ValueGenerated != ValueGenerated.Never && property.GetBeforeSaveBehavior() != PropertySaveBehavior.Save)
                return false;

            return true;
        }

        private static AdminDataTableDto MapTableDto(TableAccessDefinition table) => new()
        {
            TableName = table.TableName,
            DisplayName = table.DisplayName,
            PrimaryKeyName = table.PrimaryKey.Property.Name,
            IsRestaurantScoped = table.IsRestaurantScoped,
            Permissions = table.Permissions,
            Columns = table.Columns.Select(x => new AdminDataColumnDto
            {
                Name = x.Property.Name,
                DataType = GetDataTypeName(x.Property.ClrType),
                IsNullable = x.Property.IsNullable,
                IsPrimaryKey = x.IsPrimaryKey,
                IsEditable = x.IsEditable,
                EnumValues = GetEnumValues(x.Property.ClrType),
                ForeignKeyTableName = x.ForeignKey?.PrincipalEntityType.GetTableName(),
                ForeignKeyPrimaryKeyName = x.ForeignKey?.PrincipalKey.Properties.Count == 1 ? x.ForeignKey.PrincipalKey.Properties[0].Name : null,
                ForeignKeyLabelPropertyName = x.ForeignKeyLabelPropertyName
            }).ToList()
        };

        private static string GetDataTypeName(Type type)
        {
            var effectiveType = Nullable.GetUnderlyingType(type) ?? type;
            if (effectiveType.IsEnum)
                return "enum";

            if (effectiveType == typeof(int) || effectiveType == typeof(long) || effectiveType == typeof(decimal) || effectiveType == typeof(double))
                return "number";

            if (effectiveType == typeof(bool))
                return "boolean";

            if (effectiveType == typeof(DateTime))
                return "datetime";

            return "string";
        }

        private async Task<List<AdminDataOptionDto>> BuildForeignKeyOptionsAsync(TableColumnDefinition column, CancellationToken ct)
        {
            var foreignKey = column.ForeignKey
                ?? throw new InvalidOperationException("Column is not a foreign key.");

            var principalType = foreignKey.PrincipalEntityType;
            var specialLabels = await TryBuildSpecialForeignKeyOptionsAsync(principalType, ct);
            if (specialLabels is not null)
                return specialLabels;

            var principalTableName = principalType.GetTableName()
                ?? throw new InvalidOperationException("Referenced table not found.");

            var principalPrimaryKey = foreignKey.PrincipalKey.Properties.Single();
            var tableIdentifier = StoreObjectIdentifier.Table(principalTableName, principalType.GetSchema());
            var primaryKeyColumnName = principalPrimaryKey.GetColumnName(tableIdentifier) ?? principalPrimaryKey.Name;
            var labelProperty = GetPreferredLabelProperty(principalType);
            var labelColumnName = labelProperty?.GetColumnName(tableIdentifier) ?? primaryKeyColumnName;
            var restaurantIdProperty = principalType.GetProperties()
                .FirstOrDefault(x => string.Equals(x.Name, "RestaurantId", StringComparison.OrdinalIgnoreCase) && (x.ClrType == typeof(int) || x.ClrType == typeof(int?)));
            var restaurantIdColumnName = restaurantIdProperty?.GetColumnName(tableIdentifier);

            var connection = (SqlConnection)_db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(ct);

            var command = new SqlCommand { Connection = connection };
            var sql = $"SELECT TOP 100 [{primaryKeyColumnName}], [{labelColumnName}] FROM [{principalTableName}]";

            if (!User.IsInRole("Admin") && restaurantIdColumnName is not null)
            {
                var allowedRestaurantIds = await GetAllowedRestaurantIdsAsync(ct);
                if (allowedRestaurantIds.Count == 0)
                    return new List<AdminDataOptionDto>();

                var parameterNames = new List<string>();
                for (var i = 0; i < allowedRestaurantIds.Count; i++)
                {
                    var name = $"@restaurantId{i}";
                    parameterNames.Add(name);
                    command.Parameters.AddWithValue(name, allowedRestaurantIds[i]);
                }

                sql += $" WHERE [{restaurantIdColumnName}] IN ({string.Join(", ", parameterNames)})";
            }

            sql += $" ORDER BY [{labelColumnName}]";
            command.CommandText = sql;

            var options = new List<AdminDataOptionDto>();
            using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var value = reader[primaryKeyColumnName];
                var label = reader[labelColumnName];
                options.Add(new AdminDataOptionDto
                {
                    Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
                    Label = label == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(label, CultureInfo.InvariantCulture))
                        ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
                        : Convert.ToString(label, CultureInfo.InvariantCulture) ?? ""
                });
            }

            return options;
        }

        private async Task<List<AdminDataOptionDto>?> TryBuildSpecialForeignKeyOptionsAsync(IEntityType principalType, CancellationToken ct)
        {
            var entityName = principalType.ClrType.Name;
            var allowedRestaurantIds = User.IsInRole("Admin")
                ? null
                : await GetAllowedRestaurantIdsAsync(ct);

            if (entityName == nameof(MenuCategory))
            {
                var categories = await _db.MenuCategories
                    .AsNoTracking()
                    .Include(x => x.Translations)
                    .Where(x => allowedRestaurantIds == null || allowedRestaurantIds.Contains(x.RestaurantId))
                    .OrderBy(x => x.Id)
                    .ToListAsync(ct);

                return categories
                    .Select(x => new AdminDataOptionDto
                    {
                        Value = x.Id.ToString(CultureInfo.InvariantCulture),
                        Label = $"#{x.Id} - {ResolveTranslationLabel(x.Translations.Select(t => (t.Culture, t.Name)).ToList(), "Category")}"
                    })
                    .ToList();
            }

            if (entityName == nameof(MenuItem))
            {
                var items = await _db.MenuItems
                    .AsNoTracking()
                    .Include(x => x.Translations)
                    .Include(x => x.Category)
                    .ThenInclude(x => x!.Translations)
                    .Where(x => allowedRestaurantIds == null || allowedRestaurantIds.Contains(x.Category!.RestaurantId))
                    .OrderBy(x => x.Id)
                    .ToListAsync(ct);

                return items
                    .Select(x => new AdminDataOptionDto
                    {
                        Value = x.Id.ToString(CultureInfo.InvariantCulture),
                        Label = $"#{x.Id} - {ResolveTranslationLabel(x.Translations.Select(t => (t.Culture, t.Name)).ToList(), "Item")} ({ResolveTranslationLabel(x.Category!.Translations.Select(t => (t.Culture, t.Name)).ToList(), "Category")})"
                    })
                    .ToList();
            }

            if (entityName == nameof(Ingredient))
            {
                var ingredients = await _db.Ingredients
                    .AsNoTracking()
                    .Include(x => x.Translations)
                    .Where(x => allowedRestaurantIds == null || allowedRestaurantIds.Contains(x.RestaurantId))
                    .OrderBy(x => x.Id)
                    .ToListAsync(ct);

                return ingredients
                    .Select(x => new AdminDataOptionDto
                    {
                        Value = x.Id.ToString(CultureInfo.InvariantCulture),
                        Label = $"#{x.Id} - {ResolveTranslationLabel(x.Translations.Select(t => (t.Culture, t.Name)).ToList(), "Ingredient")}"
                    })
                    .ToList();
            }

            if (entityName == nameof(Order))
            {
                return await _db.Orders
                    .AsNoTracking()
                    .Where(x => allowedRestaurantIds == null || (x.RestaurantId.HasValue && allowedRestaurantIds.Contains(x.RestaurantId.Value)))
                    .OrderByDescending(x => x.Id)
                    .Take(200)
                    .Select(x => new AdminDataOptionDto
                    {
                        Value = x.Id.ToString(CultureInfo.InvariantCulture),
                        Label = $"Order #{x.Id} - {x.Status} - {x.CreatedAt:yyyy-MM-dd HH:mm}"
                    })
                    .ToListAsync(ct);
            }

            if (entityName == nameof(Reservation))
            {
                return await _db.Reservations
                    .AsNoTracking()
                    .Where(x => allowedRestaurantIds == null || allowedRestaurantIds.Contains(x.RestaurantId))
                    .OrderByDescending(x => x.Id)
                    .Take(200)
                    .Select(x => new AdminDataOptionDto
                    {
                        Value = x.Id.ToString(CultureInfo.InvariantCulture),
                        Label = $"Reservation #{x.Id} - {x.StartAt:yyyy-MM-dd HH:mm}"
                    })
                    .ToListAsync(ct);
            }

            if (entityName == nameof(RestaurantUserRole))
            {
                return await _db.RestaurantUserRoles
                    .AsNoTracking()
                    .Where(x => allowedRestaurantIds == null || allowedRestaurantIds.Contains(x.RestaurantId))
                    .OrderByDescending(x => x.Id)
                    .Take(200)
                    .Select(x => new AdminDataOptionDto
                    {
                        Value = x.Id.ToString(CultureInfo.InvariantCulture),
                        Label = $"Assignment #{x.Id} - {x.Role}"
                    })
                    .ToListAsync(ct);
            }

            return null;
        }

        private static List<string> GetEnumValues(Type type)
        {
            var effectiveType = Nullable.GetUnderlyingType(type) ?? type;
            return effectiveType.IsEnum
                ? Enum.GetNames(effectiveType).ToList()
                : new List<string>();
        }

        private static string ResolveTranslationLabel(IReadOnlyList<(string Culture, string Name)> translations, string fallback)
        {
            if (translations.Count == 0)
                return fallback;

            var preferred = translations.FirstOrDefault(x => string.Equals(x.Culture, "en-US", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(preferred.Name))
                return preferred.Name;

            preferred = translations.FirstOrDefault(x => string.Equals(x.Culture, "pl-PL", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(preferred.Name))
                return preferred.Name;

            return translations.Select(x => x.Name).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? fallback;
        }

        private static IProperty? GetPreferredLabelProperty(IEntityType? entityType)
        {
            if (entityType is null)
                return null;

            var preferredNames = new[] { "Name", "Label", "Title", "Email", "UserName", "Code" };
            foreach (var name in preferredNames)
            {
                var match = entityType.GetProperties()
                    .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase) && x.ClrType == typeof(string));
                if (match is not null)
                    return match;
            }

            return entityType.GetProperties().FirstOrDefault(x => x.ClrType == typeof(string));
        }

        private static string? GetPreferredLabelPropertyName(IEntityType? entityType) =>
            GetPreferredLabelProperty(entityType)?.Name;

        private static object? GetDefault(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

        private sealed record TableAccessDefinition(
            IEntityType EntityType,
            string TableName,
            string DisplayName,
            TableColumnDefinition PrimaryKey,
            List<TableColumnDefinition> Columns,
            bool IsRestaurantScoped,
            TableColumnDefinition? RestaurantIdColumn,
            AdminDataTablePermissionsDto Permissions);

        private sealed record TableColumnDefinition(
            IProperty Property,
            string ColumnName,
            bool IsPrimaryKey,
            bool IsEditable,
            IForeignKey? ForeignKey,
            string? ForeignKeyLabelPropertyName);
    }
}
