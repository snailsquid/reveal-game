import { Client, GatewayIntentBits } from "discord.js";
import { createServer } from "node:http";
import { randomUUID } from "node:crypto";

const DISCORD_BOT_TOKEN = process.env.DISCORD_BOT_TOKEN ?? "";
const GAME_API_KEY = process.env.GAME_API_KEY ?? "";
const API_PORT = parseInt(process.env.API_PORT ?? "3000", 10);

const configuredChannelIds = (process.env.DISCORD_ALLOWED_CHANNEL_IDS
  ?? process.env.DISCORD_ALLOWED_CHANNEL_ID
  ?? process.env.DISCORD_CHANNEL_ID
  ?? "")
  .split(",")
  .map((id) => id.trim())
  .filter(Boolean);

if (!DISCORD_BOT_TOKEN) {
  console.error("DISCORD_BOT_TOKEN is required. Set it in discord-listener/.env.");
  process.exit(1);
}

if (!configuredChannelIds.length) {
  console.error("At least one Discord channel ID is required in DISCORD_ALLOWED_CHANNEL_IDS.");
  process.exit(1);
}

const sessions = new Map();

const client = new Client({
  intents: [
    GatewayIntentBits.Guilds,
    GatewayIntentBits.GuildMessages,
    GatewayIntentBits.MessageContent,
  ],
});

function isChannelVisible() {
  return configuredChannelIds.some((channelId) => client.channels.cache.has(channelId));
}

function sendJson(response, statusCode, value) {
  response.writeHead(statusCode, {
    "Content-Type": "application/json",
    "Access-Control-Allow-Origin": "*",
  });
  response.end(JSON.stringify(value));
}

function isAuthorized(request) {
  return Boolean(GAME_API_KEY) && request.headers["x-game-api-key"] === GAME_API_KEY;
}

function readJson(request) {
  return new Promise((resolve, reject) => {
    let body = "";
    request.setEncoding("utf8");
    request.on("data", (chunk) => {
      body += chunk;
      if (body.length > 64 * 1024) {
        reject(new Error("Request body is too large."));
        request.destroy();
      }
    });
    request.on("end", () => {
      try {
        resolve(body ? JSON.parse(body) : {});
      } catch {
        reject(new Error("Request body must be valid JSON."));
      }
    });
    request.on("error", reject);
  });
}

function serializeSession(session) {
  return {
    sessionId: session.sessionId,
    status: session.status,
    matchedBy: session.matchedBy ?? null,
  };
}

client.once("clientReady", () => {
  console.log(`Logged in as ${client.user.tag}`);
  const visible = configuredChannelIds.filter((id) => client.channels.cache.has(id));
  if (!visible.length) {
    console.error("The bot cannot see any configured Discord channel. Check the channel IDs and permissions.");
  } else {
    console.log(`Watching ${visible.length} configured Discord channel(s).`);
  }
});

client.on("messageCreate", (message) => {
  if (message.author.bot || !configuredChannelIds.includes(message.channelId)) return;

  const guess = message.content.trim();
  if (!guess) return;

  for (const session of sessions.values()) {
    if (session.status !== "listening") continue;

    const expected = session.answer.trim();
    const matches = session.isCaseSensitive
      ? guess === expected
      : guess.localeCompare(expected, undefined, { sensitivity: "accent" }) === 0;
    if (!matches) continue;

    session.status = "matched";
    session.matchedBy = { username: message.author.username, guess };
    console.log(`[${new Date().toISOString()}] ${message.author.username} matched session ${session.sessionId}.`);
    break;
  }
});

const server = createServer(async (request, response) => {
  response.setHeader("Access-Control-Allow-Origin", "*");
  response.setHeader("Access-Control-Allow-Methods", "GET, POST, DELETE, OPTIONS");
  response.setHeader("Access-Control-Allow-Headers", "Content-Type, X-Game-Api-Key");

  if (request.method === "OPTIONS") {
    response.writeHead(204);
    response.end();
    return;
  }

  if (!isAuthorized(request)) {
    sendJson(response, 401, { error: "Unauthorized" });
    return;
  }

  if (request.method === "GET" && request.url === "/health") {
    sendJson(response, 200, {
      api: "ready",
      discord: {
        connected: client.isReady(),
        channelVisible: client.isReady() && isChannelVisible(),
      },
    });
    return;
  }

  if (request.method === "POST" && request.url === "/sessions") {
    try {
      const body = await readJson(request);
      if (typeof body.answer !== "string" || !body.answer.trim()) {
        sendJson(response, 400, { error: "answer is required" });
        return;
      }

      const session = {
        sessionId: randomUUID(),
        answer: body.answer,
        isCaseSensitive: Boolean(body.isCaseSensitive),
        hintId: typeof body.hintId === "string" ? body.hintId : "",
        status: "listening",
        matchedBy: null,
      };
      sessions.set(session.sessionId, session);
      sendJson(response, 201, serializeSession(session));
    } catch (error) {
      sendJson(response, 400, { error: error.message });
    }
    return;
  }

  const sessionMatch = request.url?.match(/^\/sessions\/([^/?]+)$/);
  if (sessionMatch) {
    const session = sessions.get(sessionMatch[1]);
    if (!session) {
      sendJson(response, 404, { error: "Session not found" });
      return;
    }

    if (request.method === "GET") {
      sendJson(response, 200, serializeSession(session));
      return;
    }

    if (request.method === "DELETE") {
      sessions.delete(session.sessionId);
      sendJson(response, 200, { status: "cancelled" });
      return;
    }
  }

  sendJson(response, 404, { error: "Not found" });
});

server.listen(API_PORT, () => {
  console.log(`API listening on http://localhost:${API_PORT}`);
});

client.login(DISCORD_BOT_TOKEN);
