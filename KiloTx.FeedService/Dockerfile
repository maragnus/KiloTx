FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["KiloTx.ArrlBulletins/KiloTx.ArrlBulletins.csproj", "KiloTx.ArrlBulletins/"]
RUN dotnet restore "KiloTx.ArrlBulletins/KiloTx.ArrlBulletins.csproj"
COPY . .
WORKDIR "/src/KiloTx.ArrlBulletins"
RUN dotnet build "KiloTx.ArrlBulletins.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KiloTx.ArrlBulletins.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KiloTx.ArrlBulletins.dll"]
