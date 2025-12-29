import Fastify = require('fastify');
import websocket = require('@fastify/websocket');
import dotenv = require('dotenv');
import { setupRoutes } from './controllers';
import { setupWebsocket } from './ws';

dotenv.config();

const PORT = Number(process.env.PORT || 3000);

export const app = Fastify({ logger: true });

app.register(websocket as any).then(() => {
  setupRoutes(app);
  setupWebsocket(app);

  app.listen({ port: PORT, host: '0.0.0.0' }).then(() => {
    app.log.info(`Server listening on ${PORT}`);
  }).catch((err) => {
    app.log.error(err);
    process.exit(1);
  });
}).catch((err) => {
  console.error('Failed to register websocket plugin', err);
  process.exit(1);
});
