using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyClinic.Infrastructure.Interfaces.Repositories;
using MyClinic.Infrastructure.Interfaces.Services;
using MyClinic.Application.Validators;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Data;
using MyClinic.Infrastructure.Repositories;
using MyClinic.Infrastructure.Servives;
using MyClinic.Application.Mapping;
using MyClinic.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Serilog;
using System.Security.Claims;

namespace MyClinic
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console() 
                .WriteTo.File("logs/myapp-.txt", rollingInterval: RollingInterval.Day) 
                .CreateLogger();
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("MyClinic.Infrastructure"))
            );
            builder.Services.AddAutoMapper(typeof(MappingProfile));
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            builder.Services.AddScoped<IAvailabilityRepository, AvailabilityRepository>();
            builder.Services.AddScoped<ISlotConfigRepository, SlotConfigRepository>();

            // Add services to the container.
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<IDoctorService, DoctorService>();
            builder.Services.AddScoped<ISlotConfigService, SlotConfigService>();
            builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
            builder.Services.AddScoped<IAppointmentService, AppointmentService>();

            //COR
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",policy =>
                {
                    policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod();
                });
            });

            builder.Services.AddControllers()
                 .AddFluentValidation(fv =>
                 {
                    
                     fv.RegisterValidatorsFromAssemblyContaining<SyncProfileRequestValidator>();
                 }); 


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Register authorization handler for database-based role checks
            builder.Services.AddScoped<IAuthorizationHandler, DatabaseRoleHandler>();

            // Add authorization with custom policies
            builder.Services.AddAuthorization(options =>
            {
                // Admin policy: Check Keycloak for admin role, with database fallback
                options.AddPolicy("AdminPolicy", policy =>
                    policy.Requirements.Add(new DatabaseRoleRequirement("admin")));

                // Doctor policy: Check database for doctor role
                options.AddPolicy("DoctorPolicy", policy =>
                    policy.Requirements.Add(new DatabaseRoleRequirement("doctor")));

                // Patient policy: Check database for patient role
                options.AddPolicy("PatientPolicy", policy =>
                    policy.Requirements.Add(new DatabaseRoleRequirement("patient")));
            });

            builder
                .Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    var authority = builder.Configuration["Keycloak:Authority"] 
                                    ?? "http://localhost:8080/realms/MyClinicRealm";
                    var audience = builder.Configuration["Keycloak:Audience"] 
                                   ?? "myclinic-frontend";
                    
                    options.Authority = authority;
                    options.Audience = audience;
                    options.RequireHttpsMetadata = false;
                    options.MetadataAddress = $"{authority}/.well-known/openid-configuration";
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false,
                        ValidateIssuer = true,
                        ValidIssuer = "http://localhost:8080/realms/MyClinicRealm",
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(5),
                        RoleClaimType = ClaimTypes.Role
                    };
                    
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context =>
                        {
                            if (context.Principal?.Identity is ClaimsIdentity claimsIdentity)
                            {
                                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                                
                                // Extract Keycloak roles from realm_access.roles
                                var token = context.SecurityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
                                if (token != null && token.Payload.TryGetValue("realm_access", out var realmAccessObj))
                                {
                                    try
                                    {
                                        var realmAccessJson = System.Text.Json.JsonSerializer.Serialize(realmAccessObj);
                                        using var doc = System.Text.Json.JsonDocument.Parse(realmAccessJson);
                                        
                                        if (doc.RootElement.TryGetProperty("roles", out var rolesElement))
                                        {
                                            foreach (var role in rolesElement.EnumerateArray())
                                            {
                                                var roleValue = role.GetString();
                                                if (!string.IsNullOrEmpty(roleValue))
                                                {
                                                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                                                    logger.LogInformation("Added role claim: {Role}", roleValue);
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.LogError(ex, "Error parsing realm_access roles");
                                    }
                                }
                                
                                // Log all claims for debugging
                                logger.LogInformation("All claims after transformation:");
                                foreach (var claim in claimsIdentity.Claims)
                                {
                                    logger.LogInformation("  {Type}: {Value}", claim.Type, claim.Value);
                                }
                            }
                            return Task.CompletedTask;
                        }
                    };
                });
            builder.Host.UseSerilog();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseHttpsRedirection();
            //app.MapGet("/", () =>
            //{
            //    Log.Information("Hello request received at {Time}", DateTime.UtcNow);
            //    return "Hello World!";
            //});
            app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
