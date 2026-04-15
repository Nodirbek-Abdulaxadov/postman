FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AdminWeb/PostalDeliverySystem.AdminWeb.csproj", "src/AdminWeb/"]
COPY ["src/Shared.Contracts/PostalDeliverySystem.Shared.Contracts.csproj", "src/Shared.Contracts/"]
COPY ["src/Shared.Client/PostalDeliverySystem.Shared.Client.csproj", "src/Shared.Client/"]
RUN dotnet restore "src/AdminWeb/PostalDeliverySystem.AdminWeb.csproj"
COPY . .
RUN dotnet publish "src/AdminWeb/PostalDeliverySystem.AdminWeb.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PostalDeliverySystem.AdminWeb.dll"]
