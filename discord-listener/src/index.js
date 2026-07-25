import { Client, GatewayIntentBits } from "discord.js";
import { createServer } from "node:http";
import { readFileSync, writeFileSync, existsSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));

const DISCORD_BOT_TOKEN = process.env.DISCORD_BOT_TOKEN ?? "";
const DISCORD_CHANNEL_ID = process.env.DISCORD_CHANNEL_ID ?? "";
const DISCORD_GUILD_ID = process.env.DISCORD_GUILD_ID ?? "";
const API_PORT = parseInt(process.env.API_PORT ?? "3100", 10);

if (!DISCORD_BOT_TOKEN) {
  console.error("DISCORD_BOT_TOKEN is required. Copy it from https://discord.com/developers/applications > your app > Bot > Reset Token.");
  process.exit(1);
}
if (!DISCORD_GUILD_ID) {
  console.error("DISCORD_GUILD_ID is required. Right-click your server name in Discord > Copy Server ID (enable Developer Mode in Settings > Advanced).");
  process.exit(1);
}
if (!DISCORD_CHANNEL_ID) {
  console.error("DISCORD_CHANNEL_ID is required. Right-click the target channel in Discord > Copy Channel ID (enable Developer Mode in Settings > Advanced).");
  process.exit(1);
}

const STATE_FILE = join(__dirname, "..", "state.json");

function loadState() {
  if (!existsSync(STATE_FILE)) return { status: "idle" };
  try {
    return JSON.parse(readFileSync(STATE_FILE, "utf8"));
  } catch {
    return { status: "idle" };
  }
}

function saveState(state) {
  writeFileSync(STATE_FILE, JSON.stringify(state, null, 2));
}

const client = new Client({
  intents: [
    GatewayIntentBits.Guilds,
    GatewayIntentBits.GuildMessages,
    GatewayIntentBits.MessageContent,
  ],
});

client.once("clientReady", () => {
  console.log(`Logged in as ${client.user.tag}`);

  const guild = client.guilds.cache.get(DISCORD_GUILD_ID);
  if (!guild) {
    console.error(`Guild ${DISCORD_GUILD_ID} not found. Make sure the bot is invited to this server.`);
    process.exit(1);
  }

  const channel = guild.channels.cache.get(DISCORD_CHANNEL_ID);
  if (!channel) {
    console.error(`Channel ${DISCORD_CHANNEL_ID} not found in guild ${DISCORD_GUILD_ID}. Make sure the bot has access to this channel.`);
    process.exit(1);
  }

  console.log(`Watching channel: #${channel.name}`);
});

client.on("messageCreate", (message) => {
  if (message.author.bot) return;
  if (message.channelId !== DISCORD_CHANNEL_ID) return;

  const content = message.content.trim();
  if (!content) return;

  console.log(`[${new Date().toISOString()}] ${message.author.username}: ${content}`);
  saveState({ status: "answered", answer: content, timestamp: Date.now() });
});

client.login(DISCORD_BOT_TOKEN);

const server = createServer((req, res) => {
  res.setHeader("Access-Control-Allow-Origin", "*");
  res.setHeader("Access-Control-Allow-Methods", "GET, OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type");

  if (req.method === "OPTIONS") {
    res.writeHead(204);
    res.end();
    return;
  }

  if (req.url === "/state") {
    res.writeHead(200, { "Content-Type": "application/json" });
    res.end(JSON.stringify(loadState()));
    return;
  }

  res.writeHead(404);
  res.end("Not found");
});

server.listen(API_PORT, () => {
  console.log(`API listening on http://localhost:${API_PORT}`);
});
