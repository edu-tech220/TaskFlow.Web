using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Web.Models;
using TaskFlow.Web.Services;

namespace TaskFlow.Web.Controllers;

public sealed class AccountController(TaskFlowApiClient api) : Controller
{
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Login(LoginViewModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(form);

        var result = await api.LoginAsync(form.Email, form.Password, ct);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(form);
        }

        await SignInAsync(result.Value!, form.RememberMe);
        return RedirectToLocal(form.ReturnUrl);
    }

    [AllowAnonymous]
    public IActionResult Register() =>
        User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View(new RegisterViewModel());

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(form);

        var result = await api.RegisterAsync(form.Email, form.DisplayName, form.Password, ct);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(form);
        }

        await SignInAsync(result.Value!, persistent: true);
        TempData["Success"] = result.Value!.User.Role == UserRole.Admin
            ? $"¡Bienvenido, {result.Value.User.DisplayName}! Eres el primer usuario, así que tienes rol de administrador."
            : $"¡Bienvenido, {result.Value.User.DisplayName}! Crea tu primera actividad para empezar.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // ---------- Perfil ----------

    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        var me = await api.GetMeAsync(ct);
        if (!me.IsSuccess)
            return View("ApiError", me.Error);

        return View(new ProfileViewModel { User = me.Value, DisplayName = me.Value!.DisplayName });
    }

    [HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            form.User = (await api.GetMeAsync(ct)).Value;
            return View(form);
        }

        var result = await api.UpdateProfileAsync(form.DisplayName, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Profile));
        }

        // El nombre vive en la cookie: se vuelve a emitir con el claim actualizado (mismo token).
        await RefreshNameClaimAsync(result.Value!.DisplayName);
        TempData["Success"] = "Perfil actualizado.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Profile));
        }

        var result = await api.ChangePasswordAsync(form.CurrentPassword, form.NewPassword, ct);
        if (result.IsSuccess) TempData["Success"] = "Contraseña actualizada.";
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Profile));
    }

    // ---------- Sesión ----------

    // Crea la cookie de sesión: claims para mostrar en la UI + el JWT para hablar con la API.
    private async Task SignInAsync(AuthResponseDto auth, bool persistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.User.Id.ToString()),
            new(ClaimTypes.Name, auth.User.DisplayName),
            new(ClaimTypes.Email, auth.User.Email),
            new(ClaimTypes.Role, auth.User.Role.ToString()),
            new(BearerTokenHandler.TokenClaim, auth.Token)
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = persistent,                                         // sobrevive a cerrar el navegador
            ExpiresUtc = DateTime.SpecifyKind(auth.ExpiresAt, DateTimeKind.Utc) // vence junto con el JWT
        });
    }

    private async Task RefreshNameClaimAsync(string displayName)
    {
        var current = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (current.Principal?.Identity is not ClaimsIdentity identity || current.Properties is null)
            return;

        if (identity.FindFirst(ClaimTypes.Name) is { } old)
            identity.RemoveClaim(old);
        identity.AddClaim(new Claim(ClaimTypes.Name, displayName));

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, current.Principal, current.Properties);
    }

    // Solo URLs locales: evita "open redirect" (que un enlace de login mande a un sitio ajeno).
    private IActionResult RedirectToLocal(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
}
