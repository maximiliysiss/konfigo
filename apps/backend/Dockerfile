FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG KONFIGO_VERSION=0.0.2

WORKDIR /src

COPY . .

RUN dotnet publish src/Konfigo/Konfigo.csproj -c Release -o /app/publish /p:Version=$KONFIGO_VERSION /p:PackageVersion=$KONFIGO_VERSION /p:AssemblyVersion=$KONFIGO_VERSION /p:FileVersion=$KONFIGO_VERSION /p:InformationalVersion=$KONFIGO_VERSION

FROM mcr.microsoft.com/dotnet/aspnet:10.0

ARG KONFIGO_VERSION=0.0.2
LABEL org.opencontainers.image.version=$KONFIGO_VERSION
ENV KONFIGO_VERSION=$KONFIGO_VERSION

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "Konfigo.dll"]
