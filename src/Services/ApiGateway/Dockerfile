FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Services/ApiGateway/ApiGateway.csproj src/Services/ApiGateway/
RUN dotnet restore src/Services/ApiGateway/ApiGateway.csproj

COPY src/Shared/ src/Shared/
COPY src/Services/ApiGateway/ src/Services/ApiGateway/

RUN dotnet publish src/Services/ApiGateway/ApiGateway.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "ApiGateway.dll"]
