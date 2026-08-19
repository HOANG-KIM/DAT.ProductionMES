using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using ProductionMES.Api.Authentication;
using ProductionMES.Api.Authorization;
using ProductionMES.Api.ExceptionHandling;
using ProductionMES.Api.Filters;
using ProductionMES.Api.Hubs;
using ProductionMES.Api.Realtime;
using ProductionMES.Application.Abstractions.Realtime;
using ProductionMES.Application.DependencyInjection;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
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
            // US-22/ADR-004: TẮT hẳn cơ chế inbound claim type mapping mặc định — nếu không, claim "role"
            // (ngắn) trong JWT bị tự động đổi tên thành URI dài (ClaimTypes.Role) TRƯỚC khi gắn vào
            // ClaimsPrincipal, khiến việc chỉ set RoleClaimType = "role" bên dưới KHÔNG đủ để tìm đúng claim
            // (context.User.FindFirst("role") trả về null dù JWT có claim "role" hợp lệ) — bug thật phát hiện
            // khi test end-to-end lần đầu (mọi kiểm thử trước đó chỉ là unit test mock, không chạm middleware
            // xác thực thật), khiến MỌI request bị 403 dù JWT đúng.
            options.MapInboundClaims = false;
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
                // US-22: khớp đúng claim type "role" (ngắn) do JwtTokenGenerator phát hành — chỉ có hiệu lực
                // đúng khi đã tắt MapInboundClaims ở trên.
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
        })
        // US-04a/ADR-005: scheme riêng cho Station.Wpf (luồng scan thường, trạm là đơn vị xác thực, Operator
        // không đăng nhập cá nhân) — chạy song song với "Bearer" ở trên, KHÔNG đổi default scheme (vẫn là
        // JwtBearerDefaults.AuthenticationScheme, dùng cho web-admin/Supervisor).
        .AddScheme<AuthenticationSchemeOptions, StationApiKeyAuthenticationHandler>(
            StationApiKeyDefaults.AuthenticationScheme, options => { });
    // ADR-004: permission động (Resource, Action) lưu DB thay cho [Authorize(Roles=...)] hardcode mức Controller
    // — mỗi policy dưới đây tương ứng đúng 1 hành động thật đang tồn tại ở Controller (không phải tích chéo đầy
    // đủ Resource x Action), khớp catalog seed ở DbSeeder.SeedPermissionsAsync.
    builder.Services.AddAuthorization(options =>
    {
        void AddPermissionPolicy(string policyName, PermissionResource resource, PermissionAction action)
            => options.AddPolicy(policyName, policy => policy.Requirements.Add(new PermissionRequirement(resource, action)));

        AddPermissionPolicy(PermissionPolicies.LineView, PermissionResource.Line, PermissionAction.View);
        AddPermissionPolicy(PermissionPolicies.LineCreate, PermissionResource.Line, PermissionAction.Create);
        AddPermissionPolicy(PermissionPolicies.LineUpdate, PermissionResource.Line, PermissionAction.Update);
        AddPermissionPolicy(PermissionPolicies.LineDeactivate, PermissionResource.Line, PermissionAction.Deactivate);

        AddPermissionPolicy(PermissionPolicies.BreakWindowView, PermissionResource.BreakWindow, PermissionAction.View);
        AddPermissionPolicy(PermissionPolicies.BreakWindowCreate, PermissionResource.BreakWindow, PermissionAction.Create);
        AddPermissionPolicy(PermissionPolicies.BreakWindowUpdate, PermissionResource.BreakWindow, PermissionAction.Update);
        AddPermissionPolicy(PermissionPolicies.BreakWindowDelete, PermissionResource.BreakWindow, PermissionAction.Delete);

        AddPermissionPolicy(PermissionPolicies.StationApiKeyView, PermissionResource.StationApiKey, PermissionAction.View);
        AddPermissionPolicy(PermissionPolicies.StationApiKeyCreate, PermissionResource.StationApiKey, PermissionAction.Create);
        AddPermissionPolicy(PermissionPolicies.StationApiKeyUpdate, PermissionResource.StationApiKey, PermissionAction.Update);
        AddPermissionPolicy(PermissionPolicies.StationApiKeyDeactivate, PermissionResource.StationApiKey, PermissionAction.Deactivate);

        AddPermissionPolicy(PermissionPolicies.StageView, PermissionResource.Stage, PermissionAction.View);
        AddPermissionPolicy(PermissionPolicies.StageCreate, PermissionResource.Stage, PermissionAction.Create);
        AddPermissionPolicy(PermissionPolicies.StageUpdate, PermissionResource.Stage, PermissionAction.Update);
        AddPermissionPolicy(PermissionPolicies.StageDeactivate, PermissionResource.Stage, PermissionAction.Deactivate);

        AddPermissionPolicy(PermissionPolicies.WorkStationView, PermissionResource.WorkStation, PermissionAction.View);
        AddPermissionPolicy(PermissionPolicies.WorkStationCreate, PermissionResource.WorkStation, PermissionAction.Create);
        AddPermissionPolicy(PermissionPolicies.WorkStationUpdate, PermissionResource.WorkStation, PermissionAction.Update);
        AddPermissionPolicy(PermissionPolicies.WorkStationDeactivate, PermissionResource.WorkStation, PermissionAction.Deactivate);

        AddPermissionPolicy(PermissionPolicies.ProductionPlanView, PermissionResource.ProductionPlan, PermissionAction.View);
        AddPermissionPolicy(PermissionPolicies.ProductionPlanCreate, PermissionResource.ProductionPlan, PermissionAction.Create);
        AddPermissionPolicy(PermissionPolicies.ProductionPlanUpdate, PermissionResource.ProductionPlan, PermissionAction.Update);

        AddPermissionPolicy(PermissionPolicies.ProductionPlanStageView, PermissionResource.ProductionPlanStage, PermissionAction.View);
        AddPermissionPolicy(PermissionPolicies.ProductionPlanStageCreate, PermissionResource.ProductionPlanStage, PermissionAction.Create);
        AddPermissionPolicy(PermissionPolicies.ProductionPlanStageUpdate, PermissionResource.ProductionPlanStage, PermissionAction.Update);
        AddPermissionPolicy(PermissionPolicies.ProductionPlanStageDelete, PermissionResource.ProductionPlanStage, PermissionAction.Delete);
        AddPermissionPolicy(PermissionPolicies.ProductionPlanStageApply, PermissionResource.ProductionPlanStage, PermissionAction.Apply);
        AddPermissionPolicy(PermissionPolicies.ProductionPlanStagePause, PermissionResource.ProductionPlanStage, PermissionAction.Pause);
        AddPermissionPolicy(PermissionPolicies.ProductionPlanStageClose, PermissionResource.ProductionPlanStage, PermissionAction.Close);

        // US-10 AC2/AC3: tra cứu lịch sử scan.
        AddPermissionPolicy(PermissionPolicies.ScanView, PermissionResource.Scan, PermissionAction.View);
        // US-18 (thay đổi yêu cầu 18/08/2026): xác nhận Scan NG tại trạm.
        AddPermissionPolicy(PermissionPolicies.ScanConfirmNg, PermissionResource.Scan, PermissionAction.ConfirmNg);
        // US-19 AC2/AC6: "Mở khóa rework" cho tem bị NG.
        AddPermissionPolicy(PermissionPolicies.ScanReworkUnlock, PermissionResource.Scan, PermissionAction.ReworkUnlock);

        // US-21: báo cáo tổng hợp ACTUAL/PLAN/BALANCE theo Line/công đoạn.
        AddPermissionPolicy(PermissionPolicies.ReportView, PermissionResource.Report, PermissionAction.View);
    });
    builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

    // SignalR (real-time) — serialize enum (vd. ScanResultDto.Result) ra chuỗi thay vì số, đồng bộ với
    // AddControllers().AddJsonOptions() ở trên (HTTP) để cùng 1 DTO trả về cùng format qua cả 2 kênh.
    builder.Services.AddSignalR()
        .AddJsonProtocol(options =>
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    // US-07/US-08: implementation IScanNotifier dùng IHubContext<ScanHub> — đặt ở Api vì ScanHub chỉ tồn tại ở đây.
    builder.Services.AddScoped<IScanNotifier, ScanHubNotifier>();

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

        // ADR-004: seed catalog Permission + RolePermission ban đầu — cùng lý do bọc try/catch như seed Admin
        // ở trên (không chặn khởi động app khi DB chưa sẵn sàng).
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await DbSeeder.SeedPermissionsAsync(dbContext);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Không seed được Permission/RolePermission mặc định (có thể do chưa kết nối được MySQL).");
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
