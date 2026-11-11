namespace BuildingBlocks.Shared.Constants;

public static class Permissions
{
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";
    public const string RolesView = "roles.view";
    public const string RolesManage = "roles.manage";
    public const string NotificationsView = "notifications.view";
    public const string NotificationsManage = "notifications.manage";
    public const string AuditLogsView = "auditlogs.view";

    public static IReadOnlyList<string> All { get; } =
    [
        UsersView, UsersManage,
        RolesView, RolesManage,
        NotificationsView, NotificationsManage,
        AuditLogsView
    ];
}