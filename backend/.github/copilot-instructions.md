# PromptParty Backend — AI Coding Guide

## Project Overview
Real-time party game backend using **Fastify + WebSocket**, supporting Unity host clients and mobile web players. Players submit creative prompts for an "actor" to perform, vote on favorites, and watch performances with timed phases.

## Architecture Pattern: In-Memory State + WebSocket Events

**Central pattern**: `roomManager.ts` holds all game state in-memory (no DB). Changes broadcast via WebSocket to all connected sockets in a room.

```typescript
// Pattern: Mutate state → Broadcast to room
submitPrompt(roomId, playerId, promptText);
broadcastRoomState(roomId); // Sends room_state to all player.socket
```

**Socket lifecycle**: 
1. Client connects → sends `auth` message with JWT
2. Server verifies token → calls `attachSocketToRoom(roomId, playerId, socket)`
3. Socket stored in `room.players[].socket` for broadcasting
4. On disconnect, socket set to `null` (not removed from players array)

**Key broadcast events** (see [src/ws/index.ts](src/ws/index.ts)):
- `room_state` — full room snapshot (players, currentRound)
- `round_started` — new round begins with actorId, topic, timestamps
- `prompts_update` → `voting_started` — automatic transition when all prompts submitted
- `vote_update` → `prompt_selected` — automatic when all votes cast

## Core Game Flow (MVP)

**Phases** (see [project-mvp.md](project-mvp.md)):
1. **Lobby** — players join via `/join` POST with `joinCode`
2. **Round Start** — host sends `host_action { action: "start_round" }` → `startRound()` picks actor, broadcasts `round_started`
3. **Prompt Submission** — non-actors send `submit_prompt { prompt: "text" }` → auto-advances to voting when all submitted
4. **Voting** — players send `submit_vote { promptPlayerId }` → `tallyVotes()` picks winner, broadcasts `prompt_selected`
5. **Performance** — (not yet implemented) actor performs winning prompt

**Actor rotation**: `startRound()` uses `room.lastActorIndex` to cycle through non-host players (see [roomManager.test.ts](src/tests/roomManager.test.ts) line 55).

## Critical Developer Workflows

**Start dev server**:
```bash
npm run dev  # ts-node-dev with hot reload
```

**Run tests**:
```bash
npm test  # vitest watch mode
```

**Build for production**:
```bash
npm run build  # tsc → compiles to dist/
```

**Testing with phone**: See [README.md](README.md) — use LAN IP (not localhost) for mobile testing.

## Project-Specific Conventions

### JWT Authentication
- **Tokens generated** in `createRoom()` and `joinRoom()` (see [roomManager.ts](src/lib/roomManager.ts#L48-L56))
- **Tokens contain**: `{ roomId, playerId, role: 'host'|'player' }`
- **Never expire during session**: 24h expiry, client stores in localStorage
- **WebSocket auth**: First message must be `{ type: 'auth', payload: { token } }`

### Join Code Format
- Always `PP` + 6 chars from `ABCDEFGHJKLMNPQRSTUVWXYZ23456789` (excludes I, O, 0, 1)
- **Normalized to uppercase** in `/join` handler (see [join.ts](src/controllers/join.ts#L8))
- Stored in `joinCodeIndex` Map for O(1) lookup

### Error Handling Pattern
```typescript
// In HTTP handlers: status codes + { error: string }
return reply.status(404).send({ error: 'room not found' });

// In WebSocket: { type: 'error', payload: { error: string } }
ws.send(JSON.stringify({ type: 'error', payload: { error: 'not authenticated' } }));
```

### Testing Convention
- Use `_test_clear()` in `beforeEach` to reset in-memory state (see [roomManager.test.ts](src/tests/roomManager.test.ts#L4-L6))
- **No mocks**: Test against real `roomManager` functions
- Timestamp assertions use `Date.now()` range checks (line 35-41)

## Key Integration Points

### Unity ↔ Backend
- Unity calls `POST /rooms/create` → gets `joinCode` + host `token`
- Unity connects to `/ws`, sends `auth`, then `host_action { action: "start_round" }`
- Unity listens for `round_started`, `prompts_update`, `prompt_selected`

### Mobile Web Player (player.html)
- Single-page app with inline JavaScript (no build step)
- Phases: join form → lobby → prompt submission → voting → results
- WebSocket reconnect not implemented (disconnects reset state)

### External Dependencies
- **QR code generation**: `qrcode` npm package (see [qr.ts](src/controllers/qr.ts))
- **Static files**: [public/player.html](public/player.html) served via `@fastify/static`

## Common Pitfalls

1. **Forgetting `broadcastRoomState()`** after state mutations → clients see stale data
2. **Not checking `socket.readyState === socket.OPEN`** → crashes on closed sockets
3. **Actor submitting prompts**: `submitPrompt()` throws error (by design, line 167)
4. **Prompt text before all submitted**: `getRoundWithPrompts()` returns `'???'` (line 275)
5. **Testing with localhost on phone**: Must use LAN IP (e.g., `192.168.x.x:3000`)

## Unanswered Questions (from README)
- Should room codes auto-uppercase in frontend or rely on server normalization? *(Currently server normalizes)*
- Should players be able to rejoin with same name after disconnect? *(Not implemented)*
- Should there be a "Leave" button or rely on tab close? *(Tab close only)*
