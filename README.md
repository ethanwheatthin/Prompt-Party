# Prompt Party

**Prompt Party** is a party game where players write creative prompts for an actor to perform, vote on the best prompt, and can cut performances that don't meet expectations. Built with Unity for the host display and a Node.js backend serving phone-based player interfaces.

## 🎮 Game Overview

- **Host (Unity)**: Displays the game state on a TV/monitor, manages rooms, and shows live gameplay
- **Players (Mobile Web)**: Join via room codes on their phones, submit prompts, vote, and rate performances
- **Core Loop**: Round start → Prompt submission → Voting → Performance → Cut mechanic → Rating → Leaderboard

## 🏗️ Architecture

- **Unity Frontend**: Game client located in the project root. Open with Unity Editor (version in `ProjectSettings/ProjectVersion.txt`)
  - Game code: `Assets/Scripts/`
  - Scenes: `Assets/Scenes/`
  - Prefabs: `Assets/Prefabs/`
  - Uses Unity Input System (`Assets/InputSystem_Actions.inputactions`)

- **Backend**: Node.js/TypeScript server in `backend/`
  - Room management and WebSocket communication
  - Static player UI served from `backend/public/`
  - Key files: `backend/src/server.ts`, `backend/src/ws/index.ts`, `backend/src/lib/roomManager.ts`

## 🚀 Quick Start

### Backend Setup

```bash
cd backend
npm install
npm run dev  # Starts server on 0.0.0.0:3000
```

### Player Access

Open `http://<YOUR_LAN_IP>:3000/player.html` on a phone to join a game.

### Unity Development

1. Open the Unity project in Unity Editor (version from `ProjectSettings/ProjectVersion.txt`)
2. Press Play to host a room (Unity will contact backend on the LAN)
3. Changes to C# require the Editor to recompile

### Testing

```bash
cd backend
npm run build  # TypeScript compile
npm run test   # Run vitest tests
```

## 📋 MVP Development Roadmap

### High Priority — Core Game Loop (MVP Critical Path)

#### 1. Round Start Flow (1–2 days)
**Backend:**
- Add `host_action` WebSocket handler for "start_round"
- Implement round creation logic: select actor, generate roundId, set timestamps
- Broadcast `round_started` event with round details
- Add `currentRound` field to `room_state` payload

**Unity:**
- Add "Start Round" button in Lobby (host only)
- Create RoundScene/RoundView with topic, actor name, countdown timer
- Show "Waiting for prompts..." status

**Player Web:**
- Listen for `round_started` event
- Show round UI with topic, timer, actor banner (if applicable)
- Display prompt submission form for non-actor players

**Acceptance:** Host starts round → backend broadcasts → Unity shows round UI → phones show topic and prompt form

#### 2. Prompt Submission Phase (1 day)
**Backend:**
- Add `submit_prompt` WebSocket handler
- Validate: round exists, not ended, player not actor, text length (10–200 chars)
- Store prompt and broadcast update

**Player Web:**
- Prompt submission form (textarea + submit button)
- Show confirmation feedback after submission
- Disable form after submit

**Unity:**
- Display "Waiting for prompts..." with live count

**Acceptance:** Player submits prompt → backend stores it → Unity shows count → player sees confirmation

#### 3. Prompt Voting Phase (1–2 days)
**Backend:**
- Add `vote_prompt` WebSocket handler
- Validate: player not actor, one vote per player
- Calculate winner when all votes cast
- Broadcast `voting_started` and `voting_ended` events

**Player Web:**
- Display list of prompts with vote buttons
- Show "Vote cast ✓" feedback
- Display winning prompt

**Unity:**
- Display prompt list during voting
- Highlight winning prompt
- Transition to performance phase

**Acceptance:** All players vote → backend picks winner → displays winning prompt → advances to performance

#### 4. Performance Phase (1–2 days)
**Backend:**
- Broadcast `performance_started` with prompt and timing
- Auto-end performance at `maxEndAt` if not cut
- Track performance state in round

**Unity:**
- Display winning prompt large (for actor)
- Show countdown timer
- Display cut availability status

**Player Web:**
- Show performance UI with prompt and timer
- Actor: "End Performance" button
- Non-actors: "Cut" button (disabled until `minCutoffAt`)

**Acceptance:** Performance starts → Unity shows prompt + timer → phones show cut button → actor or players can end

#### 5. Cut-Vote Mechanic (1 day)
**Backend:**
- Add `cast_cut_vote` WebSocket handler
- Validate: time >= `minCutoffAt`, player not actor, one vote per player
- Calculate threshold: `Math.ceil(activeNonActorCount * 0.5)`
- Broadcast `cut_vote_update` and `performance_cut` when threshold reached

**Unity:**
- Show live cut vote ticker: "Cut votes: 2 / 3"
- Show "PERFORMANCE CUT!" animation when cut occurs

**Player Web:**
- Enable cut button after `minCutoffAt`
- Show live cut vote count
- Display cut feedback

**Acceptance:** Players press Cut after 30s → backend detects threshold → broadcasts cut → advance to rating

#### 6. Rating Phase (1–2 days)
**Backend:**
- Broadcast `rating_phase_start`
- Add `submit_rating` handler (1–10 scale)
- Calculate actor score (average)
- Broadcast `round_ended` with scores and leaderboard

