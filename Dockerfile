FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

WORKDIR /app
COPY ./W3ChampionsPlayerService.sln ./

COPY ./W3ChampionsPlayerService/W3ChampionsPlayerService.csproj ./W3ChampionsPlayerService/W3ChampionsPlayerService.csproj
RUN dotnet restore ./W3ChampionsPlayerService/W3ChampionsPlayerService.csproj

COPY ./W3ChampionsPlayerService ./W3ChampionsPlayerService
RUN dotnet build ./W3ChampionsPlayerService/W3ChampionsPlayerService.csproj -c Release

RUN dotnet publish "./W3ChampionsPlayerService/W3ChampionsPlayerService.csproj" -c Release -o "../../app/out"

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

ENV ASPNETCORE_URLS http://*:80
EXPOSE 80

ENTRYPOINT dotnet W3ChampionsPlayerService.dll
