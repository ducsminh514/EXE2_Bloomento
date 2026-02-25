# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Backend/ADHDChecklist.API/ADHDChecklist.API.csproj", "Backend/ADHDChecklist.API/"]
# Restore dependencies
RUN dotnet restore "Backend/ADHDChecklist.API/ADHDChecklist.API.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/Backend/ADHDChecklist.API"
# Build the project
RUN dotnet build "ADHDChecklist.API.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "ADHDChecklist.API.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose port (Railway dynamically assigns PORT, but 8080/80 is default for container)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ADHDChecklist.API.dll"]
