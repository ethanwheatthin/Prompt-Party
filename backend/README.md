# Prompt Party Backend

Development backend for PromptParty.

## Testing Player Join (Phone)
1. Start server: `npm run dev`
2. Unity host creates room and displays join code
3. Phone: open http://<YOUR_LAN_IP>:3000/player.html
4. Enter room code and name, press Join
5. Phone should show lobby with connected players
6. Unity should see new player in player list

Questions to clarify before starting (ask me if needed)
- Should we auto-uppercase the room code in the frontend or rely on server normalization?
- Should players be able to rejoin with the same name if disconnected, or always create a new player record?
- Do you want a "Leave" button in the lobby or just rely on closing the browser tab?

