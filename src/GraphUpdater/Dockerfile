FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore "src/GraphUpdater/GraphUpdater.csproj"
# Build and publish a release
RUN dotnet publish -c Release -o out "src/GraphUpdater/GraphUpdater.csproj"

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:7.0
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "GraphUpdater.dll"]
