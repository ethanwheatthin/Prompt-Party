High Priority — Core Game Loop (MVP critical path)
1. Implement Round Start Flow (Backend + Unity + Player)
Why: Without rounds, there's no game. This unlocks the core loop.

Backend tasks:

 Add host_action WebSocket handler for "start_round" action
 Implement round creation logic: select actor (rotate through connected players), generate roundId, set timestamps (startedAt, minCutoffAt = +30s, maxEndAt = +90s)
 Broadcast round_started event to all room sockets with { roundId, actorId, topic, startedAt, minCutoffAt, maxEndAt }
 Add currentRound field to room_state payload
 Unit test: round creation, actor selection rotation, timestamp calculation
Unity tasks:

 Add "Start Round" button in Lobby (visible only to host)
 Wire button to send host_action { action: "start_round" } via NetworkManager
 Create RoundScene (or RoundView panel)
 Display: current round topic, actor name, countdown timer (sync with server startedAt and maxEndAt)
 Show "Waiting for prompts..." status text
Player web tasks:

 Listen for round_started event
 Hide lobby, show round UI
 Display: round topic, timer, "You are the actor!" banner if playerId === actorId
 Show prompt submission form (input + submit button) for non-actor players
Acceptance: Host clicks Start Round → backend creates round, broadcasts round_started → Unity shows round UI with timer → phones show round topic and prompt form (or actor banner)

Estimate: 1–2 days

2. Implement Prompt Submission Phase (Backend + Player Web)
Why: Core mechanic — players write prompts for the actor.

Backend tasks:

 Add submit_prompt WebSocket handler: { roundId, text }
 Validate: round exists, round not ended, player is not actor, prompt text length (10–200 chars), one prompt per player per round
 Store prompt in round: { promptId, playerId, text, votes:  0 }
 Broadcast prompt_submitted event (or aggregate into room_state update with prompts array)
 Unit test: prompt validation, duplicate submission rejection
Player web tasks:

 Prompt submission form (textarea + submit button)
 POST or WebSocket submit_prompt { roundId, text }
 Show "Prompt submitted ✓" feedback and disable form after submit
 Show loading/error states
Unity tasks:

 Display "Waiting for prompts..." and live count of submitted prompts
 Optional: show prompt preview list (or keep hidden until voting)
Acceptance: Non-actor player submits a prompt → backend stores it → Unity sees prompt count increment → player sees confirmation

Estimate: 1 day

3. Implement Prompt Voting Phase (Backend + Player Web + Unity)
Why: Players vote on the best prompt; winner is chosen for actor to perform.

Backend tasks:

 Add vote_prompt WebSocket handler: { roundId, promptId }
 Validate: player is not actor, one vote per player, promptId exists
 Increment vote count on prompt
 When all non-actor players voted (or timer expires), calculate winner (most votes, tiebreaker: random or first)
 Broadcast voting_started { prompts: [] } and voting_ended { winningPromptId, prompt } events
 Transition to performance phase automatically
 Unit test: vote tallying, tiebreaker, winner selection
Player web tasks:

 Listen for voting_started, display list of prompts (anonymized or with author names)
 Render vote buttons for each prompt
 Send vote_prompt { roundId, promptId } on click
 Show "Vote cast ✓" feedback, disable voting UI
 Listen for voting_ended and show winning prompt
Unity tasks:

 Display prompt list during voting phase
 Highlight winning prompt when voting_ended received
 Transition to performance phase UI
Acceptance: All players vote → backend picks winner → Unity + phones display winning prompt → round advances to performance

Estimate: 1–2 days

4. Implement Performance Phase (Backend + Unity + Player)
Why: Actor performs; players watch timer and can vote to cut.

Backend tasks:

 Broadcast performance_started { roundId, winningPrompt, actorId, startedAt, minCutoffAt, maxEndAt }
 Start server-side timer; auto-end performance at maxEndAt if not cut or actor-ended
 Track performance state in round
Unity tasks:

 Performance view: display winning prompt large (for actor to read)
 Show countdown timer (synced with server maxEndAt)
 Display "Cut available in X seconds" until minCutoffAt, then enable cut vote button for non-actors
 Actor sees "End Performance" button (voluntary end)
Player web tasks:

 Show performance phase UI: winning prompt, timer
 Actor: show "End Performance" button → send actor_end_performance { roundId }
 Non-actors: show "Cut" button (disabled until minCutoffAt, then enabled) → send cast_cut_vote { roundId }
 Display cut vote count / threshold live
