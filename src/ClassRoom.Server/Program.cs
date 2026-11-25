using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

var app = builder.Build();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

var devices = new ConcurrentDictionary<string, StudentDevice>();
var tasks = new ConcurrentDictionary<string, ClassroomTask>();
var submissions = new ConcurrentDictionary<string, Submission>();
var teacherKey = builder.Configuration["Teacher:ApiKey"] ?? "change-this-teacher-key";
var offlineAfter = builder.Configuration.GetValue("Monitoring:OfflineAfterSeconds", 30);

bool IsTeacher(HttpRequest request) =>
    request.Headers.TryGetValue("X-Teacher-Key", out var supplied) &&
    string.Equals(supplied.ToString(), teacherKey, StringComparison.Ordinal);

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", app = "ClassRoom Control", version = "1.0" }));

app.MapPost("/api/student/heartbeat", (HeartbeatRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.DeviceId) || request.DeviceId.Length > 80)
        return Results.BadRequest(new { error = "DeviceId is required (max 80 chars)." });

    var now = DateTimeOffset.UtcNow;
    devices.AddOrUpdate(request.DeviceId,
        _ => new StudentDevice(request.DeviceId, Clean(request.DisplayName, 80), now),
        (_, old) => old with { DisplayName = Clean(request.DisplayName, 80), LastSeenUtc = now });
    var currentTask = tasks.Values.Where(t => t.Published).OrderByDescending(t => t.CreatedUtc).FirstOrDefault();
    return Results.Ok(new { serverTimeUtc = now, currentTask });
});

app.MapGet("/api/student/task", () =>
{
    var task = tasks.Values.Where(t => t.Published).OrderByDescending(t => t.CreatedUtc).FirstOrDefault();
    return Results.Ok(task);
});

app.MapPost("/api/student/submission", (SubmissionRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.DeviceId) || string.IsNullOrWhiteSpace(request.TaskId))
        return Results.BadRequest(new { error = "DeviceId and TaskId are required." });
    if (!tasks.ContainsKey(request.TaskId))
        return Results.NotFound(new { error = "Task not found." });

    var id = Guid.NewGuid().ToString("N");
    var item = new Submission(id, request.TaskId, request.DeviceId,
        Clean(request.Content, 10000), DateTimeOffset.UtcNow);
    submissions[id] = item;
    return Results.Ok(item);
});

app.MapGet("/api/teacher/overview", (HttpRequest request) =>
{
    if (!IsTeacher(request)) return Results.Unauthorized();
    var now = DateTimeOffset.UtcNow;
    var allDevices = devices.Values.OrderBy(d => d.DeviceId).Select(d => new
    {
        d.DeviceId,
        d.DisplayName,
        d.LastSeenUtc,
        Online = (now - d.LastSeenUtc).TotalSeconds <= offlineAfter,
        LastSeenSecondsAgo = Math.Max(0, (int)(now - d.LastSeenUtc).TotalSeconds)
    });
    var allTasks = tasks.Values.OrderByDescending(t => t.CreatedUtc);
    var allSubmissions = submissions.Values.OrderByDescending(s => s.SubmittedUtc);
    return Results.Ok(new { devices = allDevices, tasks = allTasks, submissions = allSubmissions });
});

app.MapPost("/api/teacher/tasks", (HttpRequest request, CreateTaskRequest body) =>
{
    if (!IsTeacher(request)) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(body.Title) || string.IsNullOrWhiteSpace(body.Instructions))
        return Results.BadRequest(new { error = "Title and instructions are required." });

    var id = Guid.NewGuid().ToString("N");
    var task = new ClassroomTask(id, Clean(body.Title, 120), Clean(body.Instructions, 5000),
        DateTimeOffset.UtcNow, body.Published);
    tasks[id] = task;
    return Results.Ok(task);
});

app.MapPost("/api/teacher/tasks/{id}/publish", (HttpRequest request, string id) =>
{
    if (!IsTeacher(request)) return Results.Unauthorized();
    if (!tasks.TryGetValue(id, out var task)) return Results.NotFound();
    var updated = task with { Published = true };
    tasks[id] = updated;
    return Results.Ok(updated);
});

app.Run();

static string Clean(string? value, int max) =>
    new string((value ?? "").Where(c => !char.IsControl(c) || c == '\n' || c == '\r').Take(max).ToArray()).Trim();

record HeartbeatRequest(string DeviceId, string? DisplayName);
record CreateTaskRequest(string Title, string Instructions, bool Published = true);
record SubmissionRequest(string DeviceId, string TaskId, string Content);
record StudentDevice(string DeviceId, string DisplayName, DateTimeOffset LastSeenUtc);
record ClassroomTask(string Id, string Title, string Instructions, DateTimeOffset CreatedUtc, bool Published);
record Submission(string Id, string TaskId, string DeviceId, string Content, DateTimeOffset SubmittedUtc);
