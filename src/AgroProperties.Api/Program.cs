using System.Security.Claims;
using System.Text;
using AgroProperties.Api.Data;
using AgroProperties.Api.Domain;
using AgroProperties.Api.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// JWT configs (fail fast)
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Config Jwt:Issuer n�o encontrada.");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Config Jwt:Audience n�o encontrada.");
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Config Jwt:Key n�o encontrada.");

// DB (Azure SQL) - schema properties + migrations isoladas
builder.Services.AddDbContext<PropertiesDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("ConnectionStrings:Default n�o encontrada."),
        sql => sql
            .MigrationsHistoryTable("__EFMigrationsHistory", "properties")
            .EnableRetryOnFailure(10, TimeSpan.FromSeconds(10), null)
    )
);

// Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// Swagger Authorize
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "AgroProperties API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header usando Bearer. Ex: \"Bearer {token}\""
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

static Guid ProducerId(ClaimsPrincipal user)
{
    var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (idStr is null || !Guid.TryParse(idStr, out var id))
        throw new UnauthorizedAccessException("JWT inv�lido (NameIdentifier).");
    return id;
}

// POST /properties
app.MapPost("/properties", [Authorize] async (ClaimsPrincipal user, CreatePropertyRequest req, PropertiesDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(req.Name))
        return Results.BadRequest(new { message = "Name � obrigat�rio." });

    var entity = new FarmProperty
    {
        ProducerId = ProducerId(user),
        Name = req.Name.Trim(),
        Location = string.IsNullOrWhiteSpace(req.Location) ? null : req.Location.Trim()
    };

    db.Properties.Add(entity);
    await db.SaveChangesAsync();

    return Results.Created($"/properties/{entity.Id}", new PropertyResponse(entity.Id, entity.Name, entity.Location));
});

// GET /properties
app.MapGet("/properties", [Authorize] async (ClaimsPrincipal user, PropertiesDbContext db) =>
{
    var producerId = ProducerId(user);

    var list = await db.Properties
        .Where(p => p.ProducerId == producerId)
        .OrderBy(p => p.Name)
        .Select(p => new PropertyResponse(p.Id, p.Name, p.Location))
        .ToListAsync();

    return Results.Ok(list);
});

// POST /properties/{propertyId}/plots
app.MapPost("/properties/{propertyId:guid}/plots", [Authorize] async (
    ClaimsPrincipal user,
    Guid propertyId,
    CreatePlotRequest req,
    PropertiesDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Crop))
        return Results.BadRequest(new { message = "Name e Crop s�o obrigat�rios." });

    var producerId = ProducerId(user);

    var prop = await db.Properties.FirstOrDefaultAsync(p => p.Id == propertyId && p.ProducerId == producerId);
    if (prop is null) return Results.NotFound(new { message = "Propriedade n�o encontrada." });

    var plot = new Plot
    {
        PropertyId = prop.Id,
        Name = req.Name.Trim(),
        Crop = req.Crop.Trim(),
        Status = "Normal"
    };

    db.Plots.Add(plot);
    await db.SaveChangesAsync();

    return Results.Created($"/plots/{plot.Id}",
        new PlotResponse(plot.Id, plot.PropertyId, plot.Name, plot.Crop, plot.Status));
});

// GET /properties/{propertyId}/plots
app.MapGet("/properties/{propertyId:guid}/plots", [Authorize] async (ClaimsPrincipal user, Guid propertyId, PropertiesDbContext db) =>
{
    var producerId = ProducerId(user);

    var propOk = await db.Properties.AnyAsync(p => p.Id == propertyId && p.ProducerId == producerId);
    if (!propOk) return Results.NotFound(new { message = "Propriedade n�o encontrada." });

    var list = await db.Plots
        .Where(t => t.PropertyId == propertyId)
        .OrderBy(t => t.Name)
        .Select(t => new PlotResponse(t.Id, t.PropertyId, t.Name, t.Crop, t.Status))
        .ToListAsync();

    return Results.Ok(list);
});

app.Run();