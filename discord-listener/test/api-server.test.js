import assert from "node:assert/strict";
import test from "node:test";
import { createApiServer } from "../src/api-server.js";
import { SessionStore } from "../src/session-store.js";

const gameApiKey = "test-game-key";
const ingestKey = "test-ingest-key";

async function withServer(run) {
  const store = new SessionStore();
  const server = createApiServer({
    store,
    gameApiKey,
    ingestKey,
    allowedChannelId: "hint-channel",
    discordStatus: () => ({ mode: "relay", connected: true })
  });
  await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
  const { port } = server.address();

  try {
    await run({ baseUrl: `http://127.0.0.1:${port}`, store });
  } finally {
    await new Promise((resolve, reject) => server.close((error) => error ? reject(error) : resolve()));
  }
}

function gameHeaders() {
  return { "x-game-api-key": gameApiKey, "content-type": "application/json" };
}

test("health requires the game key and reports Discord readiness", async () => {
  await withServer(async ({ baseUrl }) => {
    const unauthorized = await fetch(`${baseUrl}/health`);
    assert.equal(unauthorized.status, 401);

    const authorized = await fetch(`${baseUrl}/health`, { headers: gameHeaders() });
    assert.equal(authorized.status, 200);
    assert.deepEqual(await authorized.json(), { api: "ready", discord: { mode: "relay", connected: true } });
  });
});

test("a matching Discord message resolves only the active hint session", async () => {
  await withServer(async ({ baseUrl }) => {
    const created = await fetch(`${baseUrl}/sessions`, {
      method: "POST",
      headers: gameHeaders(),
      body: JSON.stringify({ answer: "Dragon", isCaseSensitive: false, hintId: "hint-1" })
    });
    assert.equal(created.status, 201);
    const session = await created.json();

    const wrongChannel = await fetch(`${baseUrl}/discord/messages`, {
      method: "POST",
      headers: { "x-discord-ingest-key": ingestKey, "content-type": "application/json" },
      body: JSON.stringify({ username: "Ari", userId: "1", channelId: "other", content: "dragon" })
    });
    assert.deepEqual(await wrongChannel.json(), { matched: false, session: null });

    const match = await fetch(`${baseUrl}/discord/messages`, {
      method: "POST",
      headers: { "x-discord-ingest-key": ingestKey, "content-type": "application/json" },
      body: JSON.stringify({ username: "Ari", userId: "1", channelId: "hint-channel", content: "  dragon  " })
    });
    assert.equal((await match.json()).matched, true);

    const resolved = await fetch(`${baseUrl}/sessions/${session.sessionId}`, { headers: gameHeaders() });
    const body = await resolved.json();
    assert.equal(body.status, "matched");
    assert.deepEqual(body.matchedBy, { username: "Ari", userId: "1", guess: "dragon", matchedAt: body.matchedBy.matchedAt });
  });
});
