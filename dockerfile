FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY NetsBrokerIntegration.NetCore/NetsBrokerIntegration.NetCore.csproj ./NetsBrokerIntegration.NetCore/NetsBrokerIntegration.NetCore.csproj

WORKDIR /app
RUN dotnet restore NetsBrokerIntegration.NetCore/NetsBrokerIntegration.NetCore.csproj

# copy everything else and build app
COPY NetsBrokerIntegration.NetCore/ ./NetsBrokerIntegration.NetCore/
WORKDIR /app/NetsBrokerIntegration.NetCore
RUN dotnet publish -c Release -o out

WORKDIR /app/NetsBrokerIntegration.NetCore/out

# FROM registry.redhat.io/dotnet/dotnet-31-runtime-rhel7:3.1-2 AS runtime
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/NetsBrokerIntegration.NetCore/out ./

ENV ASPNETCORE_URLS=http://+:5015

ENTRYPOINT ["dotnet", "NetsBrokerIntegration.NetCore.dll"]