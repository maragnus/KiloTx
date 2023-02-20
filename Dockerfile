FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /src
COPY ["KiloTx.FeedService/KiloTx.FeedService.csproj", "KiloTx.FeedService/"]
RUN dotnet restore "KiloTx.FeedService/KiloTx.FeedService.csproj"
COPY . .

WORKDIR "/src/KiloTx.FeedService"
RUN dotnet build "KiloTx.FeedService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KiloTx.FeedService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KiloTx.FeedService.dll"]
