import { describe, it, expect, beforeEach } from 'vitest';
import { createRoom, joinRoom, _test_clear, rooms } from '../lib/roomManager';

beforeEach(() => {
  _test_clear();
});

describe('room manager', () => {
  it('creates room and returns join code and token', () => {
    const r = createRoom('Host');
    expect(r.roomId).toBeTruthy();
    expect(r.joinCode).toMatch(/^PP[A-Z2-9]{6}$/);
    expect(r.token).toBeTruthy();
  });

  it('joinRoom invalid code throws', () => {
    expect(() => joinRoom('BADCODE', 'Alice')).toThrow();
  });
});
