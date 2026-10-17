using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Web.Models;
using TaskFlow.Web.Services;

namespace TaskFlow.Web.Controllers;

public class HomeController(TaskFlowApiClient api) : Controller
{
    // Dashboard: resumen de hoy, métricas del mes, racha y últimos 7 días.
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var now = DateTime.Now;
        var current = await api.GetCalendarAsync(now.Year, now.Month, ct);
        if (!current.IsSuccess)
            return View("ApiError", current.Error);

        var month = current.Value!;
        var today = month.Today;

        // Mes anterior: para la racha y la semana cuando cruzan el inicio de mes.
        var prev = new DateOnly(month.Year, month.Month, 1).AddMonths(-1);
        var previous = await api.GetCalendarAsync(prev.Year, prev.Month, ct);
        var days = (previous.Value?.Days ?? []).Concat(month.Days).ToList();

        var activities = await api.GetActivitiesAsync(ct);

        return View(new DashboardViewModel
        {
            UserName = User.DisplayName(),
            Today = today,
            TodayDay = month.Days.Single(d => d.Date == today),
            Month = MonthStats.From(month),
            RestDayLimit = month.RestDayLimit,
            RestDaysUsed = month.RestDaysUsed,
            ActiveActivities = activities.Value?.Count(a => a.IsActive) ?? 0,
            Streak = DashboardViewModel.ComputeStreak(days, today),
            LastSevenDays = days.Where(d => d.Date > today.AddDays(-7) && d.Date <= today).OrderBy(d => d.Date).ToList()
        });
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
