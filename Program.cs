using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

// Top-level statements first
var builder = WebApplication.CreateBuilder(args);

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Employee API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid JWT token.\n\nUse /login with username: admin, password: password to get a token."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {

                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Add JWT authentication
var jwtKey = "super_secret_jwt_key_12345_very_long_key!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI();

// Enable authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Error handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ApiException ex)
    {
        context.Response.StatusCode = ex.StatusCode;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
        
        // Log the actual exception details
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An unhandled exception occurred");
    }
});

// Simple in-memory store for employees
var employees = new ConcurrentDictionary<int, Employee>(new[]
{
    new KeyValuePair<int, Employee>(1, new Employee { Id = 1, Name = "Alice Smith", Position = "Developer" }),
    new KeyValuePair<int, Employee>(2, new Employee { Id = 2, Name = "Bob Johnson", Position = "Manager" })
});
var nextId = employees.Keys.Max() + 1;

// Enhanced logging middleware: log method, path, and response status code
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
    Console.WriteLine($"Response: {context.Response.StatusCode}");
});

// GET all employees
app.MapGet("/employees", () => employees.Values);

// GET employee by id
app.MapGet("/employees/{id:int}", (int id) =>
{
    if (!employees.TryGetValue(id, out var emp))
    {
        throw new ApiException(StatusCodes.Status404NotFound, $"Employee with ID {id} not found.");
    }
    return Results.Ok(emp);
});

// POST create new employee
app.MapPost("/employees", (EmployeeCreateDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Position))
    {
        throw new ApiException(StatusCodes.Status400BadRequest, "Name and Position are required.");
    }
    var id = nextId++;
    var employee = new Employee { Id = id, Name = dto.Name, Position = dto.Position };
    employees[id] = employee;
    return Results.Created($"/employees/{id}", employee);
});

// PUT update employee
app.MapPut("/employees/{id:int}", (int id, EmployeeUpdateDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Position))
    {
        throw new ApiException(StatusCodes.Status400BadRequest, "Name and Position are required.");
    }
    if (!employees.ContainsKey(id))
    {
        throw new ApiException(StatusCodes.Status404NotFound, $"Employee with ID {id} not found.");
    }
    var updated = new Employee { Id = id, Name = dto.Name, Position = dto.Position };
    employees[id] = updated;
    return Results.Ok(updated);
}).RequireAuthorization();

// DELETE employee
app.MapDelete("/employees/{id:int}", (int id) =>
{
    if (!employees.TryRemove(id, out _))
    {
        throw new ApiException(StatusCodes.Status404NotFound, $"Employee with ID {id} not found.");
    }
    return Results.NoContent();
}).RequireAuthorization();

// Login endpoint for JWT token generation
app.MapPost("/login", (LoginRequest login) =>
{
    // Hardcoded username and password for demo
    if (login.Username == "admin" && login.Password == "password")
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, login.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Results.Ok(new { token = tokenString });
    }
    return Results.Unauthorized();
});

app.MapGet("/", context => {
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.Run();

public class ApiException : Exception
{
    public int StatusCode { get; }
    public string Message { get; }

    public ApiException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
        Message = message;
    }
}

record Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
}

record EmployeeCreateDto(string Name, string Position);
record EmployeeUpdateDto(string Name, string Position);
record LoginRequest(string Username, string Password); 