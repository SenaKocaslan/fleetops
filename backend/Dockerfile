# ---- derleme ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Once yalnizca surum/kural dosyalari: bunlar nadiren degisir.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/ src/

# Not: csproj'lari tek tek kopyalayip once restore etmek katman onbellegini
# daha iyi kullanirdi. 9 kaynak projesi icin 9 satirlik ve her yeni projede
# guncellenmesi gereken bir liste demek; bu boyuttaki bir projede kazanc
# (birkac saniye) bakim maliyetini karsilamiyor.
RUN dotnet publish src/FleetOps.Api/FleetOps.Api.csproj \
      -c Release -o /app --no-self-contained

# ---- calisma ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Saglik kontrolu icin; imaj icinde HTTP istegi atacak bir arac yok.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

# Imajlarda tanimli olan ayricaliksiz kullanici. Root olarak calisan bir
# konteynerde herhangi bir RCE dogrudan konteyner root'u demek.
USER $APP_UID

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

HEALTHCHECK --interval=10s --timeout=3s --start-period=20s --retries=6 \
  CMD curl -fsS http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "FleetOps.Api.dll"]
