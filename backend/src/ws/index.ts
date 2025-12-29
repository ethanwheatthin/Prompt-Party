import { FastifyInstance } from 'fastify';
import { WebSocket, RawData } from 'ws';
import jwt from 'jsonwebtoken';
import { getRoomStateForClient, attachSocketToRoom } from '../lib/roomManager';

export function setupWebsocket(app: FastifyInstance) {
  app.get('/ws', { websocket: true }, (connection, req) => {
    const ws: WebSocket = connection.socket as WebSocket;

    ws.on('message', (data: RawData) => {
      try {
        const msg = JSON.parse(data.toString());
        if (msg.type === 'auth') {
          const token = msg.payload?.token;
          if (!token) return ws.send(JSON.stringify({ type: 'auth_error', payload: { error: 'missing token' } }));
          try {
            const payload: any = jwt.verify(token, process.env.JWT_SECRET || 'dev-secret');
            attachSocketToRoom(payload.roomId, payload.playerId, ws);
            const roomState = getRoomStateForClient(payload.roomId);
            ws.send(JSON.stringify({ type: 'auth_ok', payload: { playerId: payload.playerId, roomState } }));
          } catch (e: any) {
            ws.send(JSON.stringify({ type: 'auth_error', payload: { error: e.message } }));
          }
        }
      } catch (e) {
        // ignore parse errors
      }
    });

    ws.on('close', () => {
      // handled by roomManager when socket removed
    });
  });
}
