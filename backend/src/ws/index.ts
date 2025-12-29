import { FastifyInstance } from 'fastify';
import { WebSocket, RawData } from 'ws';
import jwt from 'jsonwebtoken';
import { getRoomStateForClient, attachSocketToRoom, rooms, broadcastRoomState } from '../lib/roomManager';

export function setupWebsocket(app: FastifyInstance) {
  app.get('/ws', { websocket: true } as any, (connection, req) => {
    const ws: WebSocket = connection.socket as WebSocket;
    const remoteAddr = req.socket.remoteAddress + ':' + req.socket.remotePort;
    app.log.info(`WS connection from ${remoteAddr}`);

    ws.on('message', (data: RawData) => {
      try {
        const msg = JSON.parse(data.toString());
        app.log.debug(`WS message from ${remoteAddr}: ${data.toString()}`);
        if (msg.type === 'auth') {
          const token = msg.payload?.token;
          if (!token) {
            app.log.warn(`WS auth missing token from ${remoteAddr}`);
            return ws.send(JSON.stringify({ type: 'auth_error', payload: { error: 'missing token' } }));
          }
          try {
            const payload: any = jwt.verify(token, process.env.JWT_SECRET || 'dev-secret');
            app.log.info(`WS auth success from ${remoteAddr} -> roomId=${payload.roomId}, playerId=${payload.playerId}, role=${payload.role}`);
            attachSocketToRoom(payload.roomId, payload.playerId, ws);
            const roomState = getRoomStateForClient(payload.roomId);
            const authOk = { type: 'auth_ok', payload: { playerId: payload.playerId, roomState } };
            ws.send(JSON.stringify(authOk));
            app.log.debug(`Sent auth_ok to ${remoteAddr} for player ${payload.playerId}`);
            // broadcast updated room_state to all connected sockets in the room
            try { broadcastRoomState(payload.roomId); } catch (e) { app.log.error(`Failed to broadcast after auth: ${e}`); }
          } catch (e: any) {
            app.log.warn(`WS auth failed from ${remoteAddr}: ${e.message}`);
            ws.send(JSON.stringify({ type: 'auth_error', payload: { error: e.message } }));
          }
        }
      } catch (e) {
        app.log.warn(`WS failed to parse message from ${remoteAddr}`);
      }
    });

    ws.on('close', (code, reason) => {
      app.log.info(`WS closed ${remoteAddr} code=${code} reason=${reason}`);
      // remove socket references from any players in rooms and broadcast updates
      try {
        for (const [roomId, room] of rooms) {
          let changed = false;
          for (const p of room.players) {
            if (p.socket === ws) { p.socket = null; changed = true; }
          }
          if (changed) {
            try { broadcastRoomState(roomId); } catch (e) { app.log.error(`Failed to broadcast on close: ${e}`); }
          }
        }
      } catch (err) {
        app.log.error(`Error cleaning up socket references: ${err}`);
      }
    });
  });
}