Acceptance: Performance starts → Unity shows prompt + timer → phones show cut button (disabled, then enabled after 30s) → actor or players can end performance

Estimate: 1–2 days

5. Implement Cut-Vote Mechanic (Backend + Unity + Player)
Why: Signature feature — crowd can cut off a bad performance.

Backend tasks:

 Add cast_cut_vote WebSocket handler
 Validate: time >= minCutoffAt, player is not actor, player hasn't voted to cut already
 Add voter to round. cutVotes set
 Calculate threshold: Math.ceil(activeNonActorCount * 0.5)
 Broadcast cut_vote_update { cutVotesCount, cutThreshold, voters[] } after each vote
 When threshold reached: end performance, broadcast performance_cut { roundId, endedAt, reason:  "votes" }
 Transition to rating phase
 Unit test: threshold calculation, early vote rejection, threshold trigger
Unity tasks:

 Show live cut vote ticker: "Cut votes: 2 / 3"
 When performance_cut received, show "PERFORMANCE CUT!" animation/message
 Transition to rating phase
Player web tasks:

 Enable cut button after minCutoffAt (calculate client-side from server timestamp)
 Show live cut vote count
 When performance_cut received, show feedback and transition to rating
Acceptance: Two non-actor players press Cut after 30s → backend detects threshold → broadcasts performance_cut → Unity + phones show cut message and advance to rating

Estimate: 1 day

6. Implement Rating Phase (Backend + Unity + Player)
Why: Players rate the actor's performance; scores are calculated.

Backend tasks:

 Broadcast rating_phase_start { roundId, durationSeconds }
 Add submit_rating WebSocket handler: { roundId, rating } (1–10 scale)
 Validate: player is not actor, one rating per player
 Store ratings in round
 When all players rated (or timer expires), calculate actor score (average or weighted)
 Update leaderboard/scores
 Broadcast round_ended { roundId, actorScore, ratings, leaderboard }
 Unit test: rating aggregation, score calculation
Unity tasks:

 Show rating phase UI: "Rate the performance!"
 Display rating input (1–10 stars or slider) for non-actor players
 Actor sees "Waiting for ratings..."
 When round_ended received, show actor score and leaderboard
Player web tasks:

 Show rating UI (1–10 buttons or slider)
 Send submit_rating { roundId, rating } on submit
 Show "Rating submitted ✓"
 When round_ended received, show actor score and leaderboard
Acceptance: All players submit ratings → backend calculates score → Unity + phones display score and leaderboard

Estimate: 1–2 days

7. Implement Leaderboard & Multi-Round Flow (Backend + Unity + Player)
Why: Track scores across rounds; allow host to start next round.

Backend tasks:

 Store cumulative scores per player across rounds
 Add host_action handler for "next_round" to reset round state and start new round (rotate actor)
 Include leaderboard in room_state and round_ended payloads
Unity tasks:

 Leaderboard view/panel showing player names and scores (sorted)
 Host sees "Next Round" button after round_ended
 Send host_action { action: "next_round" }
Player web tasks:

 Display leaderboard between rounds
 Show "Waiting for host to start next round..."
Acceptance: Round ends → leaderboard shown → host clicks Next Round → new round starts with rotated actor

Estimate: 1 day

Medium Priority — Polish & UX
8. Unity Host UI Polish
 Improve Lobby UI: grid layout for player list, player avatars (initials or colors), connection status indicators
 Add room code display with large font (for in-room visibility)
 Add QR code display in Lobby (use existing joinUrl)
 Create separate scenes or panels: Lobby, Round (with phases), Leaderboard
 Add animations/transitions between phases
 Show phase labels clearly: "Prompt Submission", "Voting", "Performance", "Rating"
 Display server connection status and reconnect UI
Estimate: 2–3 days

9. Player Web UI Polish
 Improve lobby UI: show room code, host name, player count
 Add loading spinners and error toasts
 Add sound effects or vibration feedback on actions (submit prompt, vote, cut)
 Improve prompt submission: character count, placeholder examples
 Voting UI: card-based layout, highlight selected vote
 Performance phase: show actor name, animated timer
 Rating UI: star rating component or emoji slider
 Responsive design refinement for small and large phones
Estimate: 2–3 days

