using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ProductionMES.Api.ExceptionHandling;
using ProductionMES.Api.Filters;
using ProductionMES.Api.Hubs;
using ProductionMES.Application.DependencyInjection;
using ProductionMES.Domain.Entities;
using ProductionMES.Infrastructure.DependencyInjection;
using ProductionMES.Infrastructure.Persistence;
using ProductionMES.Infrastructure.Security;
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
    builder.Services.AddApplicationServices(builder.Configuration);
    builder.Services.AddInfrastructureServices(builder.Configuration);

    builder.Services.AddControllers(options =>
    {
        // Tự động chạy FluentValidation cho mọi action có tham số đăng ký IValidator<T> tương ứng.
        options.Filters.Add<FluentValidationActionFilter>();
        // ADR-003: chống CSRF cho mọi request POST/PUT — bổ sung cho SameSite=Strict.
        options.Filters.Add<AntiforgeryActionFilter>();
    })
    // Serialize enum (vd. UserRole) ra chuỗi thay vì số — tránh giá trị vô nghĩa phía client và tránh vỡ
    // hợp đồng API khi thêm enum value mới ở giữa (xem Documents/API-Conventions.md mục 10).
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    // Middleware xử lý exception toàn cục — chuyển exception chưa bắt thành ProblemDetails.
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Swagger / API docs
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // ADR-003: CORS chặt cho web-admin (origin cụ thể, không dùng "*") — bắt buộc khi dùng cookie cho request
    // cross-origin, vì AllowCredentials() không tương thích với AllowAnyOrigin().
    const string webAdminCorsPolicy = "WebAdmin";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(webAdminCorsPolicy, policy =>
        {
            var webAdminOrigin = builder.Configuration["Cors:WebAdminOrigin"] ?? string.Empty;
            policy.WithOrigins(webAdminOrigin)
                .AllowCredentials()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    // ADR-003: Antiforgery (CSRF) — cookie XSRF-TOKEN đọc được từ JS (khác access_token/refresh_token, luôn
    // HttpOnly) để client echo lại vào header X-CSRF-TOKEN cho mọi request POST/PUT.
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-TOKEN";
        options.Cookie.Name = "XSRF-TOKEN";
        options.Cookie.HttpOnly = false;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

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
                    Encoding.UTF8.GetBytes(jwtSection["Key"] ?? string.Empty)),
                // US-22: khớp đúng claim type "role" (ngắn) do JwtTokenGenerator phát hành, không phụ thuộc
                // cơ chế inbound claim type mapping mặc định — để [Authorize(Roles = "...")] hoạt động đúng.
                RoleClaimType = JwtTokenGenerator.RoleClaimType,
            };
            // ADR-003: đọc access token từ cookie HttpOnly thay vì header Authorization: Bearer.
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Cookies.TryGetValue("access_token", out var token))
                    {
                        context.Token = token;
                    }

                    return Task.CompletedTask;
                },
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

    app.UseExceptionHandler();

    app.UseHttpsRedirection();

    app.UseCors(webAdminCorsPolicy);

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<ScanHub>("/hubs/scan");
    app.MapHealthChecks("/health");

    // US-22: seed 1 tài khoản Admin mặc định nếu hệ thống chưa có người dùng nào — để đăng nhập/test
    // end-to-end ngay từ lần chạy đầu tiên. Bọc try/catch để không chặn khởi động app khi DB chưa sẵn sàng
    // (vd môi trường dev chưa cấu hình MySQL) — hệ thống vẫn chạy được, chỉ là chưa seed được tài khoản.
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            await DbSeeder.SeedDefaultAdminAsync(dbContext, passwordHasher);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Không seed được tài khoản Admin mặc định (có thể do chưa kết nối được MySQL).");
        }
    }

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
