namespace API.Authorization;

public static class AppFeatures
{
    public const string OrdersView = "orders.view";
    public const string OrdersWaiterStatus = "orders.status.waiter";
    public const string OrdersChefStatus = "orders.status.chef";
    public const string OrdersDeliveryStatus = "orders.status.delivery";
    public const string OrdersPayments = "orders.payments";
    public const string OrdersDeliveryAssignments = "orders.deliveryAssignments";
    public const string OrdersDocuments = "orders.documents";
    public const string MenuView = "menu.view";
    public const string MenuManage = "menu.manage";
    public const string RestaurantsManage = "restaurants.manage";
    public const string ReservationsManage = "reservations.manage";
    public const string ReportsView = "reports.view";
    public const string PrintTemplatesManage = "printTemplates.manage";
    public const string UsersManage = "users.manage";
    public const string PlatformManage = "platform.manage";
    public const string MaintenanceManage = "maintenance.manage";
    public const string DataCrud = "dataCrud.manage";
    public const string PermissionGroupsManage = "permissionGroups.manage";

    public static IReadOnlyList<AppFeatureDefinition> All { get; } =
    [
        new(OrdersView, "Orders", "View active, historical, and detailed orders.", "Orders", ["Admin", "RestaurantAdmin", "Waiter", "Chef", "DeliveryDriver"]),
        new(OrdersWaiterStatus, "Waiter order statuses", "Move table and pickup orders through waiter statuses.", "Orders", ["Admin", "RestaurantAdmin", "Waiter"]),
        new(OrdersChefStatus, "Kitchen order statuses", "Move orders through kitchen preparation statuses.", "Orders", ["Admin", "RestaurantAdmin", "Chef"]),
        new(OrdersDeliveryStatus, "Delivery order statuses", "Move delivery orders through courier statuses.", "Orders", ["Admin", "RestaurantAdmin", "DeliveryDriver"]),
        new(OrdersPayments, "Order payments", "Mark orders as paid and complete cash collection.", "Orders", ["Admin", "RestaurantAdmin", "Waiter"]),
        new(OrdersDeliveryAssignments, "Delivery assignments", "Assign or unassign delivery drivers.", "Orders", ["Admin", "RestaurantAdmin"]),
        new(OrdersDocuments, "Order documents", "Issue invoices, send receipts, and download order PDFs.", "Orders", ["Admin", "RestaurantAdmin", "Waiter"]),
        new(MenuView, "Menu view", "View restaurant menu management data.", "Restaurant management", ["Admin", "RestaurantAdmin"]),
        new(MenuManage, "Menu management", "Create, edit, delete, price, and translate menu items and categories.", "Restaurant management", ["Admin", "RestaurantAdmin"]),
        new(RestaurantsManage, "Restaurants and settings", "Manage restaurants, tables, settings, staff assignments, and invites.", "Restaurant management", ["Admin", "RestaurantAdmin"]),
        new(ReservationsManage, "Reservations", "Manage reservation schedules, availability, calendar, and statuses.", "Restaurant management", ["Admin", "RestaurantAdmin", "Waiter"]),
        new(ReportsView, "Reports", "View and export business reports.", "Administration", ["Admin", "RestaurantAdmin"]),
        new(PrintTemplatesManage, "Print templates", "Edit printable and email document templates.", "Administration", ["Admin"]),
        new(UsersManage, "Users", "Manage users, roles, passwords, and restaurant assignments.", "Administration", ["Admin"]),
        new(PlatformManage, "Platform settings", "Manage languages, localization files, and global order/payment options.", "Administration", ["Admin"]),
        new(MaintenanceManage, "Maintenance", "Run maintenance operations.", "Administration", ["Admin"]),
        new(DataCrud, "Data tables", "Use the generic data table CRUD panel.", "Administration", ["Admin"]),
        new(PermissionGroupsManage, "Permission groups", "Manage CRUD and application permission matrices.", "Administration", ["Admin"])
    ];

    public static AppFeatureDefinition? Find(string featureKey)
        => All.FirstOrDefault(x => string.Equals(x.FeatureKey, featureKey, StringComparison.OrdinalIgnoreCase));
}

public sealed record AppFeatureDefinition(
    string FeatureKey,
    string Name,
    string Description,
    string GroupName,
    IReadOnlyList<string> DefaultRoles);
