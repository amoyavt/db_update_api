FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update && apt-get install -y wget && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Edge.DbSync/Edge.DbSync.csproj", "Edge.DbSync/"]
COPY ["src/Edge.Data/Edge.Data.csproj", "Edge.Data/"]
COPY ["src/Shared.Models/Shared.Models.csproj", "Shared.Models/"]
COPY ["src/Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared.Infrastructure/"]

RUN dotnet restore "Edge.DbSync/Edge.DbSync.csproj"
COPY src/ .
WORKDIR "/src/Edge.DbSync"
RUN dotnet build "Edge.DbSync.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Edge.DbSync.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Edge.DbSync.dll"]