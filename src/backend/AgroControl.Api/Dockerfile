FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "AgroControl.sln"
RUN dotnet publish "src/backend/AgroControl.Api/AgroControl.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
HEALTHCHECK --interval=10s --timeout=3s --retries=5 CMD curl --fail --silent http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "AgroControl.Api.dll"]
