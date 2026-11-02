FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/LeadsTracker.Api/LeadsTracker.Api.csproj src/LeadsTracker.Api/
RUN dotnet restore src/LeadsTracker.Api/LeadsTracker.Api.csproj
COPY src/LeadsTracker.Api/ src/LeadsTracker.Api/
RUN dotnet publish src/LeadsTracker.Api/LeadsTracker.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "LeadsTracker.Api.dll"]
