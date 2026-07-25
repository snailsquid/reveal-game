import { createServer } from "node:http";

const JSON_CONTENT_TYPE = { "content-type": "application/json; charset=utf-8" };

export function createApiServer({ store, gameApiKey, discordStatus, allowedChannelIds, ingestKey }) {
  if (!gameApiKey) {
    throw new Error("GAME_API_KEY is required");
  }

  return createServer(async (request, response) => {
    try {
      const url = new URL(request.url, "http://localhost");
      if (request.method === "GET" && url.pathname === "/health") {
        requireGameAuthorization(request, gameApiKey);
        return sendJson(response, 200, { api: "ready", discord: discordStatus() });
      }

      if (request.method === "POST" && url.pathname === "/sessions") {
        requireGameAuthorization(request, gameApiKey);
        const body = await readJson(request);
        const session = store.create(body);
        return sendJson(response, 201, session);
      }

      const sessionMatch = url.pathname.match(/^\/sessions\/([^/]+)$/);
      if (sessionMatch && request.method === "GET") {
        requireGameAuthorization(request, gameApiKey);
        const session = store.get(sessionMatch[1]);
        return session ? sendJson(response, 200, session) : sendJson(response, 404, { error: "session not found" });
      }

      if (sessionMatch && request.method === "DELETE") {
        requireGameAuthorization(request, gameApiKey);
        store.remove(sessionMatch[1]);
        response.writeHead(204).end();
        return;
      }

      if (request.method === "POST" && url.pathname === "/discord/messages") {
        requireIngestAuthorization(request, ingestKey);
        const body = await readJson(request);
        const matchedSession = store.acceptDiscordMessage({ ...body, allowedChannelIds });
        return sendJson(response, 200, { matched: Boolean(matchedSession), session: matchedSession });
      }

      sendJson(response, 404, { error: "not found" });
    } catch (error) {
      const statusCode = error.statusCode ?? 400;
      sendJson(response, statusCode, { error: error.message });
    }
  });
}

function requireGameAuthorization(request, gameApiKey) {
  if (request.headers["x-game-api-key"] !== gameApiKey) {
    throw httpError(401, "invalid game API key");
  }
}

function requireIngestAuthorization(request, ingestKey) {
  if (!ingestKey || request.headers["x-discord-ingest-key"] !== ingestKey) {
    throw httpError(401, "invalid Discord ingest key");
  }
}

async function readJson(request) {
  const chunks = [];
  for await (const chunk of request) {
    chunks.push(chunk);
  }

  const rawBody = Buffer.concat(chunks).toString("utf8");
  if (!rawBody) {
    throw httpError(400, "JSON request body is required");
  }

  try {
    return JSON.parse(rawBody);
  } catch {
    throw httpError(400, "request body must be valid JSON");
  }
}

function sendJson(response, statusCode, body) {
  response.writeHead(statusCode, JSON_CONTENT_TYPE);
  response.end(JSON.stringify(body));
}

function httpError(statusCode, message) {
  const error = new Error(message);
  error.statusCode = statusCode;
  return error;
}
