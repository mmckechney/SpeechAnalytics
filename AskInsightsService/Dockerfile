FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore AskInsightsService/AskInsightsService.csproj
RUN dotnet publish AskInsightsService/AskInsightsService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AskInsightsService.dll"]
