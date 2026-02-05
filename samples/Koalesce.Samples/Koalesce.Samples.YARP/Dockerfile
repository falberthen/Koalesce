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
COPY samples/Koalesce.Samples/Koalesce.Samples.YARP/Koalesce.Samples.YARP.csproj samples/Koalesce.Samples/Koalesce.Samples.YARP/

# Restore
RUN dotnet restore "samples/Koalesce.Samples/Koalesce.Samples.YARP/Koalesce.Samples.YARP.csproj"

# Copy source
COPY src/ src/
COPY samples/Koalesce.Samples/Koalesce.Samples.YARP/ samples/Koalesce.Samples/Koalesce.Samples.YARP/

# Build
RUN dotnet build "samples/Koalesce.Samples/Koalesce.Samples.YARP/Koalesce.Samples.YARP.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "samples/Koalesce.Samples/Koalesce.Samples.YARP/Koalesce.Samples.YARP.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Koalesce.Samples.YARP.dll"]
