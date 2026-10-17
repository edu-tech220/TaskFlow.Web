using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;

namespace TaskFlow.Web.Models;

// ======================= Cuenta =======================

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "Correo no válido.")]
    [Display(Name = "Correo")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Mantener la sesión iniciada")]
    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Escribe tu nombre.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Entre 2 y 100 caracteres.")]
    [Display(Name = "Nombre")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "Correo no válido.")]
    [Display(Name = "Correo")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe una contraseña.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Incluye al menos una letra y un número.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ProfileViewModel
{
    public UserDto? User { get; set; }

    [Required(ErrorMessage = "Escribe tu nombre.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Entre 2 y 100 caracteres.")]
    [Display(Name = "Nombre")]
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Escribe tu contraseña actual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe la nueva contraseña.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Incluye al menos una letra y un número.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string NewPassword { get; set; } = string.Empty;

    [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nueva contraseña")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

// ======================= Actividades =======================

// Formulario de alta/edición de actividad. La validación del lado del cliente es solo
// comodidad; la API vuelve a validar todo y es la fuente de verdad.
public sealed class ActivityFormViewModel : IValidatableObject
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    // En edición, el horario solo se envía si se marca este switch (versionado).
    [Display(Name = "Cambiar horario")]
    public bool ChangeSchedule { get; set; } = true;

    [Display(Name = "Inicio")]
    public TimeOnly? StartTime { get; set; }

    [Display(Name = "Fin")]
    public TimeOnly? EndTime { get; set; }

    [Display(Name = "Días")]
    public List<DayOfWeek> Days { get; set; } = [];

    [Display(Name = "Aplica desde")]
    public DateOnly? EffectiveFrom { get; set; }

    // Historial para mostrar en la edición.
    public IReadOnlyList<ScheduleVersionDto> Versions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ChangeSchedule)
            yield break;

        if (StartTime is null || EndTime is null)
            yield return new ValidationResult("Indica hora de inicio y fin.", [nameof(StartTime)]);
        else if (EndTime <= StartTime)
            yield return new ValidationResult("La hora de fin debe ser mayor que la de inicio.", [nameof(EndTime)]);

        if (Days.Count == 0)
            yield return new ValidationResult("Selecciona al menos un día.", [nameof(Days)]);
    }

    // Cuerpo JSON para la API.
    public object ToCreateRequest() => new
    {
        name = Name,
        description = Description,
        schedule = new { startTime = StartTime, endTime = EndTime, days = Days, effectiveFrom = EffectiveFrom }
    };

    public object ToUpdateRequest() => new
    {
        name = Name,
        description = Description,
        schedule = ChangeSchedule
            ? new { startTime = StartTime, endTime = EndTime, days = Days, effectiveFrom = EffectiveFrom }
            : null
    };
}

// ======================= Calendario y dashboard =======================

public sealed class CalendarViewModel
{
    public required CalendarMonthDto Calendar { get; init; }

    public DateOnly FirstOfMonth => new(Calendar.Year, Calendar.Month, 1);
    public DateOnly PreviousMonth => FirstOfMonth.AddMonths(-1);
    public DateOnly NextMonth => FirstOfMonth.AddMonths(1);

    // Celdas vacías antes del día 1 para una semana que empieza en lunes.
    public int LeadingBlanks => ((int)FirstOfMonth.DayOfWeek + 6) % 7;

    public MonthStats Stats => MonthStats.From(Calendar);
}

public sealed class DayViewModel
{
    public required CalendarDayDto Day { get; init; }
    public required DateOnly Today { get; init; }
    public required int RestDayLimit { get; init; }
    public required int RestDaysUsed { get; init; }

    public bool CanRecord => Day.Date <= Today;        // regla 12
    public bool CanToggleRestDay => Day.Date >= Today; // reglas 8 y 11
}

// Métricas del mes calculadas en el frontend a partir del calendario (sin llamadas extra).
public sealed record MonthStats(int Scheduled, int Completed, int CompletedDays, int FailedDays, int PartialDays)
{
    public int CompletionPercent => Scheduled == 0 ? 0 : (int)Math.Round(100.0 * Completed / Scheduled);

    public static MonthStats From(CalendarMonthDto month)
    {
        // Solo días ya transcurridos (hasta hoy) y que no son descanso.
        var elapsed = month.Days.Where(d => d.Date <= month.Today && !d.IsRestDay).ToList();
        return new MonthStats(
            elapsed.Sum(d => d.ScheduledCount),
            elapsed.Sum(d => d.CompletedCount),
            elapsed.Count(d => d.Status == DayStatus.Completed),
            elapsed.Count(d => d.Status == DayStatus.Failed),
            elapsed.Count(d => d.Status == DayStatus.Partial));
    }
}

