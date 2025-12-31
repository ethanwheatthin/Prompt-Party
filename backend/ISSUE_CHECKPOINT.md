# Checkpoint — Player Phone Join Implementation

This checkpoint summarizes changes made so far, testing routes for local and production, and recommended next steps for dynamic host naming and production deployment.

## Summary of changes made
- Backend
  - Added static file serving for frontend pages (served from `backend/public/`)
  - Implemented `POST /join` endpoint (`backend/src/controllers/join.ts`) that:
    - Validates and normalizes `joinCode` (trim + uppercase) and `name` (trim, max 20 chars)
    - Creates a player record and returns `{ playerId, token, roomId, joinCode }`
    - Returns `400` when missing inputs, `404` when room not found
  - Implemented `GET /qr` endpoint (`backend/src/controllers/qr.ts`) to return a PNG QR for a room or arbitrary URL
  - WebSocket changes (`backend/src/ws/index.ts`): on `auth` attach player socket and broadcast `room_state` to all connected sockets in room; clear sockets on `close` and broadcast updates
  - Room manager (`backend/src/lib/roomManager.ts`) now returns `joinCode` from `joinRoom` and exports `broadcastRoomState` used by WS
  - Added TypeScript shims for `qrcode` and `@fastify/static` to ease local dev type complaints
  - Added `qrcode` dependency to `backend/package.json`
  - Added `backend/public/player.html` — mobile-friendly UI for joining and showing lobby
  - Added `backend/README.md` with testing instructions

- Frontend (mobile web)
  - `backend/public/player.html` — join form, fetch `/join`, open WebSocket, auth via token, render lobby and player statuses

- Unity
  - `Assets/Scripts/QRFromServer.cs` — downloads QR PNG from `/qr?room=...` or `/qr?url=...` and displays in `RawImage`
  - `Assets/Scripts/RoomCreator.cs` updated to request QR generation after creating a room and call `QRFromServer.GenerateForRoom(joinCode)`
  - Removed direct dependency on QRCoder DLL (use backend-generated PNG instead)

## Local testing route (recommended for development)
1. Set HOST_URL to your machine LAN IP (so phones can reach it)
   - Create `backend/.env` with:
     ```
     HOST_URL=http://<YOUR_LAN_IP>:3000
     ```
   - Find LAN IP:
     - Windows: `ipconfig` (look for IPv4 Address)
     - macOS/Linux: `ip addr` or `ifconfig`
2. Start backend
   ```bash
   cd backend
   npm install
   npm run dev
   ```
3. Start Unity (Host)
   - Assign `QRFromServer` and `RoomCreator` components in the scene:
     - `QRFromServer.backendHost` = `http://<YOUR_LAN_IP>:3000`
     - `RoomCreator.backendHost` = `http://<YOUR_LAN_IP>:3000`
     - Drag the `QRFromServer` GameObject into `RoomCreator.qrFetcher` inspector field
     - Assign a `RawImage` to `QRFromServer.qrImage`
4. Create room in Unity
   - `RoomCreator` POSTs to `/rooms` and receives `joinCode`
   - `RoomCreator` triggers `QRFromServer.GenerateForRoom(joinCode)` which downloads `/qr?room=...` and displays QR
5. Phone test
   - Scan QR (encodes `http://<YOUR_LAN_IP>:3000/player.html?code=...`) or open `http://<YOUR_LAN_IP>:3000/player.html` manually
   - Enter name and press Join
   - Phone should POST `/join`, receive token, open WS, and display lobby
   - Unity host receives `room_state` broadcast and sees player in player list

## Production route (recommended)
1. Acquire domain (e.g., `prompt.party`) and a public server (VM or container)
2. Deploy backend to server and point DNS
   - Set `HOST_URL=https://prompt.party` in backend `.env`
   - Run backend behind nginx (reverse proxy) with TLS (Let's Encrypt)
   - Configure nginx to proxy `/ws` as a WebSocket to backend and serve static files
3. Update Unity and frontend for secure endpoints
   - Use `wss://prompt.party/ws` in Unity `NetworkManager.serverUrl` for WebSocket
   - Use `https://prompt.party` in `RoomCreator.backendHost` and `QRFromServer.backendHost`
4. Use `https://prompt.party/player.html?code=...` in encoded QR (backend `/qr` will do this using HOST_URL)

## Dynamic host naming / flexible URL strategies
- We implemented two flexible options to encode dynamic URLs into QR codes:
  1. Backend `/qr?room=PP...` produces QR for `HOST_URL/player.html?code=PP...` (HOST_URL is configurable via `.env`)
  2. Backend `/qr?url=...` produces QR for any arbitrary target URL — Unity can call `GenerateForUrl` to encode a fully custom URL (e.g., a tunnel URL or cloud-hosted URL)

- For runtime dynamic host selection in Unity you can:
  - Add a UI input to set `backendHost` at runtime (developer console or settings panel). Then call `qrFetcher.GenerateForUrl("https://"+input+"/player.html?code="+joinCode)` after creating a room.
  - Or integrate with a tunnel provider (ngrok) programmatically: call ngrok API or spawn the ngrok process, get the public URL, then call `GenerateForUrl(publicUrl + "/player.html?code=" + joinCode)`.

- Production-grade dynamic domain strategies
  - Use a single stable domain (prompt.party) and encode join code in the query string — simplest and recommended.
  - If you must create per-room subdomains dynamically, use Cloudflare API + wildcard certs. This is more complex and usually unnecessary for gameplay.

## Next step implementation suggestions (pick one or more)
- Add a small Unity sample scene (Canvas + RawImage + RoomCreator + QRFromServer) already wired and defaulted to a dev LAN IP.
- Implement an optional `/leave` endpoint and a Leave button in `player.html` to gracefully disconnect players.
- Implement ngrok integration for dev: spawn ngrok from Unity or a local script, retrieve public URL, call `GenerateForUrl` to produce QR for the tunnel URL.
- Harden production deployment: provide an `nginx` config and a minimal deploy script for a VM.

---

If you want, I can now create a small Unity sample scene and wiring, or implement the ngrok flow to get a public URL automatically. Which would you like next?