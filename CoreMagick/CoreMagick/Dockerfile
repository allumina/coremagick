FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get clean -y
RUN apt-get update -y --allow-insecure-repositories --allow-unauthenticated
RUN apt-get install -y curl tar file xz-utils build-essential  
RUN curl -fLO https://github.com/ImageMagick/ImageMagick/archive/refs/tags/7.1.0-13.tar.gz && \
    tar -xzf 7.1.0-13.tar.gz && \
    cd ImageMagick-7.1.0-13 && \
    ls -la && \
    ./configure --prefix /usr/local && \
    make install && \
    cd .. && \
    rm -rf *7.1.0-13* \
RUN ldconfig /usr/local/lib

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
