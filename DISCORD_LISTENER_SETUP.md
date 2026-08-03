# Private Discord listener setup

The Unity game starts the local listener automatically in the Editor and in desktop builds.

1. In `discord-listener/.env`, set `DISCORD_BOT_TOKEN` and `DISCORD_ALLOWED_CHANNEL_IDS` (comma-separated).
2. In the `GameManager` inspector, set `_discordServerApiKey` to any private random value. The launcher passes this value to Node automatically.
3. Leave `_discordServerUrl` as `http://localhost:3000`.
4. Press Play or make a Windows, macOS, or Linux desktop build.

In the Editor, the launcher discovers the sibling `discord-listener` directory automatically. Before a desktop build, Unity copies the listener, its installed dependencies, and the `.env` file when present into `Assets/StreamingAssets/discord-listener`. The built game launches the listener automatically when it starts. Node.js 22+ must be installed on the computer running the build unless a bundled Node executable is configured in the launcher.

Node.js 22+ and the listener dependencies must be installed in `discord-listener/` before building:

```bash
cd discord-listener
npm install
```

The Discord bot token is private-use configuration and is copied into the private build. Do not distribute that build or commit `.env`.
