using System.Text.Json;
using geotagger_backend.Data;
using geotagger_backend.Models;

namespace geotagger_backend.Middleware
{
    /// <summary>
    /// Exposes /api/log/client‑action endpoint that the frontend calls with batches of user actions.
    /// This keeps DB writes out of Blazor/React event loops.
    /// </summary>
    public class ActionLoggingMiddleware : IMiddleware
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ActionLoggingMiddleware> _logger;
        public ActionLoggingMiddleware(ApplicationDbContext db, ILogger<ActionLoggingMiddleware> logger)
        {
            _db = db; _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Path.Equals("/api/log/client-action", StringComparison.OrdinalIgnoreCase)
                && context.Request.Method == "POST")
            {
                var actions = await JsonSerializer.DeserializeAsync<List<GeoUserActionLog>>(context.Request.Body);
                if (actions == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid payload");
                    return;
                }

                // attach authenticated userId if not set
                var uid = context.User.FindFirst("id")?.Value;
                if (uid != null)
                    actions.ForEach(a => a.UserId = uid);

                await _db.GeoUserActionLogs.AddRangeAsync(actions);
                await _db.SaveChangesAsync();
                context.Response.StatusCode = 204;
                return;
            }
            await next(context);
        }
    }
}