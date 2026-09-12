using Core.Contracts.AdminData;
using Core.Data.Entities;
using Core.Utilities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/data")]
    [Authorize(Roles = "Admin,RestaurantAdmin,Waiter,Chef,DeliveryDriver")]
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
            [FromQuery] string? sortColumn,
            [FromQuery] string? sortDirection,
            CancellationToken ct)
        {
            var table = await RequireTableAsync(tableName, "read", ct);
            var rows = await QueryRowsAsync(table, search, sortColumn, sortDirection, ct);
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

        [HttpGet("permission-groups")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IReadOnlyList<AdminDataRolePermissionsDto>>> GetPermissionGroups(CancellationToken ct)
        {
            var tables = GetTableDefinitions()
                .Select(x => x.TableName)
                .OrderBy(x => x)
                .ToList();
            var roleNames = await GetManageableRoleNamesAsync(ct);
            var permissions = await _db.AdminTablePermissions
                .AsNoTracking()
                .Where(x => roleNames.Contains(x.RoleName))
                .ToListAsync(ct);

            var result = roleNames.Select(roleName => new AdminDataRolePermissionsDto
            {
                RoleName = roleName,
                Grants = tables.Select(tableName =>
                {
                    var permission = permissions.FirstOrDefault(x =>
                        string.Equals(x.RoleName, roleName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.TableName, tableName, StringComparison.OrdinalIgnoreCase));

                    return new AdminDataTableGrantDto
                    {
                        TableName = tableName,
                        CanRead = permission?.CanRead ?? false,
                        CanCreate = permission?.CanCreate ?? false,
                        CanUpdate = permission?.CanUpdate ?? false,
                        CanDelete = permission?.CanDelete ?? false
                    };
                }).ToList()
            }).ToList();

            return Ok(result);
        }

        [HttpPut("permission-groups/{roleName}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePermissionGroup(string roleName, [FromBody] AdminDataRolePermissionsDto dto, CancellationToken ct)
        {
            var normalizedRole = await RequireManageableRoleNameAsync(roleName, ct);
            var validTables = GetTableDefinitions()
                .Select(x => x.TableName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var requested = dto.Grants
                .Where(x => validTables.Contains(x.TableName))
                .GroupBy(x => x.TableName, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Last())
                .ToList();

            var existing = await _db.AdminTablePermissions
                .Where(x => x.RoleName == normalizedRole)
                .ToListAsync(ct);

            foreach (var grant in requested)
            {
                var permission = existing.FirstOrDefault(x =>
                    string.Equals(x.TableName, grant.TableName, StringComparison.OrdinalIgnoreCase));
                if (permission is null)
                {
                    permission = new AdminTablePermission
                    {
                        RoleName = normalizedRole,
                        TableName = grant.TableName
                    };
                    _db.AdminTablePermissions.Add(permission);
                    existing.Add(permission);
                }

                permission.CanRead = grant.CanRead;
                permission.CanCreate = grant.CanCreate;
                permission.CanUpdate = grant.CanUpdate;
                permission.CanDelete = grant.CanDelete;
            }

            foreach (var stale in existing.Where(x => !requested.Any(grant => string.Equals(grant.TableName, x.TableName, StringComparison.OrdinalIgnoreCase))).ToList())
            {
                _db.AdminTablePermissions.Remove(stale);
            }

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        private async Task<List<TableAccessDefinition>> GetAccessibleTablesAsync(CancellationToken ct)
        {
            var tables = GetTableDefinitions();
            if (User.IsInRole("Admin"))
            {
                return tables.Select(x => x with
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
            }

            var roleNames = User.FindAll(ClaimTypes.Role)
                .Select(x => x.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (roleNames.Count == 0)
                return new List<TableAccessDefinition>();

            var grants = await _db.AdminTablePermissions
                .AsNoTracking()
                .Where(x => roleNames.Contains(x.RoleName))
                .ToListAsync(ct);

            return tables
                .Select(table =>
                {
                    var tableGrants = grants
                        .Where(x => string.Equals(x.TableName, table.TableName, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    return table with
                    {
                        Permissions = new AdminDataTablePermissionsDto
                        {
                            CanRead = tableGrants.Any(x => x.CanRead),
                            CanCreate = tableGrants.Any(x => x.CanCreate),
                            CanUpdate = tableGrants.Any(x => x.CanUpdate),
                            CanDelete = tableGrants.Any(x => x.CanDelete)
                        }
                    };
                })
                .Where(x => x.Permissions.CanRead || x.Permissions.CanCreate || x.Permissions.CanUpdate || x.Permissions.CanDelete)
                .ToList();
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

        private async Task<List<Dictionary<string, object?>>> QueryRowsAsync(
            TableAccessDefinition table,
            string? search,
            string? sortColumn,
            string? sortDirection,
            CancellationToken ct)
        {
            var allowedRestaurantIds = await GetAllowedRestaurantIdsAsync(ct);
            var query = ApplySearchQuery(
                ApplyScopeQuery(CreateEntityQuery(table), table, allowedRestaurantIds),
                table,
                search);
            var sorted = ApplySort(query, table, sortColumn, sortDirection);
            var rows = await ToListAsync(sorted.Cast<object>().Take(100), ct);

            var filtered = rows
                .Select(entity => ToRowDictionary(entity, table))
                .ToList();

            return filtered;
        }

        private async Task ExecuteInsertAsync(TableAccessDefinition table, AdminDataRowUpsertDto dto, CancellationToken ct)
        {
            var editableColumns = table.Columns.Where(x => x.IsEditable && !x.IsPrimaryKey).ToList();
            ValidateScopeForWrite(table, dto.Values);
            var entity = Activator.CreateInstance(table.EntityType.ClrType)
                ?? throw new InvalidOperationException("Could not create entity instance.");

            foreach (var column in editableColumns)
            {
                SetEntityValue(entity, column.Property, ConvertIncomingValue(dto.Values, column.Property));
            }

            _db.Add(entity);
            await _db.SaveChangesAsync(ct);
            await EnsureDefaultTranslationsAsync(entity, ct);
        }

        private async Task<int> ExecuteUpdateAsync(TableAccessDefinition table, string key, AdminDataRowUpsertDto dto, CancellationToken ct)
        {
            var editableColumns = table.Columns.Where(x => x.IsEditable && !x.IsPrimaryKey).ToList();
            ValidateScopeForWrite(table, dto.Values);
            var entity = await FindEntityAsync(table, key, ct);
            if (entity is null)
                return 0;

            await EnsureEntityScopeAsync(table, entity, ct);

            foreach (var column in editableColumns)
            {
                SetEntityValue(entity, column.Property, ConvertIncomingValue(dto.Values, column.Property));
            }

            await EnsureEntityScopeAsync(table, entity, ct);
            await _db.SaveChangesAsync(ct);
            return 1;
        }

        private async Task<int> ExecuteDeleteAsync(TableAccessDefinition table, string key, CancellationToken ct)
        {
            var entity = await FindEntityAsync(table, key, ct);
            if (entity is null)
                return 0;

            await EnsureEntityScopeAsync(table, entity, ct);
            await EnsureCanDeleteEntityAsync(table, entity, ct);
            _db.Remove(entity);
            await _db.SaveChangesAsync(ct);

            return 1;
        }

        private async Task<int> ExecuteBulkDeleteAsync(TableAccessDefinition table, IReadOnlyList<string> keys, CancellationToken ct)
        {
            var deleted = 0;
            foreach (var key in keys)
            {
                var entity = await FindEntityAsync(table, key, ct);
                if (entity is null)
                    continue;

                await EnsureEntityScopeAsync(table, entity, ct);
                await EnsureCanDeleteEntityAsync(table, entity, ct);
                _db.Remove(entity);
                deleted++;
            }

            if (deleted > 0)
            {
                await _db.SaveChangesAsync(ct);
            }

            return deleted;
        }

        private IQueryable CreateEntityQuery(TableAccessDefinition table)
            => CreateEntityQuery(table.EntityType.ClrType);

        private IQueryable CreateEntityQuery(Type entityType)
        {
            var setMethod = typeof(DbContext)
                .GetMethods()
                .Single(method => method.Name == nameof(DbContext.Set) && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);

            var set = setMethod.MakeGenericMethod(entityType).Invoke(_db, null)
                ?? throw new InvalidOperationException("Could not create query.");
            return (IQueryable)set;
        }

        private IQueryable ApplyScopeQuery(IQueryable query, TableAccessDefinition table, IReadOnlyCollection<int> allowedRestaurantIds)
        {
            if (User.IsInRole("Admin") || !table.IsRestaurantScoped || table.RestaurantIdColumn is null)
                return query;

            if (allowedRestaurantIds.Count == 0)
            {
                return ApplyConstantFilter(query, false);
            }

            var parameter = System.Linq.Expressions.Expression.Parameter(query.ElementType, "entity");
            var property = System.Linq.Expressions.Expression.Property(parameter, table.RestaurantIdColumn.Property.Name);
            System.Linq.Expressions.Expression predicate;

            if (Nullable.GetUnderlyingType(property.Type) is not null)
            {
                var hasValue = System.Linq.Expressions.Expression.Property(property, nameof(Nullable<int>.HasValue));
                var value = System.Linq.Expressions.Expression.Property(property, nameof(Nullable<int>.Value));
                predicate = System.Linq.Expressions.Expression.AndAlso(hasValue, BuildContainsExpression(allowedRestaurantIds, value));
            }
            else
            {
                predicate = BuildContainsExpression(allowedRestaurantIds, property);
            }

            return ApplyWhere(query, parameter, predicate);
        }

        private static IQueryable ApplySort(IQueryable query, TableAccessDefinition table, string? sortColumn, string? sortDirection)
        {
            var column = table.Columns.FirstOrDefault(x => string.Equals(x.Property.Name, sortColumn, StringComparison.OrdinalIgnoreCase))
                ?? table.PrimaryKey;
            var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            var methodName = descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);

            var parameter = System.Linq.Expressions.Expression.Parameter(query.ElementType, "entity");
            var body = System.Linq.Expressions.Expression.Property(parameter, column.Property.Name);
            var lambda = System.Linq.Expressions.Expression.Lambda(body, parameter);

            return (IQueryable)typeof(Queryable)
                .GetMethods()
                .Where(method => method.Name == methodName && method.GetParameters().Length == 2)
                .Single()
                .MakeGenericMethod(query.ElementType, column.Property.ClrType)
                .Invoke(null, new object[] { query, lambda })!;
        }

        private static IQueryable ApplySearchQuery(IQueryable query, TableAccessDefinition table, string? search)
        {
            var terms = TokenizedSearch.SplitTerms(search);
            if (terms.Count == 0)
                return query;

            var searchableColumns = table.Columns
                .Where(x => x.Property.ClrType == typeof(string))
                .ToList();
            if (searchableColumns.Count == 0)
                return query;

            var parameter = System.Linq.Expressions.Expression.Parameter(query.ElementType, "entity");
            foreach (var term in terms)
            {
                System.Linq.Expressions.Expression? termPredicate = null;
                foreach (var column in searchableColumns)
                {
                    var property = System.Linq.Expressions.Expression.Property(parameter, column.Property.Name);
                    var notNull = System.Linq.Expressions.Expression.NotEqual(
                        property,
                        System.Linq.Expressions.Expression.Constant(null, typeof(string)));
                    var contains = System.Linq.Expressions.Expression.Call(
                        property,
                        nameof(string.Contains),
                        Type.EmptyTypes,
                        System.Linq.Expressions.Expression.Constant(term));
                    var columnPredicate = System.Linq.Expressions.Expression.AndAlso(notNull, contains);
                    termPredicate = termPredicate is null
                        ? columnPredicate
                        : System.Linq.Expressions.Expression.OrElse(termPredicate, columnPredicate);
                }

                if (termPredicate is not null)
                    query = ApplyWhere(query, parameter, termPredicate);
            }

            return query;
        }

        private static IQueryable ApplyConstantFilter(IQueryable query, bool value)
        {
            var parameter = System.Linq.Expressions.Expression.Parameter(query.ElementType, "entity");
            var body = System.Linq.Expressions.Expression.Constant(value);
            return ApplyWhere(query, parameter, body);
        }

        private static IQueryable ApplyWhere(
            IQueryable query,
            System.Linq.Expressions.ParameterExpression parameter,
            System.Linq.Expressions.Expression predicate)
        {
            var lambda = System.Linq.Expressions.Expression.Lambda(predicate, parameter);
            return (IQueryable)typeof(Queryable)
                .GetMethods()
                .Where(method => method.Name == nameof(Queryable.Where) && method.GetParameters().Length == 2)
                .Single()
                .MakeGenericMethod(query.ElementType)
                .Invoke(null, new object[] { query, lambda })!;
        }

        private static System.Linq.Expressions.Expression BuildContainsExpression(
            IReadOnlyCollection<int> values,
            System.Linq.Expressions.Expression property)
        {
            return System.Linq.Expressions.Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Contains),
                new[] { typeof(int) },
                System.Linq.Expressions.Expression.Constant(values.ToList()),
                property);
        }

        private static Dictionary<string, object?> ToRowDictionary(object entity, TableAccessDefinition table)
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in table.Columns)
            {
                row[column.Property.Name] = GetEntityValue(entity, column.Property);
            }

            return row;
        }

        private static async Task<List<object>> ToListAsync(IQueryable query, CancellationToken ct)
        {
            var method = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .Where(x => x.Name == nameof(EntityFrameworkQueryableExtensions.ToListAsync))
                .Single(x => x.GetParameters().Length == 2)
                .MakeGenericMethod(query.ElementType);

            var task = (Task)method.Invoke(null, new object[] { query, ct })!;
            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task)!;
            return ((System.Collections.IEnumerable)result).Cast<object>().ToList();
        }

        private async Task<object?> FindEntityAsync(TableAccessDefinition table, string key, CancellationToken ct)
        {
            var keyValue = ConvertKeyValue(key, table.PrimaryKey.Property);
            return await _db.FindAsync(table.EntityType.ClrType, [keyValue], ct);
        }

        private async Task EnsureDefaultTranslationsAsync(object entity, CancellationToken ct)
        {
            var cultures = await GetActiveCultureCodesAsync(ct);

            if (entity is Cuisine cuisine)
            {
                var existingCultures = await _db.CuisineTranslations
                    .AsNoTracking()
                    .Where(x => x.CuisineId == cuisine.Id)
                    .Select(x => x.Culture)
                    .ToListAsync(ct);
                var fallbackName = string.IsNullOrWhiteSpace(cuisine.Name) ? $"Cuisine #{cuisine.Id}" : cuisine.Name;

                foreach (var culture in cultures.Where(culture => !existingCultures.Contains(culture, StringComparer.OrdinalIgnoreCase)))
                {
                    _db.CuisineTranslations.Add(new CuisineTranslation
                    {
                        CuisineId = cuisine.Id,
                        Culture = culture,
                        Name = fallbackName
                    });
                }

                await _db.SaveChangesAsync(ct);
                return;
            }

            if (entity is Allergen allergen)
            {
                var existingCultures = await _db.AllergenTranslations
                    .AsNoTracking()
                    .Where(x => x.AllergenId == allergen.Id)
                    .Select(x => x.Culture)
                    .ToListAsync(ct);
                var fallbackName = !string.IsNullOrWhiteSpace(allergen.Name)
                    ? allergen.Name
                    : !string.IsNullOrWhiteSpace(allergen.Code)
                        ? allergen.Code
                        : $"Allergen #{allergen.Id}";

                foreach (var culture in cultures.Where(culture => !existingCultures.Contains(culture, StringComparer.OrdinalIgnoreCase)))
                {
                    _db.AllergenTranslations.Add(new AllergenTranslation
                    {
                        AllergenId = allergen.Id,
                        Culture = culture,
                        Name = fallbackName
                    });
                }

                await _db.SaveChangesAsync(ct);
            }
        }

        private async Task EnsureCanDeleteEntityAsync(TableAccessDefinition table, object entity, CancellationToken ct)
        {
            await EnsureNoCustomUsageAsync(entity, ct);

            foreach (var foreignKey in table.EntityType.GetReferencingForeignKeys())
            {
                if (foreignKey.DeleteBehavior == DeleteBehavior.Cascade)
                    continue;

                if (foreignKey.PrincipalKey.Properties.Any(property => GetEntityValue(entity, property) is null))
                    continue;

                if (await HasDependentRowsAsync(foreignKey, entity, ct))
                {
                    var dependentName = foreignKey.DeclaringEntityType.GetTableName() ?? foreignKey.DeclaringEntityType.ClrType.Name;
                    throw new InvalidOperationException($"Cannot delete {table.TableName} because it is used by {dependentName}.");
                }
            }
        }

        private async Task EnsureNoCustomUsageAsync(object entity, CancellationToken ct)
        {
            if (entity is Cuisine cuisine)
            {
                var assignedCuisineValues = await _db.Restaurants
                    .AsNoTracking()
                    .Where(x => x.CuisineType != null)
                    .Select(x => x.CuisineType!)
                    .ToListAsync(ct);
                var used = assignedCuisineValues.Any(value =>
                    SplitCuisineTypes(value).Contains(cuisine.Name, StringComparer.OrdinalIgnoreCase));
                if (used)
                    throw new InvalidOperationException("Cannot delete cuisine because it is used by at least one restaurant.");
            }

            if (entity is Allergen allergen)
            {
                var used = await _db.Ingredients
                    .AsNoTracking()
                    .AnyAsync(x => x.AllergenCode != null && x.AllergenCode == allergen.Code, ct);
                if (used)
                    throw new InvalidOperationException("Cannot delete allergen because it is used by at least one ingredient.");
            }
        }

        private async Task<bool> HasDependentRowsAsync(IForeignKey foreignKey, object principalEntity, CancellationToken ct)
        {
            var query = CreateEntityQuery(foreignKey.DeclaringEntityType.ClrType);
            var parameter = System.Linq.Expressions.Expression.Parameter(query.ElementType, "entity");
            System.Linq.Expressions.Expression? predicate = null;

            for (var i = 0; i < foreignKey.Properties.Count; i++)
            {
                var dependentProperty = foreignKey.Properties[i];
                var principalProperty = foreignKey.PrincipalKey.Properties[i];
                var principalValue = GetEntityValue(principalEntity, principalProperty);
                var dependentValue = ConvertReferenceValue(principalValue, dependentProperty.ClrType);
                var dependentAccess = BuildEfPropertyExpression(parameter, dependentProperty);
                var equals = System.Linq.Expressions.Expression.Equal(
                    dependentAccess,
                    System.Linq.Expressions.Expression.Constant(dependentValue, dependentProperty.ClrType));

                predicate = predicate is null
                    ? equals
                    : System.Linq.Expressions.Expression.AndAlso(predicate, equals);
            }

            if (predicate is null)
                return false;

            var lambda = System.Linq.Expressions.Expression.Lambda(predicate, parameter);
            var method = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .Where(x => x.Name == nameof(EntityFrameworkQueryableExtensions.AnyAsync))
                .Single(x => x.GetParameters().Length == 3)
                .MakeGenericMethod(query.ElementType);

            var task = (Task)method.Invoke(null, new object[] { query, lambda, ct })!;
            await task.ConfigureAwait(false);
            return (bool)task.GetType().GetProperty(nameof(Task<bool>.Result))!.GetValue(task)!;
        }

        private static System.Linq.Expressions.Expression BuildEfPropertyExpression(
            System.Linq.Expressions.Expression parameter,
            IProperty property)
        {
            return System.Linq.Expressions.Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                new[] { property.ClrType },
                parameter,
                System.Linq.Expressions.Expression.Constant(property.Name));
        }

        private static object? ConvertReferenceValue(object? value, Type targetType)
        {
            if (value is null)
                return null;

            var effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            var converted = value.GetType() == effectiveType
                ? value
                : Convert.ChangeType(value, effectiveType, CultureInfo.InvariantCulture);

            return Nullable.GetUnderlyingType(targetType) is null
                ? converted
                : Activator.CreateInstance(targetType, converted);
        }

        private async Task<List<string>> GetActiveCultureCodesAsync(CancellationToken ct)
        {
            var cultures = await _db.AppLanguages
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .Select(x => x.Culture)
                .ToListAsync(ct);

            return cultures.Count > 0 ? cultures : new List<string> { "pl-PL" };
        }

        private async Task EnsureEntityScopeAsync(TableAccessDefinition table, object entity, CancellationToken ct)
        {
            if (User.IsInRole("Admin") || !table.IsRestaurantScoped || table.RestaurantIdColumn is null)
                return;

            var value = GetEntityValue(entity, table.RestaurantIdColumn.Property);
            if (value is null)
                throw new InvalidOperationException("RestaurantId is required for this table.");

            var restaurantId = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            var allowed = await GetAllowedRestaurantIdsAsync(ct);
            if (!allowed.Contains(restaurantId))
                throw new InvalidOperationException("You are not allowed to modify rows for this restaurant.");
        }

        private async Task<List<string>> GetManageableRoleNamesAsync(CancellationToken ct)
        {
            return await _db.Roles
                .AsNoTracking()
                .Select(x => x.Name!)
                .Where(x => x != null && x != "Admin")
                .OrderBy(x => x)
                .ToListAsync(ct);
        }

        private async Task<string> RequireManageableRoleNameAsync(string roleName, CancellationToken ct)
        {
            var normalized = roleName.Trim();
            var role = await _db.Roles
                .AsNoTracking()
                .Where(x => x.Name != null && x.Name != "Admin")
                .Select(x => x.Name!)
                .FirstOrDefaultAsync(x => x == normalized, ct);

            if (string.IsNullOrWhiteSpace(role))
                throw new InvalidOperationException("Role not found or cannot be managed here.");

            return role;
        }

        private static object? GetEntityValue(object entity, IProperty property)
        {
            var propertyInfo = property.PropertyInfo
                ?? throw new InvalidOperationException($"Property {property.Name} is not mapped to a CLR property.");
            return propertyInfo.GetValue(entity);
        }

        private static void SetEntityValue(object entity, IProperty property, object? value)
        {
            var propertyInfo = property.PropertyInfo
                ?? throw new InvalidOperationException($"Property {property.Name} is not mapped to a CLR property.");
            propertyInfo.SetValue(entity, value);
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
                .Where(x => x.UserId == userId)
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

        private static List<string> SplitCuisineTypes(string? value)
            => string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

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

            var principalPrimaryKey = foreignKey.PrincipalKey.Properties.Single();
            var labelProperty = GetPreferredLabelProperty(principalType);
            var restaurantIdProperty = principalType.GetProperties()
                .FirstOrDefault(x => string.Equals(x.Name, "RestaurantId", StringComparison.OrdinalIgnoreCase) && (x.ClrType == typeof(int) || x.ClrType == typeof(int?)));

            var query = CreateEntityQuery(principalType.ClrType);
            var rows = await ToListAsync(query, ct);

            if (!User.IsInRole("Admin") && restaurantIdProperty is not null)
            {
                var allowedRestaurantIds = await GetAllowedRestaurantIdsAsync(ct);
                if (allowedRestaurantIds.Count == 0)
                    return new List<AdminDataOptionDto>();

                rows = rows
                    .Where(row =>
                    {
                        var value = GetEntityValue(row, restaurantIdProperty);
                        return value is not null && allowedRestaurantIds.Contains(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                    })
                    .ToList();
            }

            return rows
                .OrderBy(row => Convert.ToString(labelProperty is null ? GetEntityValue(row, principalPrimaryKey) : GetEntityValue(row, labelProperty), CultureInfo.InvariantCulture))
                .Take(100)
                .Select(row =>
                {
                    var value = GetEntityValue(row, principalPrimaryKey);
                    var label = labelProperty is null ? value : GetEntityValue(row, labelProperty);
                    return new AdminDataOptionDto
                    {
                        Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
                        Label = string.IsNullOrWhiteSpace(Convert.ToString(label, CultureInfo.InvariantCulture))
                            ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
                            : Convert.ToString(label, CultureInfo.InvariantCulture) ?? ""
                    };
                })
                .ToList();
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
