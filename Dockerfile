# syntax=docker/dockerfile:1
# =====================================================================
# TaskFlow.Web (ASP.NET Core MVC) - imagen multi-etapa
# =====================================================================

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TaskFlow.Web/TaskFlow.Web.csproj TaskFlow.Web/
RUN dotnet restore TaskFlow.Web/TaskFlow.Web.csproj

COPY TaskFlow.Web/ TaskFlow.Web/
RUN dotnet publish TaskFlow.Web/TaskFlow.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- final ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Data Protection cifra la cookie de sesión (que lleva el JWT). Sus llaves se
# guardan en $HOME/.aspnet/DataProtection-Keys; ahí se monta un volumen para
# que reiniciar el contenedor NO cierre la sesión de todos. Se crea la carpeta
# con dueño "app" para que el volumen herede esos permisos.
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
 && chown -R $APP_UID /home/app/.aspnet

USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskFlow.Web.dll"]
