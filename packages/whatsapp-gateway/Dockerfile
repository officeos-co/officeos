FROM node:22-slim

WORKDIR /app

COPY packages/whatsapp-gateway/package.json packages/whatsapp-gateway/package-lock.json* ./

RUN npm install --omit=dev

COPY packages/whatsapp-gateway/src/ src/

EXPOSE 3002

CMD ["node", "src/server.js"]
