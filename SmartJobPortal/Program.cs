using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Common.Utilities;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Infrastructure.Data;
using SmartJobPortal.Infrastructure.Repositories;
using SmartJobPortal.Infrastructure.Services;
using System.Text;
using SmartJobPortal.API.Hubs;
using SmartJobPortal.API.Services;
using SmartJobPortal.Application;
using SmartJobPortal.Infrastructure;


var builder = WebApplication.CreateBuilder(args);

//  Use Newtonsoft.Json — handles multi-line strings, lenient parsing
builder.Services.AddControllers()
    .AddNewtonsoftJson(opts =>
    {
        opts.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        opts.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    });
builder.Services.AddSignalR();


//Add CORS
builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowAll", policy =>
  {
      policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();

      // Fallback for generic dev
      policy.SetIsOriginAllowed(origin => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
  });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter your token below."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("Default");
// ── Core Modules (Clean Architecture) ──────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── API Services ───────────────────────────────────────────────
builder.Services.AddScoped<INotificationHubService, NotificationHubService>();
builder.Services.AddScoped<IFeedHubService, FeedHubService>();
builder.Services.AddScoped<DataSeeder>();



// JWT
builder.Services.AddAuthentication("Bearer")
.AddJwtBearer("Bearer", options =>
{
    var key = builder.Configuration["Jwt:Key"];

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key!)
        )
    };

    //  Add logic to read JWT from Cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            else
            {
                var token = context.Request.Cookies["AuthToken"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
            }
            return Task.CompletedTask;
        },

        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var response = ApiResponse<string>.Unauthorized("Login Required. Please log in to access this resource.");
            return context.Response.WriteAsJsonAsync(response);
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var response = ApiResponse<string>.Fail("Access Denied. You are not authorized to access this resource.", 403);
            return context.Response.WriteAsJsonAsync(response);
        }
    };
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value != null && e.Value.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors)
            .Select(x => x.ErrorMessage)
            .ToList();

        var response = new ApiResponse<string>
        {
            Success = false,
            Errors = errors,
            Message = "Validation failed"
        };

        return new BadRequestObjectResult(response);
    };
});

// Use Distributed Memory Cache instead of Redis for easier local setup
// This provides immediate speed optimization without requiring external services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddScoped<ICacheService, RedisCacheService>();

var app = builder.Build();

// Global Exception Handling
app.UseMiddleware<SmartJobPortal.API.Middleware.ExceptionHandlingMiddleware>();

//  Run data seeder on startup 
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

//  Swagger 
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<FeedHub>("/hubs/feed");
app.Run();