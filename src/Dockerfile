FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /source

COPY ./Contonance.Shared ./Contonance.Shared/

COPY ./Contonance.WebPortal ./Contonance.WebPortal/
WORKDIR /source/Contonance.WebPortal/Server
RUN dotnet restore
RUN dotnet publish -c release -o /app --self-contained true --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "Contonance.WebPortal.dll"]

