FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update && apt-get install -y wget && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Edge.Service/Edge.Service.csproj", "Edge.Service/"]
COPY ["src/Shared.Models/Shared.Models.csproj", "Shared.Models/"]
COPY ["src/Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared.Infrastructure/"]

RUN dotnet restore "Edge.Service/Edge.Service.csproj"
COPY src/ .
WORKDIR "/src/Edge.Service"
RUN dotnet build "Edge.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Edge.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Edge.Service.dll"]