namespace DragChain.API.Sensor.Security;

public record RbacPermissionItem(
    string Code,
    string Name,
    string Group,
    string Kind,
    string? RoutePath,
    string? ApiPattern,
    string[] Methods);

public static class RbacPermissionCatalog
{
    public const string EmptyPermissionMarker = "__none__";

    public static readonly RbacPermissionItem[] Items =
    [
        new("menu:sensor:selector", "菜单-感应器选型", "菜单权限", "menu", "/sensor/selector", null, []),
        new("menu:sensor:products", "菜单-感应器产品管理", "菜单权限", "menu", "/sensor/products", null, []),
        new("menu:rbac", "菜单-系统管理", "菜单权限", "menu", "/system/users", null, []),

        new("page:sensor:selector", "页面-感应器选型", "页面权限", "page", "/sensor/selector", null, []),
        new("page:sensor:products", "页面-感应器产品管理", "页面权限", "page", "/sensor/products", null, []),
        new("page:rbac:users", "页面-用户管理", "页面权限", "page", "/system/users", null, []),
        new("page:rbac:roles", "页面-角色权限", "页面权限", "page", "/system/roles", null, []),
        new("page:trunking:catalog", "页面-线槽型录", "页面权限", "page", "/trunking/catalog", null, []),
        new("page:chain:catalog", "页面-拖链型录", "页面权限", "page", "/chain/wzl", null, []),
        new("page:pipe:library", "页面-管线库", "页面权限", "page", "/pipe-library", null, []),

        new("api:selector:read", "接口-感应器选型读取", "接口权限", "api", null, "/api/selector", ["GET"]),
        new("api:selector:write", "接口-感应器选型维护", "接口权限", "api", null, "/api/selector", ["POST", "PUT", "DELETE"]),
        new("api:products:read", "接口-感应器产品读取", "接口权限", "api", null, "/api/products", ["GET"]),
        new("api:products:write", "接口-感应器产品维护", "接口权限", "api", null, "/api/products", ["POST", "PUT", "DELETE"]),
        new("api:taxonomy:read", "接口-感应器配置项读取", "接口权限", "api", null, "/api/taxonomy", ["GET"]),
        new("api:taxonomy:write", "接口-感应器配置项维护", "接口权限", "api", null, "/api/taxonomy", ["POST", "PUT", "DELETE"]),
        new("api:trunking:read", "接口-线槽选型使用", "接口权限", "api", null, "/api/trunking", ["GET", "POST", "PUT", "DELETE"]),
        new("api:trunking:write", "接口-线槽维护", "接口权限", "api", null, "/api/trunking", ["POST", "PUT", "DELETE"]),
        new("api:chain:read", "接口-拖链读取/计算", "接口权限", "api", null, "/api/chain", ["GET", "POST"]),
        new("api:chain:write", "接口-拖链型录维护", "接口权限", "api", null, "/api/chain", ["POST", "PUT", "DELETE"]),
        new("api:pipe:read", "接口-管线库读取", "接口权限", "api", null, "/api/pipe", ["GET"]),
        new("api:pipe:write", "接口-管线库维护", "接口权限", "api", null, "/api/pipe", ["POST", "PUT", "DELETE"]),
        new("api:rbac:read", "接口-权限读取", "接口权限", "api", null, "/api/rbac", ["GET"]),
        new("api:rbac:write", "接口-权限维护", "接口权限", "api", null, "/api/rbac", ["POST", "PUT", "DELETE"])
    ];

    public static bool IsKnownRole(string? role) => role is
        "super_admin" or "admin" or "editor" or "user";

    public static string[] DefaultPermissions(string role) => role switch
    {
        "super_admin" => Items.Select(item => item.Code).ToArray(),
        "admin" => Items.Select(item => item.Code).ToArray(),
        "editor" => Items
            .Where(item => item.Code is not "menu:rbac" and not "page:rbac:users" and not "page:rbac:roles" and not "api:rbac:write")
            .Select(item => item.Code)
            .ToArray(),
        "user" =>
        [
            "menu:sensor:selector",
            "page:sensor:selector",
            "api:selector:read",
            "api:products:read",
            "api:taxonomy:read",
            "api:trunking:read",
            "api:chain:read",
            "api:pipe:read"
        ],
        _ => []
    };

    public static bool IsPublicRequest(string method, string path)
    {
        var normalizedMethod = method.ToUpperInvariant();
        var normalizedPath = path.Split('?', 2)[0].ToLowerInvariant().TrimEnd('/');

        if (normalizedMethod == "OPTIONS") return true;
        if (normalizedMethod == "GET" && normalizedPath == "/api/health") return true;
        if (normalizedMethod == "POST" && normalizedPath == "/api/auth/login") return true;
        if (normalizedMethod == "POST" && normalizedPath == "/api/auth/refresh-token") return true;

        return false;
    }

    public static bool IsPublicRequest(HttpRequest request) =>
        IsPublicRequest(request.Method, request.Path.Value ?? "");

    public static string? MatchApiPermission(HttpRequest request)
    {
        var path = request.Path.Value?.ToLowerInvariant() ?? "";
        var method = request.Method.ToUpperInvariant();

        if (path.StartsWith("/api/rbac"))
            return method == "GET" ? "api:rbac:read" : "api:rbac:write";

        if (path.StartsWith("/api/products"))
            return method == "GET" ? "api:products:read" : "api:products:write";

        if (path.StartsWith("/api/process-notes"))
            return method == "GET" ? "api:products:read" : "api:products:write";

        if (path.StartsWith("/api/scenarios")
            || path.StartsWith("/api/sensor-types")
            || path.StartsWith("/api/process-scenarios")
            || path.StartsWith("/api/scenario-functions")
            || path.StartsWith("/api/function-conditions"))
            return method == "GET" ? "api:taxonomy:read" : "api:taxonomy:write";

        if (path.StartsWith("/api/selection-entries") || path.StartsWith("/api/selection-tree"))
            return method == "GET" ? "api:selector:read" : "api:selector:write";

        if (path.StartsWith("/api/trunking/calc")
            || path.StartsWith("/api/trunking/saved-selection")
            || path.StartsWith("/api/trunking/saved-selections"))
            return "api:trunking:read";

        if (path.StartsWith("/api/trunking"))
            return method == "GET" ? "api:trunking:read" : "api:trunking:write";

        if (path.StartsWith("/api/calculation/calc"))
            return "api:chain:read";

        if (path.StartsWith("/api/wzl") || path.StartsWith("/api/me"))
            return method == "GET" ? "api:chain:read" : "api:chain:write";

        if (path.StartsWith("/api/pipelibrary")
            || path.StartsWith("/api/pipemodules")
            || path.StartsWith("/api/pipecomponents"))
            return method == "GET" ? "api:pipe:read" : "api:pipe:write";

        return null;
    }
}
