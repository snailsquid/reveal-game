# Reveal Game Discord Listener

This is the game's working Discord bot listener. It connects to one configured Discord channel, ignores bot messages, and saves the latest non-empty message to `state.json`.

## Run

1. Copy `.env.example` to `.env`.
2. Set `DISCORD_BOT_TOKEN`, `DISCORD_GUILD_ID`, and `DISCORD_CHANNEL_ID`.
3. Enable the bot's **Message Content Intent** in the Discord Developer Portal and invite the bot to the configured server/channel.
4. Run `npm install`, then `npm start`.

The listener exposes `GET /state` on `http://localhost:3100` by default. It returns either `{ "status": "idle" }` or the most recently received answer with its timestamp.
