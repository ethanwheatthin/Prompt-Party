import { FastifyInstance } from 'fastify';
import jwt from 'jsonwebtoken';
import { getRoomStateForClient, attachSocketToRoom, rooms, broadcastRoomState, startRound, getRoom, submitPrompt, getRoundWithPrompts, submitVote, tallyVotes } from '../lib/roomManager';

export function setupWebsocket(app: FastifyInstance) {
  app.get('/ws', { websocket: true } as any, (connection, req) => {
    const ws: any = connection.socket;
    const remoteAddr = req.socket.remoteAddress + ':' + req.socket.remotePort;
    app.log.info(`WS connection from ${remoteAddr}`);

    // Store auth context for this socket
    let authenticatedPayload: any = null;

    ws.on('message', (data: any) => {
      try {
        const msg = JSON.parse(data.toString());
        app.log.debug(`WS message from ${remoteAddr}: ${data.toString()}`);
        
        if (msg.type === 'auth') {
          const token = msg.payload?.token;
          if (!token) {
            app.log.warn(`WS auth missing token from ${remoteAddr}`);
            return ws.send(JSON.stringify({ type: 'auth_error', payload: { error: 'missing token' } }));
          }
          try {
            const payload: any = jwt.verify(token, process.env.JWT_SECRET || 'dev-secret');
            app.log.info(`WS auth success from ${remoteAddr} -> roomId=${payload.roomId}, playerId=${payload.playerId}, role=${payload.role}`);
            authenticatedPayload = payload;
            attachSocketToRoom(payload.roomId, payload.playerId, ws);
            const roomState = getRoomStateForClient(payload.roomId);
            const authOk = { type: 'auth_ok', payload: { playerId: payload.playerId, roomState } };
            ws.send(JSON.stringify(authOk));
            app.log.debug(`Sent auth_ok to ${remoteAddr} for player ${payload.playerId}`);
            // broadcast updated room_state to all connected sockets in the room
            try { broadcastRoomState(payload.roomId); } catch (e) { app.log.error(`Failed to broadcast after auth: ${e}`); }
          } catch (e: any) {
            app.log.warn(`WS auth failed from ${remoteAddr}: ${e.message}`);
            ws.send(JSON.stringify({ type: 'auth_error', payload: { error: e.message } }));
          }
        } else if (msg.type === 'host_action') {
          // Handle host actions
          if (!authenticatedPayload) {
            app.log.warn(`WS host_action without auth from ${remoteAddr}`);
            return ws.send(JSON.stringify({ type: 'error', payload: { error: 'not authenticated' } }));
          }
          
          if (authenticatedPayload.role !== 'host') {
            app.log.warn(`WS host_action from non-host ${remoteAddr}`);
            return ws.send(JSON.stringify({ type: 'error', payload: { error: 'unauthorized' } }));
          }
          
          const action = msg.payload?.action;
          if (action === 'start_round') {
            try {
              const room = getRoom(authenticatedPayload.roomId);
              if (!room) {
                return ws.send(JSON.stringify({ type: 'error', payload: { error: 'room not found' } }));
              }
              
              const round = startRound(authenticatedPayload.roomId);
              app.log.info(`Round started: roomId=${authenticatedPayload.roomId}, roundId=${round.roundId}, actorId=${round.actorId}`);
              
              // Broadcast round_started event to all room sockets
              room.players.forEach((p) => {
                if (p.socket && p.socket.readyState === p.socket.OPEN) {
                  p.socket.send(JSON.stringify({
                    type: 'round_started',
                    payload: {
                      roundId: round.roundId,
                      actorId: round.actorId,
                      topic: round.topic,
                      startedAt: round.startedAt,
                      minCutoffAt: round.minCutoffAt,
                      maxEndAt: round.maxEndAt,
                    }
                  }));
                }
              });
              
              // Also broadcast updated room_state with currentRound
              broadcastRoomState(authenticatedPayload.roomId);
            } catch (e: any) {
              app.log.error(`Failed to start round: ${e.message}`);
              ws.send(JSON.stringify({ type: 'error', payload: { error: e.message } }));
            }
          } else {
            app.log.warn(`Unknown host_action: ${action}`);
          }
        } else if (msg.type === 'submit_prompt') {
          // Handle prompt submission from players
          if (!authenticatedPayload) {
            app.log.warn(`WS submit_prompt without auth from ${remoteAddr}`);
            return ws.send(JSON.stringify({ type: 'error', payload: { error: 'not authenticated' } }));
          }
          
          const promptText = msg.payload?.prompt;
          if (!promptText || typeof promptText !== 'string' ) {
            return ws.send(JSON.stringify({ type: 'error', payload: { error: 'invalid prompt' } }));
          }
          
          try {
            submitPrompt(authenticatedPayload.roomId, authenticatedPayload.playerId, promptText);
            app.log.info(`Prompt submitted: playerId=${authenticatedPayload.playerId}, prompt=${promptText.substring(0, 30)}...`);
            
            // Send confirmation to the player
            ws.send(JSON.stringify({ type: 'prompt_accepted', payload: { success: true } }));
            
            // Broadcast updated prompts to all players in the room (especially the host)
            const room = getRoom(authenticatedPayload.roomId);
            if (room) {
              const roundData = getRoundWithPrompts(authenticatedPayload.roomId);
              room.players.forEach((p) => {
                if (p.socket && p.socket.readyState === p.socket.OPEN) {
                  p.socket.send(JSON.stringify({
                    type: 'prompts_update',
                    payload: roundData
                  }));
                }
              });
              
              // If all prompts submitted, send voting_started event
              if (roundData?.allPromptsSubmitted) {
                app.log.info(`All prompts submitted for room ${authenticatedPayload.roomId}, starting voting phase`);
                room.players.forEach((p) => {
                  if (p.socket && p.socket.readyState === p.socket.OPEN) {
                    p.socket.send(JSON.stringify({
                      type: 'voting_started',
                      payload: roundData
                    }));
                  }
                });
              }
            }
          } catch (e: any) {
            app.log.error(`Failed to submit prompt: ${e.message}`);
            ws.send(JSON.stringify({ type: 'error', payload: { error: e.message } }));
          }
        } else if (msg.type === 'submit_vote') {
          // Handle vote submission from players
          if (!authenticatedPayload) {
            app.log.warn(`WS submit_vote without auth from ${remoteAddr}`);
            return ws.send(JSON.stringify({ type: 'error', payload: { error: 'not authenticated' } }));
          }
          
          const promptPlayerId = msg.payload?.promptPlayerId;
          if (!promptPlayerId || typeof promptPlayerId !== 'string') {
            return ws.send(JSON.stringify({ type: 'error', payload: { error: 'invalid vote' } }));
          }
          
          try {
            const allVoted = submitVote(authenticatedPayload.roomId, authenticatedPayload.playerId, promptPlayerId);
            app.log.info(`Vote submitted: voterId=${authenticatedPayload.playerId}, votedFor=${promptPlayerId}`);
            
            // Send confirmation to the voter
            ws.send(JSON.stringify({ type: 'vote_accepted', payload: { success: true } }));
            
            // Broadcast updated vote counts
            const room = getRoom(authenticatedPayload.roomId);
            if (room) {
              const roundData = getRoundWithPrompts(authenticatedPayload.roomId);
              room.players.forEach((p) => {
                if (p.socket && p.socket.readyState === p.socket.OPEN) {
                  p.socket.send(JSON.stringify({
                    type: 'vote_update',
                    payload: roundData
                  }));
                }
              });
              
              // If all votes are in, tally and announce winner
              if (allVoted) {
                const winner = tallyVotes(authenticatedPayload.roomId);
                app.log.info(`All votes submitted for room ${authenticatedPayload.roomId}, winner: ${winner?.promptPlayerId}`);
                
                if (winner) {
                  room.players.forEach((p) => {
                    if (p.socket && p.socket.readyState === p.socket.OPEN) {
                      p.socket.send(JSON.stringify({
                        type: 'prompt_selected',
                        payload: {
                          promptPlayerId: winner.promptPlayerId,
                          promptText: winner.text,
                          votes: winner.votes,
                          playerName: room.players.find(pl => pl.id === winner.promptPlayerId)?.name || 'Unknown',
                          actorId: room.currentRound?.actorId,
                          actorName: room.players.find(pl => pl.id === room.currentRound?.actorId)?.name || 'Unknown',
                          topic: room.currentRound?.topic || 'Unknown'
                        }
                      }));
                    }
                  });
                }
              }
            }
          } catch (e: any) {
            app.log.error(`Failed to submit vote: ${e.message}`);
            ws.send(JSON.stringify({ type: 'error', payload: { error: e.message } }));
          }
        }
      } catch (e) {
        app.log.warn(`WS failed to parse message from ${remoteAddr}`);
      }
    });

    ws.on('close', (code: any, reason: any) => {
      app.log.info(`WS closed ${remoteAddr} code=${code} reason=${reason}`);
      // remove socket references from any players in rooms and broadcast updates
      try {
        for (const [roomId, room] of rooms) {
          let changed = false;
          for (const p of room.players) {
            if (p.socket === ws) { p.socket = null; changed = true; }
          }
          if (changed) {
            try { broadcastRoomState(roomId); } catch (e) { app.log.error(`Failed to broadcast on close: ${e}`); }
          }
        }
      } catch (err) {
        app.log.error(`Error cleaning up socket references: ${err}`);
      }
    });
  });
}
