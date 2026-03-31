using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using WebSapaFreshWayStaff.Services.Api;
using WebSapaFreshWayStaff.Services.Api.Interfaces;
using WebSapaFreshWayStaff.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri("http://localhost:5013");
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };
});

builder.Services.AddHttpClient<ApiService>();
builder.Services.AddScoped<ApiService>();
//builder.Services.AddHttpClient<KitchenDisplayService>(); // để inject HttpClient cho KitchenDisplayService
//builder.Services.AddScoped<KitchenDisplayService>();     // để inject KitchenDisplayService
builder.Services.AddHttpContextAccessor();    // để dùng Session trong ApiService
builder.Services.AddSession();


// Register API Services with Dependency Injection
builder.Services.AddHttpClient<IAuthApiService, AuthApiService>();
builder.Services.AddHttpClient<IUserApiService, UserApiService>();
builder.Services.AddHttpClient<IOwnerDashboardApiService, OwnerDashboardApiService>();
builder.Services.AddHttpClient<IAdminDashboardApiService, AdminDashboardApiService>();
builder.Services.AddHttpClient<IOwnerRevenueApiService, OwnerRevenueApiService>();
builder.Services.AddHttpClient<IOwnerWarehouseAlertApiService, OwnerWarehouseAlertApiService>();



// Keep backward compatibility with old ApiService (can be removed after migration)
builder.Services.AddHttpClient<ApiService>();
builder.Services.AddScoped<ApiService>();



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        b => b.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});


builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.Name = "Sapafreshway.Auth";
        options.SlidingExpiration = true;
    });



builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Owner", p => p.RequireRole("Owner"));
    options.AddPolicy("Admin", p => p.RequireRole("Admin", "Owner"));
    options.AddPolicy("Manager", p => p.RequireRole("Manager", "Admin", "Owner"));
    options.AddPolicy("Staff", p => p.RequireRole("Staff", "Manager", "Admin", "Owner"));
    options.AddPolicy("Customer", p => p.RequireRole("Customer"));

    bool HasManagementRole(ClaimsPrincipal user) =>
        user.IsInRole("Owner") || user.IsInRole("Admin") || user.IsInRole("Manager");

    bool HasPositionClaim(ClaimsPrincipal user, int positionId)
    {
        var positionValue = positionId.ToString();
        var hasSingle = user.Claims.Any(c =>
            string.Equals(c.Type, "PositionId", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.Value, positionValue, StringComparison.OrdinalIgnoreCase));

        var hasFromList = user.Claims.Any(c =>
            string.Equals(c.Type, "PositionIds", StringComparison.OrdinalIgnoreCase) &&
            c.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Any(v => string.Equals(v.Trim(), positionValue, StringComparison.OrdinalIgnoreCase)));

        return hasSingle || hasFromList;
    }

    options.AddPolicy("Position:Waiter", policy =>
        policy.RequireAssertion(ctx => HasManagementRole(ctx.User) ||
            (ctx.User.IsInRole("Staff") && HasPositionClaim(ctx.User, 1))));

    options.AddPolicy("Position:Cashier", policy =>
        policy.RequireAssertion(ctx => HasManagementRole(ctx.User) ||
            (ctx.User.IsInRole("Staff") && HasPositionClaim(ctx.User, 2))));

    options.AddPolicy("Position:Kitchen", policy =>
        policy.RequireAssertion(ctx => HasManagementRole(ctx.User) ||
            (ctx.User.IsInRole("Staff") && HasPositionClaim(ctx.User, 3))));

    options.AddPolicy("Position:Inventory", policy =>
        policy.RequireAssertion(ctx => HasManagementRole(ctx.User) ||
            (ctx.User.IsInRole("Staff") && HasPositionClaim(ctx.User, 4))));

    // OR logic: Allow access if user has Waiter OR Cashier position
    options.AddPolicy("Position:WaiterOrCashier", policy =>
        policy.RequireAssertion(ctx => HasManagementRole(ctx.User) ||
            (ctx.User.IsInRole("Staff") &&
             (HasPositionClaim(ctx.User, 1) || HasPositionClaim(ctx.User, 2)))));
});

builder.Services.AddSignalR();

var app = builder.Build();
app.UseSession();
app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        var token = context.Session.GetString("Token");
        var refreshToken = context.Session.GetString("RefreshToken");

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
        {
            await context.SignOutAsync("Cookies");
            context.Session.Clear();

            if (!context.Response.HasStarted)
            {
                context.Response.Redirect("/Auth/Login");
            }
            return;
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
