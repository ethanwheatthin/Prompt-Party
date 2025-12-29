import { FastifyReply, FastifyRequest } from 'fastify';
import { createRoom } from '../lib/roomManager';

export async function createRoomHandler(req: FastifyRequest, reply: FastifyReply) {
  const body = req.body as any;
  if (!body?.hostName) return reply.status(400).send({ error: 'hostName required' });
  const room = createRoom(body.hostName);
  return reply.send(room);
}
