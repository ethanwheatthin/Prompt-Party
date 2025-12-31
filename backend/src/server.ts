import Fastify = require('fastify');
import websocket = require('@fastify/websocket');
import fastifyStatic = require('@fastify/static');
import path = require('path');
import dotenv = require('dotenv');
import { setupRoutes } from './controllers';
import { setupWebsocket } from './ws';

dotenv.config();

const PORT = Number(process.env.PORT || 3000);

export const app = Fastify({ logger: true });

// register static files from ../public
app.register(fastifyStatic as any, {
  root: path.join(__dirname, '..', 'public'),
  prefix: '/', // serve at /player.html
});

app.register(websocket as any).then(() => {
  setupRoutes(app);
  setupWebsocket(app);

  app.listen({ port: PORT, host: '0.0.0.0' }).then(() => {
    app.log.info(`Server listening on ${PORT}`);
  }).catch((err: any) => {
    app.log.error(err);
    process.exit(1);
  });
}, (err: any) => {
  console.error('Failed to register websocket plugin', err);
  process.exit(1);
});
