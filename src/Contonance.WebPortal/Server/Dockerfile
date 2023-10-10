FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /source

COPY . .

WORKDIR /source/Contonance.WebPortal/Server
RUN dotnet restore
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "Contonance.WebPortal.Server.dll"]

