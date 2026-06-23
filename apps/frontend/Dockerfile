FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM node:22-alpine
ARG KONFIGO_VERSION=0.0.4
LABEL org.opencontainers.image.version=$KONFIGO_VERSION
ENV KONFIGO_VERSION=$KONFIGO_VERSION
WORKDIR /app
COPY --from=build /app/build .
COPY --from=build /app/package*.json ./
RUN npm ci --omit=dev
EXPOSE 3000
ENV NODE_ENV=production
CMD ["node", "index.js"]
