FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Koalesce.Samples.InventoryAPI/Koalesce.Samples.InventoryAPI.csproj"
RUN dotnet build "Koalesce.Samples.InventoryAPI/Koalesce.Samples.InventoryAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Koalesce.Samples.InventoryAPI/Koalesce.Samples.InventoryAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Koalesce.Samples.InventoryAPI.dll"]
