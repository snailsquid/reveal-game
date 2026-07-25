# Reveal Game Discord Listener

This service owns Discord answer matching. Unity starts a session only while a hint is on screen, polls that session, and never accepts player input through the game UI.

## Run

1. Copy `.env.example` to `.env` and set `GAME_API_KEY` to a long random secret.
2. For direct Discord integration, set `DISCORD_BOT_TOKEN` and enable the **Message Content Intent** for the bot in Discord's Developer Portal. Optionally lock answers to `DISCORD_ALLOWED_CHANNEL_ID`.
3. Run `npm install`, then `npm start`.

The Unity `GameManager` inspector must use the same `GAME_API_KEY` and the service URL, for example `http://localhost:3000` in the Editor.

## API

All game routes require `X-Game-Api-Key`.

- `GET /health` returns API and Discord listener readiness.
- `POST /sessions` accepts `{ "answer", "isCaseSensitive", "hintId" }` and returns a listening session.
- `GET /sessions/:sessionId` returns `listening` or `matched`; matched sessions include `matchedBy.username` and `matchedBy.guess`.
- `DELETE /sessions/:sessionId` cancels a hint when the game leaves it.

### Existing bot relay mode

If another service already receives Discord messages, set `DISCORD_INGEST_KEY` and POST each message to `/discord/messages` with `X-Discord-Ingest-Key`:

```json
{
  "username": "Discord display name",
  "userId": "Discord user ID",
  "channelId": "Discord channel ID",
  "content": "the player guess"
}
```

In relay mode, this service still owns matching and sends no inbound traffic to the Unity client. Deploy it behind HTTPS for any non-local game.
