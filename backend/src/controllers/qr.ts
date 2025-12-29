import { FastifyReply, FastifyRequest } from 'fastify';
import QRCode from 'qrcode';

export async function qrHandler(req: FastifyRequest, reply: FastifyReply) {
  const room = (req.query as any)?.room;
  const url = (req.query as any)?.url;
  const target = room ? `${process.env.HOST_URL || 'http://localhost:3000'}/player.html?code=${encodeURIComponent(room)}` : url;
  if (!target) return reply.status(400).send({ error: 'room or url required' });

  try {
    const png = await QRCode.toBuffer(target, { type: 'png', margin: 2, width: 400 });
    reply.header('Content-Type', 'image/png');
    return reply.send(png);
  } catch (e: any) {
    return reply.status(500).send({ error: e.message });
  }
}
