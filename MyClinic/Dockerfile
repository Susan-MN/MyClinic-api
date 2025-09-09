# Base image (runtime only)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

# Install ICU for globalization (fix CultureInfo errors)
RUN apt-get update && \
    apt-get install -y --no-install-recommends libicu72 && \
    rm -rf /var/lib/apt/lists/*

# Enable full globalization
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Build image (SDK needed)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files first for better caching
COPY ["MyClinic/MyClinic.csproj", "MyClinic/"]
COPY ["MyClinic.Infrastructure/MyClinic.Infrastructure.csproj", "MyClinic.Infrastructure/"]
COPY ["MyClinic.Application/MyClinic.Application.csproj", "MyClinic.Application/"]
COPY ["MyClinic.Domain/MyClinic.Domain.csproj", "MyClinic.Domain/"]

# Restore dependencies
RUN dotnet restore "MyClinic/MyClinic.csproj"

# Copy all source
COPY . .

# Build
WORKDIR "/src/MyClinic"
RUN dotnet build "MyClinic.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "MyClinic.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyClinic.dll"]
