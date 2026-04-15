FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Api/PostalDeliverySystem.Api.csproj", "src/Api/"]
COPY ["src/Application/PostalDeliverySystem.Application.csproj", "src/Application/"]
COPY ["src/Domain/PostalDeliverySystem.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/PostalDeliverySystem.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Shared.Contracts/PostalDeliverySystem.Shared.Contracts.csproj", "src/Shared.Contracts/"]
RUN dotnet restore "src/Api/PostalDeliverySystem.Api.csproj"
COPY . .
RUN dotnet publish "src/Api/PostalDeliverySystem.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PostalDeliverySystem.Api.dll"]
