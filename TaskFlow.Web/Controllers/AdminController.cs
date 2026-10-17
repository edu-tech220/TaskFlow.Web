using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Web.Models;
using TaskFlow.Web.Services;

namespace TaskFlow.Web.Controllers;

// La protección real está en la API (devuelve 403 si no eres Admin). Este atributo solo
// evita mostrar la página a quien no lo es.
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminController(TaskFlowApiClient api) : Controller
{
    public async Task<IActionResult> Users(CancellationToken ct)
    {
        var result = await api.GetUsersAsync(ct);
        return result.IsSuccess ? View(result.Value) : View("ApiError", result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateUser(int id, UserRole role, bool isActive, CancellationToken ct)
    {
        var result = await api.UpdateUserAsync(id, role, isActive, ct);
        if (result.IsSuccess)
            TempData["Success"] = $"{result.Value!.DisplayName}: {(result.Value.IsActive ? "activo" : "desactivado")}, rol {result.Value.Role}.";
        else
            TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Users));
    }
}
