FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]
COPY ["OrderManagement.API/OrderManagement.API.csproj", "OrderManagement.API/"]
RUN dotnet restore "OrderManagement.API/OrderManagement.API.csproj"
COPY Shared.Contracts/ Shared.Contracts/
COPY OrderManagement.API/ OrderManagement.API/
WORKDIR "/src/OrderManagement.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderManagement.API.dll"]
