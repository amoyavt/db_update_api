FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8082

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Edge.ScheduleService/Edge.ScheduleService.csproj", "src/Edge.ScheduleService/"]
COPY ["src/Edge.Data/Edge.Data.csproj", "src/Edge.Data/"]
COPY ["src/Shared.Models/Shared.Models.csproj", "src/Shared.Models/"]
COPY ["src/Shared.Infrastructure/Shared.Infrastructure.csproj", "src/Shared.Infrastructure/"]
RUN dotnet restore "src/Edge.ScheduleService/Edge.ScheduleService.csproj"
COPY . .
WORKDIR "/src/src/Edge.ScheduleService"
RUN dotnet build "Edge.ScheduleService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Edge.ScheduleService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Edge.ScheduleService.dll"]