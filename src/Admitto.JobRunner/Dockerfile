FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Admitto.JobRunner/Admitto.JobRunner.csproj", "src/Admitto.JobRunner/"]
COPY ["src/Admitto.Domain/Admitto.Domain.csproj", "src/Admitto.Domain/"]
COPY ["src/Admitto.Infrastructure/Admitto.Infrastructure.csproj", "src/Admitto.Infrastructure/"]
COPY ["src/Admitto.ServiceDefaults/Admitto.ServiceDefaults.csproj", "src/Admitto.ServiceDefaults/"]
COPY ["src/Admitto.Application/Admitto.Application.csproj", "src/Admitto.Application/"]
RUN dotnet restore "src/Admitto.JobRunner/Admitto.JobRunner.csproj"
COPY . .
WORKDIR "/src/src/Admitto.JobRunner"
RUN dotnet build "Admitto.JobRunner.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Admitto.JobRunner.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Admitto.JobRunner.dll"]