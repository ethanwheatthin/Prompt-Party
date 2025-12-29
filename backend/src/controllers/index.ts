import { FastifyInstance } from 'fastify';
import { createRoomHandler } from './rooms';
import { joinHandler } from './join';
import { qrHandler } from './qr';

export function setupRoutes(app: FastifyInstance) {
  app.post('/rooms', createRoomHandler);
  app.post('/join', joinHandler);
  app.get('/qr', qrHandler);
  app.get('/', async () => 'Hello world!');
}
