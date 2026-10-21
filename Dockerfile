FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY WebsiteAPI.csproj ./
RUN dotnet restore WebsiteAPI.csproj
COPY . .
RUN dotnet publish WebsiteAPI.csproj --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish ./
RUN mkdir -p /app/Uploads && chown -R "$APP_UID:$APP_UID" /app/Uploads
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebsiteAPI.dll"]
