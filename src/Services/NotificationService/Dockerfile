FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Services/NotificationService/NotificationService.csproj src/Services/NotificationService/
RUN dotnet restore src/Services/NotificationService/NotificationService.csproj

COPY src/Shared/ src/Shared/
COPY src/Services/NotificationService/ src/Services/NotificationService/

RUN dotnet publish src/Services/NotificationService/NotificationService.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "NotificationService.dll"]