public sealed class DashboardViewModel
{
    public required string UserName { get; init; }
    public required DateOnly Today { get; init; }
    public required CalendarDayDto TodayDay { get; init; }
    public required MonthStats Month { get; init; }
    public required int RestDayLimit { get; init; }
    public required int RestDaysUsed { get; init; }
    public required int ActiveActivities { get; init; }
    public required int Streak { get; init; }
    public required IReadOnlyList<CalendarDayDto> LastSevenDays { get; init; }

    public string Greeting => DateTime.Now.Hour switch
    {
        < 12 => "Buenos días",
        < 19 => "Buenas tardes",
        _ => "Buenas noches"
    };

    // Racha: días completados seguidos hacia atrás. Descansos y días sin actividades no la
    // rompen ni la suman; hoy solo cuenta si ya está completo (si sigue en progreso, se omite).
    public static int ComputeStreak(IEnumerable<CalendarDayDto> days, DateOnly today)
    {
        var streak = 0;
        foreach (var day in days.Where(d => d.Date <= today).OrderByDescending(d => d.Date))
        {
            switch (day.Status)
            {
                case DayStatus.Completed: streak++; break;
                case DayStatus.RestDay or DayStatus.NoActivities: break;
                case DayStatus.InProgress when day.Date == today: break;
                default: return streak;
            }
        }
        return streak;
    }
}

// ======================= Presentación =======================

// Textos, íconos y estilos. El backend solo devuelve estados; aquí se decide cómo se ven.
public static class Display
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("es-MX");

    public static readonly DayOfWeek[] WeekMondayFirst =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
         DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

    public static string DayName(DayOfWeek day) => Culture.TextInfo.ToTitleCase(Culture.DateTimeFormat.GetDayName(day));
    public static string DayShort(DayOfWeek day) => Culture.TextInfo.ToTitleCase(Culture.DateTimeFormat.GetAbbreviatedDayName(day).TrimEnd('.'));

    // Inicial de cada día para las "píldoras" (L M X J V S D).
    public static string DayLetter(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "L", DayOfWeek.Tuesday => "M", DayOfWeek.Wednesday => "X",
        DayOfWeek.Thursday => "J", DayOfWeek.Friday => "V", DayOfWeek.Saturday => "S", _ => "D"
    };

    public static string MonthTitle(int year, int month) =>
        Capitalize(new DateOnly(year, month, 1).ToString("MMMM yyyy", Culture));

    // "sábado 17 de octubre" => "Sábado 17 de octubre" (en español solo va mayúscula al inicio).
    public static string LongDate(DateOnly date) => Capitalize(date.ToString("dddd d 'de' MMMM", Culture));

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpper(s[0], Culture) + s[1..];

    public static string ShortDate(DateOnly date) => date.ToString("d MMM yyyy", Culture);

    public static string Days(IEnumerable<DayOfWeek> days) =>
        string.Join(", ", WeekMondayFirst.Where(days.Contains).Select(DayShort));

    public static string Time(TimeOnly time) => time.ToString("HH:mm");

    public static string Initials(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpper(p[0])));

    public static (string Label, string Css, string Icon) Of(DayStatus status) => status switch
    {
        DayStatus.Future => ("Próximo", "st-future", "bi-hourglass"),
        DayStatus.RestDay => ("Descanso", "st-rest", "bi-cup-hot"),
        DayStatus.NoActivities => ("Libre", "st-none", "bi-dash-circle"),
        DayStatus.Completed => ("Completado", "st-completed", "bi-check-circle-fill"),
        DayStatus.InProgress => ("En progreso", "st-progress", "bi-lightning-charge-fill"),
        DayStatus.Failed => ("Fallido", "st-failed", "bi-x-circle-fill"),
        DayStatus.Partial => ("Parcial", "st-partial", "bi-circle-half"),
        _ => (status.ToString(), "", "bi-circle")
    };

    public static (string Label, string Css, string Icon) Of(ActivityDayStatus status) => status switch
    {
        ActivityDayStatus.Scheduled => ("Programada", "chip-muted", "bi-calendar-event"),
        ActivityDayStatus.Pending => ("Pendiente", "chip-warning", "bi-clock"),
        ActivityDayStatus.Completed => ("Completada", "chip-success", "bi-check-lg"),
        ActivityDayStatus.NotCompleted => ("No completada", "chip-danger", "bi-x-lg"),
        ActivityDayStatus.Unrecorded => ("Sin registro", "chip-muted", "bi-question-lg"),
        _ => (status.ToString(), "chip-muted", "bi-circle")
    };
}

// Atajos para leer los claims de la cookie en vistas y controladores.
public static class UserClaims
{
    public static string DisplayName(this ClaimsPrincipal user) => user.Identity?.Name ?? "Usuario";
    public static string Email(this ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Email)?.Value ?? "";
    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(nameof(UserRole.Admin));
    public static int Id(this ClaimsPrincipal user) => int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
}
