FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Admitto.Worker/Admitto.Worker.csproj", "Admitto.Worker/"]
COPY ["Admitto.Domain/Admitto.Domain.csproj", "Admitto.Domain/"]
COPY ["Admitto.Infrastructure/Admitto.Infrastructure.csproj", "Admitto.Infrastructure/"]
COPY ["Admitto.ServiceDefaults/Admitto.ServiceDefaults.csproj", "Admitto.ServiceDefaults/"]
COPY ["Admitto.Application/Admitto.Application.csproj", "Admitto.Application/"]
RUN dotnet restore "Admitto.Worker/Admitto.Worker.csproj"
COPY . .
WORKDIR "/src/Admitto.Worker"
RUN dotnet build "Admitto.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Admitto.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Admitto.Worker.dll"]