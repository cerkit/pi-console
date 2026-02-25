# Stage 1: Build and Restore
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project files first to cache the NuGet restore layer
COPY ["pi-functions/pi-functions.csproj", "pi-functions/"]
COPY ["Pi.Shared/Pi.Shared.csproj", "Pi.Shared/"]

# Restore dependencies for the API
RUN dotnet restore "pi-functions/pi-functions.csproj"

# Copy the rest of the workspace source code
COPY . .

# Build the API
WORKDIR "/src/pi-functions"
RUN dotnet build "pi-functions.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "pi-functions.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "pi-functions.dll"]