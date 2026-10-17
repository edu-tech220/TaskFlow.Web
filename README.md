# TaskFlow.Web

Frontend de **TaskFlow** en ASP.NET Core MVC (Razor + Bootstrap 5.3). Es un cliente independiente: no comparte código con el backend y solo se comunica por HTTP con la API REST.

Backend: [edu-tech220/TaskFlow](https://github.com/edu-tech220/TaskFlow)

## Qué incluye

- **Login y registro** con diseño de dos paneles. El primer usuario registrado en la API es administrador.
- **Sidebar** fija en escritorio y deslizable en móvil, con el usuario actual y cierre de sesión.
- **Modo claro / oscuro**: respeta el del sistema y recuerda la elección.
- **Dashboard**: saludo, progreso de hoy (anillo), racha de días completos, cumplimiento del mes, actividades activas, descansos usados, actividades de hoy con acciones rápidas y gráfica de los últimos 7 días.
- **Calendario mensual**: un color por estado del día, barra de avance por día, descansos marcados, resumen del mes y límite de descansos.
- **Detalle del día**: marcar como completada o no completada en un clic; hora real y nota opcionales; deshacer un registro; marcar o quitar descanso.
- **Actividades** en tarjetas con sus días (L M X J V S D); alta y edición con selector de días y atajos; cambio de horario versionado con historial; desactivar/reactivar y eliminar.
- **Mi perfil**: cambiar nombre y contraseña.
- **Usuarios** (solo administradores): lista con métricas, dar o quitar rol de admin, desactivar o reactivar cuentas.

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

También se puede sobrescribir con la variable de entorno `TaskFlowApi__BaseUrl`. Si corres la API con IIS Express, usa su puerto (por ejemplo `https://localhost:44325/`).

## Cómo funciona la sesión

1. El login manda correo y contraseña a `POST /api/auth/login` y recibe un **JWT**.
2. El frontend crea su propia **cookie de sesión** (`HttpOnly`, `SameSite=Lax`, cifrada con Data Protection) y guarda el JWT **dentro** de ella. El navegador nunca ve el token.
3. `BearerTokenHandler` (un `DelegatingHandler` del `HttpClient`) agrega `Authorization: Bearer <token>` a cada llamada a la API.
4. Si la API responde **401** (token vencido o usuario desactivado), `ApiSessionFilter` cierra la sesión y manda al login con un mensaje.
5. La cookie vence a la misma hora que el JWT.

Toda página requiere sesión (filtro global `AuthorizeFilter`) salvo login y registro. La sección de usuarios exige rol `Admin`, y la API lo vuelve a verificar.

## Estructura

```
TaskFlow.Web/
  Controllers/   HomeController (dashboard), CalendarController (calendario, día, registros, descansos),
                 ActivitiesController, AccountController (login, registro, perfil), AdminController (usuarios)
  Models/        ApiModels.cs: copia del contrato JSON de la API
                 ViewModels.cs: formularios, métricas del dashboard y textos/colores de presentación
  Services/      TaskFlowApiClient (HttpClient tipado), BearerTokenHandler, ApiSessionFilter
  Views/         Razor; Shared/_Layout (sidebar) y Shared/_AuthLayout (login/registro)
  wwwroot/css/   site.css: sistema visual (tokens claro/oscuro, estados, calendario, tarjetas)
  wwwroot/js/    site.js: cambio de tema y confirmaciones
```

## Notas de diseño

- **Patrón PRG** (Post-Redirect-Get): cada formulario termina en un redirect y el resultado se muestra con TempData, así recargar la página no reenvía el formulario.
- **Antiforgery global**: todos los POST exigen el token que agregan los tag helpers de `<form>`.
- **Validación doble**: el formulario valida para dar respuesta rápida, pero la API es la fuente de verdad y sus errores (400/401/409) se muestran tal cual.
- **Sin dependencias de build**: Bootstrap viene en `wwwroot/lib`; la fuente Inter y Bootstrap Icons se cargan desde CDN.
- **Cultura es-MX** para nombres de días y meses.
