FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]
COPY ["ShippingService/ShippingService.csproj", "ShippingService/"]
RUN dotnet restore "ShippingService/ShippingService.csproj"
COPY Shared.Contracts/ Shared.Contracts/
COPY ShippingService/ ShippingService/
WORKDIR "/src/ShippingService"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShippingService.dll"]
