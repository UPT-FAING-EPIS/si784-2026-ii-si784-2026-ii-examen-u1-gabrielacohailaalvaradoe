using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UniversitySocial.Api.Data;
using UniversitySocial.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default must be configured.");
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must be configured with at least 32 bytes.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
    {
        Status = StatusCodes.Status400BadRequest, Title = "Validation failed", Type = "https://httpstatuses.com/400"
    });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "UniversitySocial",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "UniversitySocial.Client",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins((builder.Configuration["Cors:Origins"] ?? "http://localhost:5173").Split(',', StringSplitOptions.RemoveEmptyEntries))
        .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "University Social API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

var app = builder.Build();
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var status = exception is UnauthorizedAccessException ? StatusCodes.Status401Unauthorized : StatusCodes.Status500InternalServerError;
    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = status, Title = status == 401 ? "Unauthorized" : "Unexpected server error",
        Detail = app.Environment.IsDevelopment() ? exception?.Message : null
    });
}));
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

if (app.Configuration.GetValue<bool>("ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DevelopmentSeeder.SeedAsync(db, app.Configuration);
}

await app.RunAsync();

public partial class Program;
