FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY . .
WORKDIR /source/EnterpriseWarehouse.Backend/
RUN dotnet restore

RUN dotnet publish -c release -o /app --self-contained false --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "EnterpriseWarehouse.Backend.dll"]