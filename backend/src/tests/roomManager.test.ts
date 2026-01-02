import { describe, it, expect, beforeEach } from 'vitest';
import jwt from 'jsonwebtoken';
import { createRoom, joinRoom, _test_clear, rooms, genJoinCode, startRound } from '../lib/roomManager';

beforeEach(() => {
  _test_clear();
});

describe('room manager', () => {
  it('creates room and returns join code and token', () => {
    const r = createRoom('Host');
    expect(r.roomId).toBeTruthy();
    expect(r.joinCode).toMatch(/^PP[A-Z2-9]{6}$/);
    expect(r.token).toBeTruthy();

    const payload: any = jwt.verify(r.token, process.env.JWT_SECRET || 'dev-secret');
    expect(payload.roomId).toBe(r.roomId);
    expect(payload.role).toBe('host');
  });

  it('joinRoom invalid code throws', () => {
    expect(() => joinRoom('BADCODE', 'Alice')).toThrow();
  });

  it('genJoinCode format', () => {
    const code = genJoinCode();
    expect(code).toMatch(/^PP[A-Z2-9]{6}$/);
  });

  it('startRound creates round with correct timestamps', () => {
    const r = createRoom('Host');
    joinRoom(r.joinCode, 'Player1');
    joinRoom(r.joinCode, 'Player2');
    
    const beforeStart = Date.now();
    const round = startRound(r.roomId);
    const afterStart = Date.now();
    
    expect(round.roundId).toBeTruthy();
    expect(round.actorId).toBeTruthy();
    expect(round.topic).toBeTruthy();
    expect(round.startedAt).toBeGreaterThanOrEqual(beforeStart);
    expect(round.startedAt).toBeLessThanOrEqual(afterStart);
    expect(round.minCutoffAt).toBe(round.startedAt + 30000);
    expect(round.maxEndAt).toBe(round.startedAt + 90000);
  });

  it('startRound rotates actor through players', () => {
    const r = createRoom('Host');
    const p1 = joinRoom(r.joinCode, 'Player1');
    const p2 = joinRoom(r.joinCode, 'Player2');
    const p3 = joinRoom(r.joinCode, 'Player3');
    
    const round1 = startRound(r.roomId);
    const round2 = startRound(r.roomId);
    const round3 = startRound(r.roomId);
    const round4 = startRound(r.roomId);
    
    const actors = [round1.actorId, round2.actorId, round3.actorId, round4.actorId];
    
    // All three players should be selected in first three rounds
    expect(actors.slice(0, 3)).toContain(p1.playerId);
    expect(actors.slice(0, 3)).toContain(p2.playerId);
    expect(actors.slice(0, 3)).toContain(p3.playerId);
    
    // Fourth round should cycle back to first actor
    expect(round4.actorId).toBe(round1.actorId);
  });

  it('startRound throws when no players available', () => {
    const r = createRoom('Host');
    expect(() => startRound(r.roomId)).toThrow('No players available to be actor');
  });
});
