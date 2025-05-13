# ─────────build stage ─────────
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# copy just the csproj + sln for better caching
COPY geotagger-backend.csproj .
COPY geotagger-backend.sln .

RUN dotnet restore "geotagger-backend.csproj"

# now copy *everything* and publish
COPY . .
RUN dotnet publish "geotagger-backend.csproj" -c Release -o /app/publish

# ─────────runtime stage ─────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "geotagger-backend.dll"]
