# Disaster Event Tracker

An ASP.NET Core MVC web application (Web Technologies with .NET mini project).
It shows live earthquake data from the USGS, and lets registered users keep their own
**tracker** of important earthquakes and a **watch list** of regions to keep an eye on.

> It is a monitoring and review tool. It is **not** a real-time warning system.

## Technology stack
- ASP.NET Core MVC (.NET 10), C#
- Entity Framework Core 9 with MySQL (Pomelo provider), database: XAMPP MySQL / MariaDB
- Razor views, Bootstrap 5, jQuery validation, Leaflet.js (map), Chart.js (charts)
- Data source: USGS Earthquake Hazards Program (GeoJSON feed, last 7 days)

## How to run
1. Open the XAMPP Control Panel and **Start MySQL** (Apache is not needed).
2. Open this folder in VS Code, open a terminal and run:
   ```
   dotnet restore
   dotnet run
   ```
3. Open http://localhost:5080
4. The database `disaster_event_tracker` and its tables are created automatically on first start.

Connection string: `appsettings.json` (default XAMPP: user `root`, empty password).
If your MariaDB/MySQL version differs, change `Database:ServerVersion` (for example `8.0.36-mysql`).

### Optional: use EF Core migrations instead of automatic creation
```
dotnet tool install --global dotnet-ef --version "9.*"
dotnet ef migrations add InitialCreate
dotnet ef database update
```
(If the database was already created automatically, drop it in phpMyAdmin first.)

## Where each project requirement is implemented
| Requirement | Where |
|---|---|
| MVC architecture | `Models/`, `Views/`, `Controllers/`, plus `Services/` and `ViewModels/` |
| EF Core + MySQL | `Data/AppDbContext.cs`, `Program.cs` (UseMySql), entities in `Models/` |
| CRUD | `SavedEventsController` (saved earthquakes), `WatchListController` (watch list) |
| Form handling | Dashboard filter form (GET), Create/Edit/Delete forms (POST), Login/Register |
| Model validation | DataAnnotations in `ViewModels/` and `Models/`, `ModelState`, custom checks (duplicates, min <= max) |
| State management | Session (last dashboard filters), TempData (messages), cookies (theme and login) |
| Responsive UI | Bootstrap 5 grid, mobile navbar, responsive tables |
| User accounts | `AccountController`: register, login, logout, PBKDF2 password hashing, cookie authentication |

## Security notes
- Passwords are never stored, only a salted PBKDF2 hash (`Services/PasswordHelper.cs`).
- All forms use anti-forgery tokens. Saved events and watches are always queried with the logged-in user's id.
- The earthquake details are re-loaded on the server when saving, not trusted from the browser.
- Text from USGS is HTML-encoded by Razor (and inserted with `textContent` in JavaScript).
