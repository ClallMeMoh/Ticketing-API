FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Ticketing.sln .
COPY src/Ticketing.Domain/Ticketing.Domain.csproj src/Ticketing.Domain/
COPY src/Ticketing.Application/Ticketing.Application.csproj src/Ticketing.Application/
COPY src/Ticketing.Infrastructure/Ticketing.Infrastructure.csproj src/Ticketing.Infrastructure/
COPY src/Ticketing.API/Ticketing.API.csproj src/Ticketing.API/
COPY tests/Ticketing.Tests/Ticketing.Tests.csproj tests/Ticketing.Tests/

RUN dotnet restore

COPY . .
RUN dotnet publish src/Ticketing.API/Ticketing.API.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ticketing.API.dll"]
