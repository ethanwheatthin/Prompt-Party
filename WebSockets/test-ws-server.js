// Requires: npm i ws
const WebSocket = require('ws');

const wss = new WebSocket.Server({ port: 8080 }, () => console.log('WS server listening on ws://localhost:8080'));

wss.on('connection', (ws) => {
    console.log('client connected');

    ws.on('message', (message) => {
        console.log('received:', message.toString());
        try {
            const env = JSON.parse(message);
            if (env.type === 'auth') {
                // respond with auth_ok and a sample room state
                ws.send(JSON.stringify({
                    type: 'auth_ok',
                    payload: {
                        playerId: 'host-1',
                        roomState: {
                            roomId: 'room-123',
                            joinCode: 'PP123',
                            players: [
                                { id: 'host-1', name: 'Host (Unity)' },
                                { id: 'p1', name: 'Alice' },
                                { id: 'p2', name: 'Bob' }
                            ],
                            currentRound: null
                        }
                    }
                }));

                // emit a round_started after 3s
                setTimeout(() => {
                    ws.send(JSON.stringify({
                        type: 'round_started',
                        payload: {
                            roundId: 'round-1',
                            actorId: 'p1',
                            startedAt: new Date().toISOString(),
                            minCutoffAt: new Date(Date.now() + 30_000).toISOString(),
                            maxEndAt: new Date(Date.now() + 90_000).toISOString()
                        }
                    }));
                }, 3000);

                // emit cut vote updates periodically (demo)
                let votes = 0;
                const interval = setInterval(() => {
                    votes++;
                    ws.send(JSON.stringify({
                        type: 'cut_vote_update',
                        payload: {
                            roundId: 'round-1',
                            cutVotesCount: votes,
                            cutThreshold: 2,
                            voters: ['p2']
                        }
                    }));
                    if (votes >= 2) {
                        ws.send(JSON.stringify({
                            type: 'performance_cut',
                            payload: {
                                roundId: 'round-1',
                                endedAt: new Date().toISOString(),
                                reason: 'votes'
                            }
                        }));
                        clearInterval(interval);
                    }
                }, 8000);
            }

            if (env.type === 'host_action') {
                console.log('host_action:', env.payload);
                // echo back a simple room_state update
                ws.send(JSON.stringify({
                    type: 'room_state',
                    payload: {
                        roomId: 'room-123',
                        joinCode: 'PP123',
                        players: [
                            { id: 'host-1', name: 'Host (Unity)' },
                            { id: 'p1', name: 'Alice' },
                            { id: 'p2', name: 'Bob' },
                            { id: 'p3', name: 'Carol' }
                        ],
                        currentRound: null
                    }
                }));
            }
        } catch (e) {
            console.error('invalid message', e);
        }
    });

    ws.on('close', () => console.log('client disconnected'));
});