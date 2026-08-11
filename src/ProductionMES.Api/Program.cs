using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ProductionMES.Api.Hubs;
using ProductionMES.Application.DependencyInjection;
using ProductionMES.Infrastructure.DependencyInjection;
using Serilog;

// Bootstrap logger — dùng tạm trong lúc host chưa khởi tạo xong, để không mất log nếu có lỗi ngay từ đầu.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Đang khởi động ProductionMES.Api...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    // Đăng ký service theo từng layer
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    builder.Services.AddControllers();

    // Swagger / API docs
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // JWT Authentication (placeholder — key/issuer/audience cần thay bằng giá trị thật khi deploy)
    var jwtSection = builder.Configuration.GetSection("Jwt");
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSection["Key"] ?? string.Empty))
            };
        });
    builder.Services.AddAuthorization();

    // SignalR (real-time)
    builder.Services.AddSignalR();

    // Health checks (kèm check MySQL)
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    builder.Services
        .AddHealthChecks()
        .AddMySql(connectionString, name: "mysql");

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<ScanHub>("/hubs/scan");
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ProductionMES.Api dừng đột ngột do lỗi khởi động");
}
finally
{
    Log.CloseAndFlush();
}
