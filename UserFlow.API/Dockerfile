FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["UserFlow.Demo/UserFlow.Demo.csproj", "UserFlow.Demo/"]
RUN dotnet restore "UserFlow.Demo/UserFlow.Demo.csproj"
COPY ./UserFlow.Demo/. ./UserFlow.Demo/
WORKDIR "/src/UserFlow.Demo"
RUN dotnet publish "UserFlow.Demo.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserFlow.Demo.dll"]
