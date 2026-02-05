FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy Directory.*.props for central package management
COPY Directory.Build.props Directory.Packages.props ./

# Copy project files
COPY src/Koalesce.Core/Koalesce.Core.csproj src/Koalesce.Core/
COPY src/Koalesce/Koalesce.csproj src/Koalesce/
COPY samples/Koalesce.Samples/Koalesce.Samples.Swagger/Koalesce.Samples.Swagger.csproj samples/Koalesce.Samples/Koalesce.Samples.Swagger/

# Restore
RUN dotnet restore "samples/Koalesce.Samples/Koalesce.Samples.Swagger/Koalesce.Samples.Swagger.csproj"

# Copy source
COPY src/Koalesce.Core/ src/Koalesce.Core/
COPY src/Koalesce/ src/Koalesce/
COPY samples/Koalesce.Samples/Koalesce.Samples.Swagger/ samples/Koalesce.Samples/Koalesce.Samples.Swagger/
COPY samples/Koalesce.Samples/products-api.json samples/Koalesce.Samples/
COPY samples/Koalesce.Samples/products-api.Docker.json samples/Koalesce.Samples/

# Build
RUN dotnet build "samples/Koalesce.Samples/Koalesce.Samples.Swagger/Koalesce.Samples.Swagger.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "samples/Koalesce.Samples/Koalesce.Samples.Swagger/Koalesce.Samples.Swagger.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Koalesce.Samples.Swagger.dll"]
