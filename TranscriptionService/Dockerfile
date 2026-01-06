# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TranscriptionService/TranscriptionService.csproj", "TranscriptionService/"]
COPY ["SpeechAnalyticsLibrary/SpeechAnalyticsLibrary.csproj", "SpeechAnalyticsLibrary/"]
RUN dotnet restore "TranscriptionService/TranscriptionService.csproj"
COPY . .
RUN dotnet publish "TranscriptionService/TranscriptionService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TranscriptionService.dll"]
