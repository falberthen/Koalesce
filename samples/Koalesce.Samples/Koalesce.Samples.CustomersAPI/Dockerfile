FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy Directory.*.props for central package management
COPY Directory.Build.props Directory.Packages.props ./

# Copy project files
COPY samples/Koalesce.Samples/Koalesce.Samples.CustomersAPI/Koalesce.Samples.CustomersAPI.csproj samples/Koalesce.Samples/Koalesce.Samples.CustomersAPI/

# Restore
RUN dotnet restore "samples/Koalesce.Samples/Koalesce.Samples.CustomersAPI/Koalesce.Samples.CustomersAPI.csproj"

# Copy source
COPY samples/Koalesce.Samples/Koalesce.Samples.CustomersAPI/ samples/Koalesce.Samples/Koalesce.Samples.CustomersAPI/

# Build
RUN dotnet build "samples/Koalesce.Samples/Koalesce.Samples.CustomersAPI/Koalesce.Samples.CustomersAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "samples/Koalesce.Samples/Koalesce.Samples.CustomersAPI/Koalesce.Samples.CustomersAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Koalesce.Samples.CustomersAPI.dll"]