**Unity:**
- Show rating phase UI
- Display rating input for non-actor players
- Show actor score and leaderboard when round ends

**Player Web:**
- Show rating UI (1–10 buttons or slider)
- Submit rating and show confirmation
- Display score and leaderboard

**Acceptance:** All players rate → backend calculates score → display score and leaderboard

#### 7. Leaderboard & Multi-Round Flow (1 day)
**Backend:**
- Store cumulative scores per player
- Add `next_round` handler to rotate actor
- Include leaderboard in `room_state` and `round_ended`

**Unity:**
- Leaderboard view with player names and scores
- Host "Next Round" button after round ends

**Player Web:**
- Display leaderboard between rounds
- Show "Waiting for host to start next round..."

**Acceptance:** Round ends → leaderboard shown → host starts next round with rotated actor

### Medium Priority — Polish & UX

#### 8. Unity Host UI Polish (2–3 days)
- Improve Lobby UI with grid layout, player avatars, connection status
- Large room code display and QR code
- Separate scenes/panels for Lobby, Round phases, Leaderboard
- Phase transition animations
- Phase labels: "Prompt Submission", "Voting", "Performance", "Rating"
- Server connection status and reconnect UI

#### 9. Player Web UI Polish (2–3 days)
- Improve lobby UI with room code, host name, player count
- Loading spinners and error toasts
- Sound effects/vibration feedback
- Enhanced prompt submission with character count and examples
- Card-based voting UI
- Animated timer in performance phase
- Star rating component or emoji slider
- Responsive design for various phone sizes

#### 10. Reconnection & Presence Handling (1–2 days)
**Backend:**
- Session tokens for reconnection
- Track `lastSeen` and mark players disconnected after timeout
- Allow reconnection with token validation
- Broadcast current state on reconnect

**Unity & Player Web:**
- Detect WebSocket disconnect
- Show "Reconnecting..." UI
- Auto-reconnect with exponential backoff
- Re-send auth token on reconnect

**Acceptance:** Player disconnects mid-round → reconnects → rejoins at current phase

#### 11. Topic Selection / Prompt Packs (0.5–1 day)
- Create topic/pack JSON file
- Randomly select topic on round start
- Allow host to choose topic (optional)
- Display topic name in round UI

### Lower Priority — Testing, DevOps, Future Features

#### 12. Unit & Integration Tests (2–3 days)
- Backend: unit tests for game logic (round lifecycle, cut threshold, voting, rating)
- Backend: integration tests for WebSocket flows
- Unity: Unity Test Runner for NetworkManager and RoomStateController

#### 13. Persistence (Postgres + Prisma) (2–3 days)
- Add Prisma schema for Room, Player, Round, Prompt, Vote, Rating
- Migrate from in-memory to database
- Docker Compose for local Postgres

#### 14. CI/CD & Build Pipeline (2–3 days)
- GitHub Actions: lint, typecheck, test backend on PR
- Unity Cloud Build or GitHub Actions Unity Builder
- SteamCMD upload pipeline for Steam depots

#### 15. Steamworks Integration (3–5 days)
- Install Steamworks.NET
- Steam authentication (optional)
- Achievements: "First Round Completed", "Cut Master", etc.
- Test on Steam Deck and Big Picture mode

#### 16. Controller & Input System (2–3 days)
- Unity Input System (already in progress)
- Gamepad/controller navigation for all UI
- Steam Deck testing

## 📅 Suggested Sprint Plan (MVP in 2–3 weeks)

### Week 1 — Core Game Loop
- Days 1–2: Round start + prompt submission (#1, #2)
- Days 3–4: Prompt voting + performance phase (#3, #4)
- Days 5–7: Cut-vote mechanic + rating phase (#5, #6)

### Week 2 — Multi-Round & Polish
- Days 8–9: Leaderboard + next round flow (#7)
- Days 10–12: Unity UI polish (#8)
- Days 13–14: Player web UI polish + reconnection (#9, #10)

### Week 3 — Testing & Deployment Prep
- Days 15–17: Tests + bug fixes (#12)
- Days 18–19: Topic packs + final playtesting (#11)
- Days 20–21: Persistence (optional) or CI setup (#13, #14)

## 🎯 Immediate Next Steps

1. Create GitHub issues for tasks #1–#7 (core game loop) with acceptance criteria
2. Set up a project board (GitHub Projects or Trello)
3. Start with task #1 (Round Start Flow) — it unblocks everything else
4. After completing #1–#6, do a full playthrough with 3+ people to validate the loop

## 🔧 Development Notes

- **Network Protocol**: Unity host creates rooms; phones connect via WebSocket to backend
- **Authentication**: JWTs signed with `JWT_SECRET` (env var, fallback `dev-secret`)
- **Room Codes**: Format `PP[A-Z2-9]{6}` (validated in `roomManager.ts`)
- **Generated Files**: Don't edit `Assets/InputSystem_Actions.cs` (auto-generated by Unity)
- **Git Pagers**: Use `git --no-pager` in scripts to avoid interactive output issues

## 📚 Additional Documentation

- [Backend README](backend/README.md) - Backend-specific setup and testing
- [Copilot Instructions](.github/copilot-instructions.md) - AI agent development guidelines

## 📝 License

[Add license information here]
