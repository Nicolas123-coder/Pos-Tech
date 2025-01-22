FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

COPY ["Fase02/API/API.csproj", "Fase02/API/"]
COPY ["Fase02/Application/Application.csproj", "Fase02/Application/"]
COPY ["Fase02/Domain/Domain.csproj", "Fase02/Domain/"]
COPY ["Fase02/Infrastructure/Infrastructure.csproj", "Fase02/Infrastructure/"]
COPY ["Fase02/Tests/Tests.csproj", "Fase02/Tests/"]

WORKDIR "/src/Fase02"
RUN dotnet restore "API/API.csproj"

COPY ["Fase02/", "."]

RUN dotnet ef migrations add InitialCreate --project Infrastructure --startup-project API

RUN dotnet build "API/API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "API/API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]