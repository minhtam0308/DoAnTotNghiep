using BusinessAccessLayer.Mapping;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using CloudinaryDotNet;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Enums;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SapaBackend.Services;
using SapaFoRestRMSAPI.Services;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<SapaBackendContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyDatabase"), sqlOptions =>
    {
        sqlOptions.CommandTimeout(60); // 60 seconds command timeout
    });
});

//Show connection string in console
Console.WriteLine(builder.Configuration.GetConnectionString("MyDatabase"));



//  Đảm bảo hỗ trợ multipart form data
builder.Services.AddControllers()
    .AddNewtonsoftJson(); // Nếu dùng Newtonsoft.Json
builder.Services.AddEndpointsApiExplorer();
//log in token UI
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SapaFoRestSMS API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập token theo dạng: Bearer {token}"
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

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(options =>
{
    // Khi chưa đăng nhập mà vào trang yêu cầu auth, hệ thống sẽ redirect về /Auth/Login    
    options.LoginPath = "/Auth/Login";

    // Khi logout thì redirect về /Auth/Logout
    options.LogoutPath = "/Auth/Logout";

    // Khi không đủ quyền truy cập (AccessDenied) thì redirect về /Auth/AccessDenied
    options.AccessDeniedPath = "/Auth/AccessDenied";

    // Tên của cookie lưu trữ thông tin đăng nhập
    options.Cookie.Name = "Sapafreshway.Auth";

    // Cookie chỉ cho server đọc (client JS không đọc được) → tăng bảo mật
    options.Cookie.HttpOnly = true;

    // Thời gian sống của cookie (ở đây là 1 tiếng)
    options.ExpireTimeSpan = TimeSpan.FromHours(1);

    // Nếu người dùng hoạt động trong thời gian hiệu lực → hệ thống tự động kéo dài thêm hạn cookie
    options.SlidingExpiration = true;
});


builder.Services.AddAutoMapper(typeof(MappingProfile));


builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IOwnerDashboardService, OwnerDashboardService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminDashboardRepository, AdminDashboardRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ITableRepository, TableRepository>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IOwnerRevenueService, OwnerRevenueService>();
builder.Services.AddScoped<IInventoryIngredientRepository, InventoryIngredientRepository>();
builder.Services.AddScoped<IOwnerWarehouseAlertService, OwnerWarehouseAlertService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped<IStaffManagementService, StaffManagementService>();
builder.Services.AddScoped<IStaffManagementRepository, StaffManagementRepository>();
builder.Services.AddScoped<ICustomerManagementService, CustomerManagementService>();


builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
builder.Services.AddScoped<IMenuItemService, MenuItemService>();


// Đăng ký dịch vụ chạy ngầm của chúng ta
builder.Services.AddHostedService<OrderStatusUpdaterService>();
builder.Services.AddSignalR();


//  Cấu hình kích thước file upload (nếu cần)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50MB
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Roles.Admin, p => p.RequireRole(Roles.Admin));
    options.AddPolicy(Roles.Manager, p => p.RequireRole(Roles.Manager, Roles.Owner, Roles.Admin));
    options.AddPolicy(Roles.Staff, p => p.RequireRole(Roles.Staff));
    options.AddPolicy(Roles.Customer, p => p.RequireRole(Roles.Customer));
    options.AddPolicy(Roles.Owner, p => p.RequireRole(Roles.Owner));
    options.AddPolicy("AdminOrManager", p => p.RequireRole(Roles.Admin, Roles.Manager));
    options.AddPolicy("StaffOrManager", p => p.RequireRole(Roles.Staff, Roles.Manager));


    // Position-based policies for Staff (Owner/Admin/Manager always pass)
    bool HasManagementRole(ClaimsPrincipal user) =>
    user.IsInRole(Roles.Owner) || user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Manager);

    bool HasPositionClaim(ClaimsPrincipal user, int positionId)
    {
        var positionValue = positionId.ToString();
        var hasSingle = user.Claims.Any(c =>
            string.Equals(c.Type, "positionId", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.Value, positionValue, StringComparison.OrdinalIgnoreCase));

        var hasFromList = user.Claims.Any(c =>
            string.Equals(c.Type, "positionIds", StringComparison.OrdinalIgnoreCase) &&
            c.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Any(v => string.Equals(v.Trim(), positionValue, StringComparison.OrdinalIgnoreCase)));

        return hasSingle || hasFromList;
        }

    options.AddPolicy("Position:Waiter", policy =>
        policy.RequireAssertion(ctx => HasManagementRole(ctx.User) ||
            (ctx.User.IsInRole(Roles.Staff) && HasPositionClaim(ctx.User, 1))));

    options.AddPolicy("Position:Cashier", policy =>
        policy.RequireAssertion(ctx => HasManagementRole(ctx.User) ||
            (ctx.User.IsInRole(Roles.Staff) && HasPositionClaim(ctx.User, 2))));

    options.AddPolicy("Position:Kitchen", policy =>
        policy.RequireAssertion(ctx => HasManagementRole(ctx.User) ||
            (ctx.User.IsInRole(Roles.Staff) && HasPositionClaim(ctx.User, 3))));

    options.AddPolicy("Position:Inventory", policy =>
        policy.RequireAssertion(ctx => HasManagementRole(ctx.User) ||
            (ctx.User.IsInRole(Roles.Staff) && HasPositionClaim(ctx.User, 4))));
    });

// JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"] ?? "replace-with-strong-key"));
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        // Đọc từ appsettings.json hoặc appsettings.Development.json
        var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:5123" }; // Fallback mặc định nếu không có config

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();

app.UseCors(MyAllowSpecificOrigins);

//app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
