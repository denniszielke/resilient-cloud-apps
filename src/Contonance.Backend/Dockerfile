FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /source

# copy csproj and restore as distinct layers

COPY ./Contonance.Shared ./Contonance.Shared/
COPY ./Contonance.Backend ./Contonance.Backend/
WORKDIR /source/Contonance.Backend/
RUN dotnet restore

RUN dotnet publish -c release -o /app --self-contained false --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "Contonance.Backend.dll"]