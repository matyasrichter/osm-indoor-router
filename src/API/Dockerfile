ARG BASE_IMAGE=mcr.microsoft.com/dotnet/aspnet:7.0

FROM $BASE_IMAGE AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM $BASE_IMAGE AS build
WORKDIR /src
COPY ["src/API/API.csproj", "src/API/"]
RUN dotnet restore "src/API/API.csproj"
COPY . .
WORKDIR "/src/src/API"
RUN dotnet build "API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]
