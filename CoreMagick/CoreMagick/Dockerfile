FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["CoreMagick/CoreMagick.csproj", "CoreMagick/"]
RUN dotnet restore "CoreMagick/CoreMagick.csproj"
COPY . .
WORKDIR "/src/CoreMagick"
RUN dotnet build "CoreMagick.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CoreMagick.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoreMagick.dll"]
