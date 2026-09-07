# ---- derleme ----
FROM node:24-alpine AS build
WORKDIR /src

# Bagimlilklar kaynak koddan once: kod degistiginde npm ci tekrar calismaz.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Varsayilan yapilandirma "production": environment.ts devrede, yani apiUrl
# "/api" gibi GORELI. Adres imaja gomulmez, nginx ayni origin'den vekiller.
RUN npm run build

# ---- calisma ----
FROM nginx:1.29-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/fleetops-web/browser /usr/share/nginx/html

EXPOSE 80

# 127.0.0.1, "localhost" DEGIL: konteynerde localhost once ::1'e cozuluyor,
# nginx ise "listen 80" ile yalnizca IPv4'u dinliyor -> baglanti reddedilir.
HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=5 \
  CMD wget -qO- http://127.0.0.1/ >/dev/null || exit 1