10. Reconnection & Presence Handling
Why: Players/host might disconnect mid-game.

Backend tasks:

 Issue session tokens (refresh or long-lived) so players can reconnect
 Store lastSeen timestamp and mark players disconnected after timeout
 Allow reconnection: if token valid and player exists, reattach socket
 Broadcast room_state on reconnect with latest round/phase
Unity tasks:

 Detect WebSocket disconnect, show "Reconnecting..." UI
 Auto-reconnect with exponential backoff
 Re-send auth token on reconnect
Player web tasks:

 Same reconnect logic as Unity
 Show "Connection lost" banner, attempt reconnect
Acceptance: Player disconnects mid-round → backend marks disconnected → player reconnects → rejoins at current phase

Estimate: 1–2 days

11. Add Topic Selection / Prompt Packs
Why: Variety in prompts.

Backend tasks:

 Create a simple topic/pack JSON file or in-memory array
 Randomly select topic on round start (or allow host to pick)
 Include topic name/description in round_started
Unity tasks:

 Show topic name in round UI
 Optional: host can choose topic before starting round
Acceptance: Each round shows a different topic (e.g., "At the DMV", "Space Station", "Wedding")

Estimate: 0.5–1 day

Lower Priority — Testing, DevOps, Future Features
12. Unit & Integration Tests
 Backend: unit tests for game logic (round lifecycle, cut threshold, voting, rating)
 Backend: integration tests for WebSocket flows (mock sockets, simulate join/vote/cut)
 Unity: use Unity Test Runner for NetworkManager and RoomStateController logic
Estimate: 2–3 days

13. Persistence (Postgres + Prisma)
Why: Save rooms, rounds, scores for analytics or replay.

 Add Prisma schema for Room, Player, Round, Prompt, Vote, Rating
 Migrate from in-memory to DB
 Add Docker Compose for local Postgres
Estimate: 2–3 days

14. CI/CD & Build Pipeline
 GitHub Actions: lint, typecheck, test backend on PR
 Unity Cloud Build or GitHub Actions Unity Builder for desktop builds
 SteamCMD upload pipeline for Steam depots (later)
Estimate: 2–3 days

15. Steamworks Integration (Unity)
 Install Steamworks. NET
 Implement Steam authentication (optional for MVP)
 Add achievements: "First Round Completed", "Cut Master" (5 cuts), etc.
 Test on Steam Deck and Big Picture mode
Estimate: 3–5 days

16. Controller & Input System (Unity)
 Replace legacy input with Unity Input System
 Add gamepad/controller navigation for all UI
 Test on Steam Deck
Estimate: 2–3 days

Suggested Sprint Plan (MVP in ~2–3 weeks)
Week 1 — Core Game Loop:

Days 1–2: Round start + prompt submission (#1, #2)
Days 3–4: Prompt voting + performance phase (#3, #4)
Days 5–7: Cut-vote mechanic + rating phase (#5, #6)
Week 2 — Multi-Round & Polish:

Days 8–9: Leaderboard + next round flow (#7)
Days 10–12: Unity UI polish (#8)
Days 13–14: Player web UI polish + reconnection (#9, #10)
Week 3 — Testing & Deployment Prep:

Days 15–17: Tests + bug fixes (#12)
Days 18–19: Topic packs + final playtesting (#11)
Days 20–21: Persistence (optional) or CI setup (#13, #14)
How to Track This
Option A: GitHub Issues + Project Board
Create issues for each task above (use the checkboxes as subtasks or separate issues)
Label them: backend, unity, player-web, MVP, polish, testing
Use GitHub Projects (Kanban board): columns: Backlog, To Do, In Progress, Review, Done
Assign priorities: P0 (critical), P1 (high), P2 (medium), P3 (nice-to-have)
Option B: Simple Markdown Checklist (lightweight)
Create a MVP_CHECKLIST.md in your repo root and copy the tasks above. Check them off as you complete.

Option C: Trello / Notion / Linear
Copy tasks into cards/pages and organize by sprint.

Immediate Next Steps (do these today)
Create GitHub issues for tasks #1–#7 (core game loop) with acceptance criteria
Set up a project board (GitHub Projects or Trello)
Start with task #1 (Round Start Flow) — it unblocks everything else
Timebox playtesting: after completing #1–#6, do a full playthrough with 3+ people (you + 2 phones) to validate the loop
