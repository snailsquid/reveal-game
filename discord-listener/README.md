# Reveal Game Discord Listener

This is the game's Discord bot listener. The Unity desktop build starts it as a child process and communicates with it over a private local HTTP API.

## Run

1. Copy `.env.example` to `.env` if it does not already exist.
2. Set `DISCORD_BOT_TOKEN` and `DISCORD_ALLOWED_CHANNEL_IDS`.
3. Enable the bot's **Message Content Intent** in the Discord Developer Portal and invite the bot to the configured server/channel.
4. Run `npm install` in this directory before making a build.

When the game starts, `DiscordListenerProcessLauncher` runs `node src/index.js` automatically from the bundled listener directory. The listener exposes authenticated `/health` and `/sessions` endpoints on `http://localhost:3000`, matching the Unity client.
