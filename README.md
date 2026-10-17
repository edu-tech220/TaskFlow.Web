# TaskFlow.Web

Frontend de **TaskFlow** en ASP.NET Core MVC (Razor + Bootstrap). Es un cliente independiente: no comparte código con el backend y solo se comunica por HTTP con la API REST.

Backend: [edu-tech220/TaskFlow](https://github.com/edu-tech220/TaskFlow)

## Qué incluye

- **Calendario mensual** con un color por estado del día (Completado, Parcial, Fallido, En progreso, Descanso, Próximo, Sin actividades) y el contador de descansos usados contra el límite.
- **Detalle del día**: marcar actividades como completadas (con hora real y nota) o no completadas, borrar un registro, y marcar o quitar el día como descanso.
- **Actividades**: alta, edición, cambio de horario versionado (aplica desde mañana) con historial, desactivar/reactivar y eliminar.
- **Límite mensual de descansos** editable desde el calendario.

Los colores y textos se deciden aquí: la API solo devuelve estados.

## Requisitos

- .NET SDK 10
- La API de TaskFlow corriendo (por defecto en `https://localhost:7256`, perfil `https` del backend)

## Puesta en marcha

```powershell
cd TaskFlow.Web
dotnet run --launch-profile https   # https://localhost:7100
```

La URL de la API se configura en `TaskFlow.Web/appsettings.json`:

```json
"TaskFlowApi": { "BaseUrl": "https://localhost:7256" }
```

También se puede sobrescribir con la variable de entorno `TaskFlowApi__BaseUrl`.

## Estructura

```
TaskFlow.Web/
  Controllers/   CalendarController (calendario, día, registros, descansos, límite)
                 ActivitiesController (CRUD de actividades)
  Models/        ApiModels.cs: copia del contrato JSON de la API
                 ViewModels.cs: formularios y textos/colores de presentación
  Services/      TaskFlowApiClient: HttpClient tipado; convierte ProblemDetails en mensajes
  Views/         Razor + Bootstrap
  wwwroot/css/   site.css: colores por estado
```

## Notas de diseño

- **Patrón PRG** (Post-Redirect-Get): cada formulario termina en un redirect y el resultado se muestra con TempData, así recargar la página no reenvía el formulario.
- **Antiforgery global**: todos los POST exigen el token que agregan los tag helpers de `<form>`.
- **Validación doble**: el formulario valida para dar respuesta rápida, pero la API es la fuente de verdad y sus errores (400/409) se muestran tal cual.
- **Cultura es-MX** para nombres de días y meses.
