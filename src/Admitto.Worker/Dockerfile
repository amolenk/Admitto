FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Admitto.Worker/Admitto.Worker.csproj", "src/Admitto.Worker/"]
COPY ["src/Admitto.Domain/Admitto.Domain.csproj", "src/Admitto.Domain/"]
COPY ["src/Admitto.Infrastructure/Admitto.Infrastructure.csproj", "src/Admitto.Infrastructure/"]
COPY ["src/Admitto.ServiceDefaults/Admitto.ServiceDefaults.csproj", "src/Admitto.ServiceDefaults/"]
COPY ["src/Admitto.Application/Admitto.Application.csproj", "src/Admitto.Application/"]
RUN dotnet restore "src/Admitto.Worker/Admitto.Worker.csproj"
COPY . .
WORKDIR "/src/src/Admitto.Worker"
RUN dotnet build "Admitto.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Admitto.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Admitto.Worker.dll"]