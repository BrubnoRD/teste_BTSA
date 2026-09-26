# Tela (React + Vite) servida como arquivos estáticos pelo nginx.
# Construída pelo docker-compose.yml da raiz: docker compose up -d --build

FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . ./

# O Vite embute a URL da API no build. Ela é acessada pelo navegador, não de dentro
# da rede do Docker, por isso aponta para a porta publicada no host.
ARG VITE_URL_API=http://localhost:5080
ENV VITE_URL_API=$VITE_URL_API
RUN npm run build

FROM nginx:1.27-alpine
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
