using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using LearnHub.API.Middlewares;
using LearnHub.Repositories.Interfaces;
using LearnHub.Services;
using LearnHub.Services.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddServices(builder.Configuration);
builder.Services.AddHttpContextAccessor();

// Controllers
builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

builder.Services.AddAuthorization();

builder
    .Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>(
        (options, jwtOptions) =>
        {
            var jwt = jwtOptions.Value;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,

                ValidateAudience = true,
                ValidAudience = jwt.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (
                        !context.Request.Headers.ContainsKey("Authorization")
                        && context.Request.Cookies.TryGetValue("access_token", out var token)
                    )
                    {
                        context.Token = token;
                    }

                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var userRepository =
                        context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();

                    var userIdClaim = context
                        .Principal?.FindFirst(ClaimTypes.NameIdentifier)
                        ?.Value;

                    var versionClaim = context.Principal?.FindFirst("tokenVersion")?.Value;

                    if (
                        !Guid.TryParse(userIdClaim, out var userId)
                        || !int.TryParse(versionClaim, out var tokenVersion)
                    )
                    {
                        context.Fail("Invalid token claims.");
                        return;
                    }

                    var user = await userRepository.GetByIdAsync(userId);

                    if (user is null || user.TokenVersion != tokenVersion)
                    {
                        context.Fail("Token is no longer valid.");
                        return;
                    }

                    if (!user.IsActive)
                    {
                        context.Fail("User is inactive.");
                        return;
                    }

                    // Synchronize role with the database.
                    var identity = context.Principal!.Identity as ClaimsIdentity;

                    if (identity is not null)
                    {
                        foreach (var claim in identity.FindAll(ClaimTypes.Role).ToList())
                        {
                            identity.RemoveClaim(claim);
                        }

                        identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
                    }
                    context.HttpContext.Items["User"] = user;
                },
            };
        }
    );

// Application
var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

// if (app.Environment.IsDevelopment())
// {
app.MapOpenApi();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "LearnHub API V1");
    options.ConfigObject.AdditionalItems["withCredentials"] = true;
});

// }

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => "Welcome to LearnHub API");

app.Run();

public partial class Program { }
