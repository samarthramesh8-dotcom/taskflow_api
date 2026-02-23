using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace TaskAPI.Controller;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly string _cs;

    public TasksController(IConfiguration config)
    {
        _cs = config.GetConnectionString("Default")
              ?? throw new InvalidOperationException("Missing connection string");
    }

    public record CreateTaskRequest(
        string Text,
        string? Description,
        string? Category,
        string? Priority,
        DateTimeOffset? DueAt,
        DateTimeOffset? DueDate,
        Guid? UserId
    );

    public record TaskDto(
        Guid Id,
        string Text,
        bool Done,
        DateTimeOffset? DueDate,
        string Priority,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        Guid? UserId,
        string? Description,
        string? Category,
        DateTimeOffset? DueAt
    );

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Text))
            return BadRequest(new { error = "text is required" });

        var sql = @"
INSERT INTO tasks
(text, description, category, priority, due_at, due_date, user_id, updated_at)
VALUES
(@Text, @Description, COALESCE(@Category,'general'),
 COALESCE(@Priority,'medium'), @DueAt, @DueDate, @UserId, now())
RETURNING
 id, text, done, due_date, priority,
 created_at, updated_at, user_id, description, category, due_at;
";

        await using var conn = new NpgsqlConnection(_cs);

        var task = await conn.QuerySingleAsync<TaskDto>(sql, new
        {
            Text = req.Text.Trim(),
            Description = req.Description,
            Category = req.Category,
            Priority = req.Priority,
            DueAt = req.DueAt,
            DueDate = req.DueDate,
            UserId = req.UserId
        });

        return Created($"/api/tasks/{task.Id}", task);
    }
}