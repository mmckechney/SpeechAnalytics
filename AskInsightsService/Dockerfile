# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AskInsightsService/AskInsightsService.csproj", "AskInsightsService/"]
COPY ["SpeechAnalyticsLibrary/SpeechAnalyticsLibrary.csproj", "SpeechAnalyticsLibrary/"]
RUN dotnet restore "AskInsightsService/AskInsightsService.csproj"
COPY . .
RUN dotnet publish "AskInsightsService/AskInsightsService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AskInsightsService.dll"]
