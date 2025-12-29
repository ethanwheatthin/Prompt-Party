import { FastifyReply, FastifyRequest } from 'fastify';
import { joinRoom } from '../lib/roomManager';

export async function joinHandler(req: FastifyRequest, reply: FastifyReply) {
  const body = req.body as any;
  const rawCode = body?.joinCode;
  const name = typeof body?.name === 'string' ? body.name.trim().slice(0,20) : '';
  if (!rawCode || !name) return reply.status(400).send({ error: 'joinCode and name required' });
  const joinCode = String(rawCode).trim().toUpperCase();
  try {
    const result = joinRoom(joinCode, name);
    // best-effort logging
    try { console.log(`[joinHandler] Player ${name} joined room ${joinCode} -> playerId=${result.playerId}`); } catch (e) {}
    return reply.send(result);
  } catch (e: any) {
    if (e.message === 'invalid join code') return reply.status(404).send({ error: 'room not found' });
    return reply.status(400).send({ error: e.message });
  }
}
