using DragChain.API.Sensor.Controllers;

using DragChain.API.Sensor.Data;
using Microsoft.EntityFrameworkCore;

namespace DragChain.API.Sensor.Security;

public class RbacMiddleware
{
    private readonly RequestDelegate _next;

    public RbacMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, SensorDbContext db)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        if (RbacPermissionCatalog.IsPublicRequest(context.Request))
        {
            await _next(context);
            return;
        }

        var session = await ResolveSessionAsync(context, db);
        if (session == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "请先登录" });
            return;
        }

        if (!IsAllowed(context.Request, session))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "当前账号无权限访问" });
            return;
        }

        await _next(context);
    }

    private static async Task<RbacSession?> ResolveSessionAsync(HttpContext context, SensorDbContext db)
    {
        var auth = context.Request.Headers.Authorization.ToString();
        const string bearer = "Bearer ";
        if (!auth.StartsWith(bearer, StringComparison.OrdinalIgnoreCase)) return null;

        var token = auth[bearer.Length..].Trim();
        if (!AuthController.TryGetSession(token, out var session)) return null;

        var user = await db.RbacUsers.AsNoTracking().FirstOrDefaultAsync(item => item.Id == session.UserId);
        if (user == null || !user.Enabled ||
            !string.Equals(user.SecurityStamp, session.SecurityStamp, StringComparison.Ordinal))
        {
            return null;
        }

        var permissions = await AuthController.GetRolePermissionsAsync(db, user.Role);
        var refreshedSession = new RbacSession(
            user.Id,
            user.EmployeeNo,
            user.Name,
            user.Role,
            permissions,
            user.SecurityStamp,
            session.Expires);
        context.Items[AuthController.SessionItemKey] = refreshedSession;
        return refreshedSession;
    }

    private static bool IsAllowed(HttpRequest request, RbacSession session)
    {
        if (session.Role == "super_admin") return true;

        var required = RbacPermissionCatalog.MatchApiPermission(request);
        if (required == null) return false;

        return AuthController.GetSessionPermissions(session).Contains(required);
    }
}
