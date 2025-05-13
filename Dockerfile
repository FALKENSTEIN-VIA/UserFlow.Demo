FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["UserFlow.API/UserFlow.API.csproj", "UserFlow.API/"]
RUN dotnet restore "UserFlow.API/UserFlow.API.csproj"
COPY ./UserFlow.API/. ./UserFlow.API/
WORKDIR "/src/UserFlow.API"
RUN dotnet publish "UserFlow.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserFlow.API.dll"]
