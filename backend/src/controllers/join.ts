import { FastifyReply, FastifyRequest } from 'fastify';
import { joinRoom } from '../lib/roomManager';

export async function joinHandler(req: FastifyRequest, reply: FastifyReply) {
  const body = req.body as any;
  if (!body?.joinCode || !body?.name) return reply.status(400).send({ error: 'joinCode and name required' });
  try {
    const result = joinRoom(body.joinCode, body.name);
    return reply.send(result);
  } catch (e: any) {
    return reply.status(400).send({ error: e.message });
  }
}
