FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/GraphUpdater/GraphUpdater.csproj", "src/GraphUpdater/"]
RUN dotnet restore "src/GraphUpdater/GraphUpdater.csproj"
COPY . .
WORKDIR "/src/src/GraphUpdater"
RUN dotnet build "GraphUpdater.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GraphUpdater.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GraphUpdater.dll"]
