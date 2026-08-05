# Builds the Angular SPA and publishes it into the API's wwwroot, so the
# API serves both the app and /api/* from a single container/origin.

FROM node:22-alpine AS client-build
WORKDIR /client
COPY ClientApp/package*.json ./
RUN npm ci
COPY ClientApp/ ./
RUN npm run build -- --configuration production

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src
COPY Api/Api.csproj Api/
RUN dotnet restore Api/Api.csproj
COPY Api/ Api/
RUN dotnet publish Api/Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=api-build /app/publish .
COPY --from=client-build /client/dist/ClientApp/browser ./wwwroot
ENTRYPOINT ["dotnet", "Api.dll"]
