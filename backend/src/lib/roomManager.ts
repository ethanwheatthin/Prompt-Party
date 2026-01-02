import { v4 as uuidv4 } from 'uuid';
import jwt from 'jsonwebtoken';

export type Player = {
  id: string;
  name: string;
  isHost?: boolean;
  socket?: any;
};

export type Round = {
  roundId: string;
  actorId: string;
  topic: string;
  startedAt: number;
  minCutoffAt: number;
  maxEndAt: number;
};

export type Room = {
  roomId: string;
  joinCode: string;
  hostPlayerId: string;
  players: Player[];
  currentRound?: Round | null;
  lastActorIndex?: number;
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
  const room: Room = { roomId, joinCode, hostPlayerId, players: [host], currentRound: null, lastActorIndex: -1 };
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
  return { playerId, token, roomId, joinCode: room.joinCode };
}

export function attachSocketToRoom(roomId: string, playerId: string, socket: any) {
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
    players: room.players.map((p) => ({ id: p.id, name: p.name, isHost: p.isHost })),
    currentRound: room.currentRound
  };
}

export function broadcastRoomState(roomId: string) {
  const room = rooms.get(roomId);
  if (!room) return;
  const state = getRoomStateForClient(roomId);

  // best-effort logging to stdout to help debug when fastify logger not available
  try {
    // eslint-disable-next-line no-console
    console.log(`[roomManager] broadcasting room_state for room=${roomId} players=${room.players.length}`);
  } catch (e) {}

  room.players.forEach((p) => {
    if (p.socket && p.socket.readyState === p.socket.OPEN) {
      p.socket.send(JSON.stringify({ type: 'room_state', payload: state }));
    }
  });
}

export function _test_clear() {
  rooms.clear();
  joinCodeIndex.clear();
}

const defaultTopics = [
  'A day at the beach',
  'Cooking dinner',
  'Meeting a celebrity',
  'First day at a new job',
  'Lost in a forest',
  'Shopping spree',
  'Winning the lottery',
  'Flying in an airplane',
];

export function startRound(roomId: string): Round {
  const room = rooms.get(roomId);
  if (!room) throw new Error('Room not found');
  
  // Get non-host players for actor selection
  const eligiblePlayers = room.players.filter(p => !p.isHost);
  if (eligiblePlayers.length === 0) throw new Error('No players available to be actor');
  
  // Rotate through players
  const nextActorIndex = ((room.lastActorIndex ?? -1) + 1) % eligiblePlayers.length;
  const actor = eligiblePlayers[nextActorIndex];
  
  // Select random topic
  const topic = defaultTopics[Math.floor(Math.random() * defaultTopics.length)];
  
  const now = Date.now();
  const round: Round = {
    roundId: uuidv4(),
    actorId: actor.id,
    topic,
    startedAt: now,
    minCutoffAt: now + 30000, // 30 seconds
    maxEndAt: now + 90000, // 90 seconds
  };
  
  room.currentRound = round;
  room.lastActorIndex = nextActorIndex;
  
  return round;
}

export function getRoom(roomId: string): Room | undefined {
  return rooms.get(roomId);
}

export { rooms };
