using DisasterEventTracker.Data;
using DisasterEventTracker.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// Services (dependency injection container)
// ---------------------------------------------------------------
builder.Services.AddControllersWithViews();

// Database: EF Core + MySQL (XAMPP's MariaDB works too)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing from appsettings.json.");
var serverVersion = ServerVersion.Parse(builder.Configuration["Database:ServerVersion"] ?? "10.4.32-mariadb");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

// USGS earthquake service: typed HttpClient + in-memory cache
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IEarthquakeService, EarthquakeService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DisasterEventTracker/1.0");
});

// State management: server-side session (used to remember the last dashboard filters)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Authentication: cookie based login
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// ---------------------------------------------------------------
// Create the database and tables on startup.
// If you generated EF migrations they are applied, otherwise the tables are created directly.
// ---------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        if (db.Database.GetMigrations().Any())
        {
            db.Database.Migrate();
        }
        else
        {
            db.Database.EnsureCreated();
        }
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(
            "Could not connect to or create the MySQL database. " +
            "Start MySQL in the XAMPP Control Panel and check the connection string in appsettings.json.", ex);
    }
}

// ---------------------------------------------------------------
// HTTP request pipeline (the order matters)
// ---------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Earthquakes}/{action=Index}/{id?}");

app.Run();
