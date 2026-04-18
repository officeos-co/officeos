# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY apps/backend/EnterpriseAgentOs.sln ./
COPY apps/backend/EnterpriseAgentOs.Api.csproj ./
COPY apps/backend/src/EnterpriseAgentOs.Domain/EnterpriseAgentOs.Domain.csproj src/EnterpriseAgentOs.Domain/
COPY apps/backend/src/EnterpriseAgentOs.Application/EnterpriseAgentOs.Application.csproj src/EnterpriseAgentOs.Application/
COPY apps/backend/src/EnterpriseAgentOs.Infrastructure/EnterpriseAgentOs.Infrastructure.csproj src/EnterpriseAgentOs.Infrastructure/
RUN dotnet restore EnterpriseAgentOs.sln
COPY apps/backend/. ./
RUN dotnet publish EnterpriseAgentOs.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8000
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish ./
EXPOSE 8000
ENTRYPOINT ["dotnet", "EnterpriseAgentOs.Api.dll"]
