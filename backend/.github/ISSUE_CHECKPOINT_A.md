# Issue: Checkpoint A — Player registration + room creation

Summary
-------
Implement REST endpoints and WebSocket auth for basic room lifecycle and player registration. Server should issue JWT tokens and broadcast room state updates.

Acceptance criteria
-------------------
- POST `/rooms` accepts `{ "hostName": "string" }` and returns `{ roomId, joinCode, hostPlayerId, token, joinUrl }`.
- POST `/join` accepts `{ "joinCode", "name" }` and returns `{ playerId, token, roomId }`.
- WS endpoint at `/ws` accepts JSON envelope `{ type: "auth", payload: { token } }` and responds with `{ type: "auth_ok", payload: { playerId, roomState } }` if token valid.
- Room state is broadcast to all connected sockets when a player joins or when a socket disconnects.
- Unit tests for join code validation and token issuance/verification exist.

How to test locally
-------------------
1. Start server:
   - npm install
   - npm run dev
2. Create a room (curl):
   - curl -X POST http://localhost:3000/rooms -H "Content-Type: application/json" -d '{"hostName":"Unity Host"}'
3. Use `wscat` or Unity WebSocket to connect to `ws://localhost:3000/ws` and send auth envelope with returned token.
4. POST /join from another terminal to join a player; the connected sockets should receive `room_state` broadcast including the new player.

Notes
-----
This issue was created before implementing changes in the backend/ folder. See README for exact examples.
