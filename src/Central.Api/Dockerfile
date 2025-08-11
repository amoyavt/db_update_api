FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update && apt-get install -y wget && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Central.Api/Central.Api.csproj", "Central.Api/"]
COPY ["src/Shared.Models/Shared.Models.csproj", "Shared.Models/"]
COPY ["src/Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared.Infrastructure/"]

RUN dotnet restore "Central.Api/Central.Api.csproj"
COPY src/ .
WORKDIR "/src/Central.Api"
RUN dotnet build "Central.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Central.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Central.Api.dll"]