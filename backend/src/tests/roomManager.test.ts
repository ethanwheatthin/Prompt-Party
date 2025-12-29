import { describe, it, expect, beforeEach } from 'vitest';
import jwt from 'jsonwebtoken';
import { createRoom, joinRoom, _test_clear, rooms, genJoinCode } from '../lib/roomManager';

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
});
