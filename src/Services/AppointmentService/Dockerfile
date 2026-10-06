FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Services/AppointmentService/AppointmentService.csproj src/Services/AppointmentService/
RUN dotnet restore src/Services/AppointmentService/AppointmentService.csproj

COPY src/Shared/ src/Shared/
COPY src/Services/AppointmentService/ src/Services/AppointmentService/

RUN dotnet publish src/Services/AppointmentService/AppointmentService.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "AppointmentService.dll"]
