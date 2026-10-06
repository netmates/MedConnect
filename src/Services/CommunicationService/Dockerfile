FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Services/CommunicationService/CommunicationService.csproj src/Services/CommunicationService/
RUN dotnet restore src/Services/CommunicationService/CommunicationService.csproj

COPY src/Shared/ src/Shared/
COPY src/Services/CommunicationService/ src/Services/CommunicationService/

RUN dotnet publish src/Services/CommunicationService/CommunicationService.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "CommunicationService.dll"]
