# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY apps/v2-backend/EnterpriseAgentOs.Api.csproj ./
RUN dotnet restore EnterpriseAgentOs.Api.csproj
COPY apps/v2-backend/. ./
RUN dotnet publish EnterpriseAgentOs.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8000
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish ./
EXPOSE 8000
ENTRYPOINT ["dotnet", "EnterpriseAgentOs.Api.dll"]
