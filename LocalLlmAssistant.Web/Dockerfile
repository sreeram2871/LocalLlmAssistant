# ==========================================
# Build Angular application
# ==========================================

FROM node:22-alpine AS build

WORKDIR /app

COPY package*.json ./

RUN npm ci

COPY . .

RUN npm run build


# ==========================================
# Nginx
# ==========================================

FROM nginx:alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf

COPY --from=build /app/dist/LocalLlmAssistant.Web/browser /usr/share/nginx/html

RUN rm /usr/share/nginx/html/index.html && \
    mv /usr/share/nginx/html/index.csr.html /usr/share/nginx/html/index.html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]