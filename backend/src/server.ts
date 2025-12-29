import Fastify from 'fastify';
import websocket from '@fastify/websocket';
import dotenv from 'dotenv';
import { setupRoutes } from './controllers';
import { setupWebsocket } from './ws';

dotenv.config();

const PORT = Number(process.env.PORT || 3000);

export const app = Fastify({ logger: true });

await app.register(websocket);

setupRoutes(app);
setupWebsocket(app);

app.listen({ port: PORT, host: '0.0.0.0' }).then(() => {
  app.log.info(`Server listening on ${PORT}`);
});
