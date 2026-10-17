using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using TaskFlow.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Autenticación del frontend ----------
// El navegador nunca ve el JWT: el frontend lo guarda DENTRO de su cookie de sesión, que
// ASP.NET cifra y firma con Data Protection. En cada llamada a la API, BearerTokenHandler
// saca el token de la cookie y lo pone en "Authorization: Bearer ...".
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TaskFlow.Auth";
        options.Cookie.HttpOnly = true;                 // JavaScript no puede leerla (XSS)
        options.Cookie.SameSite = SameSiteMode.Lax;     // no viaja en POST de otros sitios (CSRF)
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = false;              // la cookie vence junto con el JWT
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddScoped<ApiSessionFilter>();

// MVC con vistas Razor y tres filtros globales:
//  - AuthorizeFilter: toda página requiere sesión salvo [AllowAnonymous] (login/registro).
//  - AutoValidateAntiforgeryToken: todo POST exige el token antifalsificación (CSRF).
//  - ApiSessionFilter: si la API responde 401 (token vencido o usuario desactivado),
//    cierra la sesión y manda al login.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.AddService<ApiSessionFilter>();
});

// Typed HttpClient hacia la API, con el handler que agrega el JWT.
var apiBaseUrl = builder.Configuration["TaskFlowApi:BaseUrl"]
    ?? throw new InvalidOperationException("Falta TaskFlowApi:BaseUrl en la configuración.");
builder.Services.AddHttpClient<TaskFlowApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(15);
    })
    .AddHttpMessageHandler<BearerTokenHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Cultura es-MX para fechas y números en las vistas.
var culture = CultureInfo.GetCultureInfo("es-MX");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = [culture],
    SupportedUICultures = [culture]
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();   // lee la cookie => HttpContext.User
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
