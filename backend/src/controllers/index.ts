import { FastifyInstance } from 'fastify';
import { createRoomHandler } from './rooms';
import { joinHandler } from './join';

export function setupRoutes(app: FastifyInstance) {
  app.post('/rooms', createRoomHandler);
  app.post('/join', joinHandler);
}
