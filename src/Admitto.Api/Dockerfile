FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Admitto.Api/Admitto.Api.csproj", "Admitto.Api/"]
COPY ["Admitto.Application/Admitto.Application.csproj", "Admitto.Application/"]
COPY ["Admitto.Domain/Admitto.Domain.csproj", "Admitto.Domain/"]
COPY ["Admitto.Infrastructure/Admitto.Infrastructure.csproj", "Admitto.Infrastructure/"]
COPY ["Admitto.ServiceDefaults/Admitto.ServiceDefaults.csproj", "Admitto.ServiceDefaults/"]
RUN dotnet restore "Admitto.Api/Admitto.Api.csproj"
COPY . .
WORKDIR "/src/Admitto.Api"
RUN dotnet build "Admitto.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Admitto.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Admitto.Api.dll"]