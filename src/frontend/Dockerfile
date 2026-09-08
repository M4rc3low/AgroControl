FROM node:24-alpine AS build
WORKDIR /app
ARG VITE_MAPLIBRE_SCRIPT_URL=""
ARG VITE_MAPLIBRE_CSS_URL=""
ARG VITE_MAP_STYLE_URL=""
ENV VITE_MAPLIBRE_SCRIPT_URL=${VITE_MAPLIBRE_SCRIPT_URL}
ENV VITE_MAPLIBRE_CSS_URL=${VITE_MAPLIBRE_CSS_URL}
ENV VITE_MAP_STYLE_URL=${VITE_MAP_STYLE_URL}
COPY src/frontend/package.json src/frontend/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY src/frontend/ .
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.29.4-alpine
ENV API_UPSTREAM=api:8080
COPY src/frontend/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --retries=5 CMD wget -q -O /dev/null http://127.0.0.1:8080/ || exit 1
