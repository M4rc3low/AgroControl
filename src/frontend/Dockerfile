FROM node:24-alpine AS build
WORKDIR /app
COPY src/frontend/package.json ./package.json
RUN npm install --no-audit --no-fund
COPY src/frontend/ .
RUN npm run build

FROM nginx:1.29-alpine
COPY src/frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
HEALTHCHECK --interval=10s --timeout=3s --retries=5 CMD wget -q -O /dev/null http://127.0.0.1/ || exit 1
