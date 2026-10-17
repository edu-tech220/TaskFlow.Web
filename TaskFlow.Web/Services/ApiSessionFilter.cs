using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace TaskFlow.Web.Services;

// Filtro de acción global: corre DESPUÉS de la acción y ANTES de mostrar la vista.
// Si alguna llamada a la API respondió 401, la sesión del frontend ya no sirve:
// se cierra y se reemplaza el resultado por un redirect al login (con returnUrl).
public sealed class ApiSessionFilter(ITempDataDictionaryFactory tempDataFactory) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        var http = context.HttpContext;

        if (!http.Items.ContainsKey(BearerTokenHandler.UnauthorizedFlag))
            return;

        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var tempData = tempDataFactory.GetTempData(http);
        tempData["Error"] = "Tu sesión expiró o tu cuenta fue desactivada. Inicia sesión de nuevo.";
        tempData.Save();

        var returnUrl = http.Request.Method == HttpMethods.Get
            ? http.Request.Path + http.Request.QueryString
            : "/";
        executed.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
    }
}
