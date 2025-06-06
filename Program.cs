using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI();

// Simple in-memory store for employees
var employees = new ConcurrentDictionary<int, Employee>(new[]
{
    new KeyValuePair<int, Employee>(1, new Employee { Id = 1, Name = "Alice Smith", Position = "Developer" }),
    new KeyValuePair<int, Employee>(2, new Employee { Id = 2, Name = "Bob Johnson", Position = "Manager" })
});
var nextId = employees.Keys.Max() + 1;

// Middleware for logging requests
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
});

// GET all employees
app.MapGet("/employees", () => employees.Values);

// GET employee by id
app.MapGet("/employees/{id:int}", (int id) =>
    employees.TryGetValue(id, out var emp) ? Results.Ok(emp) : Results.NotFound());

// POST create new employee
app.MapPost("/employees", (Employee employee) =>
{
    var id = nextId++;
    employee.Id = id;
    employees[id] = employee;
    return Results.Created($"/employees/{id}", employee);
});

// PUT update employee
app.MapPut("/employees/{id:int}", (int id, Employee updated) =>
{
    if (!employees.ContainsKey(id)) return Results.NotFound();
    updated.Id = id;
    employees[id] = updated;
    return Results.Ok(updated);
});

// DELETE employee
app.MapDelete("/employees/{id:int}", (int id) =>
{
    return employees.TryRemove(id, out _) ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/", context => {
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.Run();

record Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
} 