import { v4 as uuidv4 } from 'uuid';
import jwt from 'jsonwebtoken';
import { WebSocket } from 'ws';

export type Player = {
  id: string;
  name: string;
  isHost?: boolean;
  socket?: WebSocket | null;
};

export type Room = {
  roomId: string;
  joinCode: string;
  hostPlayerId: string;
  players: Player[];
};

const rooms = new Map<string, Room>();
const joinCodeIndex = new Map<string, string>();

export function genJoinCode() {
  const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  let code = '';
  for (let i = 0; i < 6; i++) code += chars[Math.floor(Math.random() * chars.length)];
  return 'PP' + code;
}

export function createRoom(hostName: string) {
  const roomId = uuidv4();
  let joinCode = genJoinCode();
  while (joinCodeIndex.has(joinCode)) joinCode = genJoinCode();
  const hostPlayerId = uuidv4();
  const host: Player = { id: hostPlayerId, name: hostName, isHost: true };
  const room: Room = { roomId, joinCode, hostPlayerId, players: [host] };
  rooms.set(roomId, room);
  joinCodeIndex.set(joinCode, roomId);

  const token = jwt.sign({ roomId, playerId: hostPlayerId, role: 'host' }, process.env.JWT_SECRET || 'dev-secret', { expiresIn: '24h' });
  return { roomId, joinCode, hostPlayerId, token, joinUrl: `${process.env.HOST_URL || 'http://localhost:3000'}/join?code=${joinCode}` };
}

export function joinRoom(joinCode: string, name: string) {
  const roomId = joinCodeIndex.get(joinCode);
  if (!roomId) throw new Error('invalid join code');
  const room = rooms.get(roomId)!;
  const playerId = uuidv4();
  const player: Player = { id: playerId, name };
  room.players.push(player);
  const token = jwt.sign({ roomId, playerId, role: 'player' }, process.env.JWT_SECRET || 'dev-secret', { expiresIn: '24h' });
  broadcastRoomState(roomId);
  return { playerId, token, roomId };
}

export function attachSocketToRoom(roomId: string, playerId: string, socket: WebSocket) {
  const room = rooms.get(roomId);
  if (!room) return;
  const player = room.players.find((p) => p.id === playerId);
  if (!player) return;
  player.socket = socket;
  // keep sockets list as players[*].socket
}

export function getRoomStateForClient(roomId: string) {
  const room = rooms.get(roomId);
  if (!room) return null;
  return {
    roomId: room.roomId,
    joinCode: room.joinCode,
    players: room.players.map((p) => ({ id: p.id, name: p.name, isHost: p.isHost }))
  };
}

function broadcastRoomState(roomId: string) {
  const room = rooms.get(roomId);
  if (!room) return;
  const state = getRoomStateForClient(roomId);
  room.players.forEach((p) => {
    if (p.socket && p.socket.readyState === WebSocket.OPEN) {
      p.socket.send(JSON.stringify({ type: 'room_state', payload: state }));
    }
  });
}

export function _test_clear() {
  rooms.clear();
  joinCodeIndex.clear();
}

export { rooms };
