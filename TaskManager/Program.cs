using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using TaskManager.Data;
using TaskManager.Models;

var builder = WebApplication.CreateBuilder(args);
var conn = builder.Configuration.GetConnectionString("Default");
Console.WriteLine($"[DEV CHECK] CS starts with: {conn[..Math.Min(conn.Length, 40)]}...");

// OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();    // <-- нужно для Minimal API
builder.Services.AddSwaggerGen();

// (опционально; даёт / openapi / v1.json от нового генератора)
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


//GET /tasks
app.MapGet("/tasks", async (AppDbContext db) =>
    await db.Tasks.OrderByDescending(t => t.Id).ToListAsync());

string[] allowed = ["todo", "in_progress", "done"];

// GET /tasks?skip=0&take=20
app.MapGet("/tasks/paged", async (int skip , int take , AppDbContext db, CancellationToken ct) =>
{
    var list = await db.Tasks
    .OrderByDescending(t => t.Id)
    .Skip(skip)
    .Take(take)
    .ToListAsync(ct);

    return Results.Ok(list);
});

//POST /tasks
app.MapPost("/tasks", async (TaskItem input, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.Title))
        return Results.BadRequest(new { error = "Title is required" });

    if (!string.IsNullOrWhiteSpace(input.Status) && !allowed.Contains(input.Status))
        return Results.BadRequest(new { error = "Invalid status" });

    // Значения по умолчанию
    input.Title = input.Title.Trim();
    if (string.IsNullOrWhiteSpace(input.Status))
        input.Status = "todo";
    if (input.CreatedAt == default)
        input.CreatedAt = DateTimeOffset.UtcNow;

    db.Tasks.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/tasks/{input.Id}", input);
});

//GET /tasks/{id}
app.MapGet("/tasks/{id:int}", async(int id, AppDbContext db, CancellationToken ct) =>
{
    var t = await db.Tasks.FindAsync([id],ct);
    return t is null ? Results.NotFound() : Results.Ok(t);
});

//PUT /tasks/{id}
app.MapPut("/tasks/{id:int}", async (int id, TaskItem input, AppDbContext db, CancellationToken ct) =>
{
    var t = await db.Tasks.FindAsync([id], ct);
    if (t is null) return Results.NotFound();

    if (!string.IsNullOrWhiteSpace(input.Title))
        t.Title = input.Title.Trim();

    if (!string.IsNullOrWhiteSpace(input.Status))
        t.Status = input.Status.Trim();

    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

//DELETE /tasks/{id}
app.MapDelete("/tasks/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
{
    var t = await db.Tasks.FindAsync([id], ct);
    if(t is null) return Results.NotFound();
    
    db.Tasks.Remove(t);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

/*app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");*/

app.MapGet("/health", () => Results.Ok(new { status = "ok", ts = DateTimeOffset.UtcNow }))
   .WithName("Health");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

