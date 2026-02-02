FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Koalesce.Samples.ProductsAPI/Koalesce.Samples.ProductsAPI.csproj"
RUN dotnet build "Koalesce.Samples.ProductsAPI/Koalesce.Samples.ProductsAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Koalesce.Samples.ProductsAPI/Koalesce.Samples.ProductsAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Koalesce.Samples.ProductsAPI.dll"]
