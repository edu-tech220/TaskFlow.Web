using System.Net;
using System.Net.Http.Headers;

namespace TaskFlow.Web.Services;

// DelegatingHandler = middleware del HttpClient: se ejecuta en cada petición saliente.
// Toma el JWT guardado en la cookie del usuario y lo agrega como Bearer.
public sealed class BearerTokenHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    public const string TokenClaim = "access_token";
    public const string UnauthorizedFlag = "TaskFlow.ApiUnauthorized";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var context = accessor.HttpContext;
        var token = context?.User.FindFirst(TokenClaim)?.Value;

        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, ct);

        // Teníamos token y la API lo rechazó: venció o el usuario fue desactivado.
        // Se marca la petición; ApiSessionFilter cierra la sesión y redirige al login.
        if (token is not null && response.StatusCode == HttpStatusCode.Unauthorized && context is not null)
            context.Items[UnauthorizedFlag] = true;

        return response;
    }
}
