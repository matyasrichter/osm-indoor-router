ARG BASE_IMAGE=mcr.microsoft.com/dotnet/aspnet:7.0

FROM $BASE_IMAGE AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM $BASE_IMAGE AS build
WORKDIR /src
COPY ["src/Application/Application.csproj", "src/Application/"]
RUN dotnet restore "src/Application/Application.csproj"
COPY . .
WORKDIR "/src/src/Application"
RUN dotnet build "Application.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Application.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Application.dll"]
