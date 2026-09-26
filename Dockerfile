# docker build --platform linux/amd64 -t illusionist:local .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY NuGet.config Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/Illusionist.Core/Illusionist.Core.csproj src/Illusionist.Core/
COPY src/Illusionist.Service/Illusionist.Service.csproj src/Illusionist.Service/
RUN dotnet restore src/Illusionist.Service/Illusionist.Service.csproj -p:IllusionistUsePackageReferences=true
COPY src/Illusionist.Core/ src/Illusionist.Core/
COPY src/Illusionist.Service/ src/Illusionist.Service/
RUN dotnet publish src/Illusionist.Service/Illusionist.Service.csproj -c Release -o /app --no-restore \
    -p:IllusionistUsePackageReferences=true -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Illusionist.Service.dll"]
