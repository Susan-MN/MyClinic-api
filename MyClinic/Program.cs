using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyClinic.Application.Interfaces.Repositories;
using MyClinic.Application.Interfaces.Services;
using MyClinic.Application.Validators;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Data;
using MyClinic.Infrastructure.Repositories;
using MyClinic.Infrastructure.Servives;
using MyClinic.Application.Mapping;
using Serilog;

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
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
            );
            builder.Services.AddAutoMapper(typeof(MappingProfile));
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            // Add services to the container.
            builder.Services.AddScoped<IProfileService, ProfileService>();

            //CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
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

            builder
                .Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = "http://keycloak-host:8080/auth/realms/MyClinicRealm";
                    options.Audience = "myclinic-frontend";
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidAudience = "myclinic-frontend",
                        ValidateIssuer = true,
                        ValidIssuer = "http://keycloak-host:8080/auth/realms/MyClinicRealm",
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

            app.UseHttpsRedirection();
            //app.MapGet("/", () =>
            //{
            //    Log.Information("Hello request received at {Time}", DateTime.Now);
            //    return "Hello World!";
            //});
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
