FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["MyClinic/MyClinic.csproj", "MyClinic/"]
COPY ["MyClinic.Application/MyClinic.Application.csproj", "MyClinic.Application/"]
COPY ["MyClinic.Domain/MyClinic.Domain.csproj", "MyClinic.Domain/"]
COPY ["MyClinic.Infrastructure/MyClinic.Infrastructure.csproj", "MyClinic.Infrastructure/"]
RUN dotnet restore "MyClinic/MyClinic.csproj"
COPY . .
WORKDIR "/src/MyClinic"
RUN dotnet build "MyClinic.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MyClinic.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyClinic.dll"]
