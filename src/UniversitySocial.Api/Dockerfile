FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/UniversitySocial.Api/UniversitySocial.Api.csproj", "src/UniversitySocial.Api/"]
RUN dotnet restore "src/UniversitySocial.Api/UniversitySocial.Api.csproj"
COPY . .
RUN dotnet publish "src/UniversitySocial.Api/UniversitySocial.Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
COPY --from=build --chown=app:app /app/publish .
USER app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "UniversitySocial.Api.dll"]
