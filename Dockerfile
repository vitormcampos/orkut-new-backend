# Build from the repository root: docker build -t orkut-new-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first to keep NuGet restore cached when source changes.
COPY App.API/OrkutNew.csproj App.API/
COPY App.IOC/App.IOC.csproj App.IOC/
COPY App.Infrastructure/App.Infrastructure.csproj App.Infrastructure/
COPY App.Application/App.Application.csproj App.Application/
COPY App.Domain/App.Domain.csproj App.Domain/
RUN dotnet restore App.API/OrkutNew.csproj

COPY App.API/ App.API/
COPY App.IOC/ App.IOC/
COPY App.Infrastructure/ App.Infrastructure/
COPY App.Application/ App.Application/
COPY App.Domain/ App.Domain/
RUN dotnet publish App.API/OrkutNew.csproj -c Release -o /app/publish --no-restore \
    && rm -f /app/publish/appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "OrkutNew.dll"]
