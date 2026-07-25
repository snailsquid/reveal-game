import { randomUUID } from "node:crypto";

const MAX_ANSWER_LENGTH = 200;

export class SessionStore {
  #sessions = new Map();

  create({ answer, isCaseSensitive, hintId }) {
    if (typeof answer !== "string" || answer.trim().length === 0 || answer.length > MAX_ANSWER_LENGTH) {
      throw new Error("answer must be a non-empty string up to 200 characters");
    }

    const session = {
      id: randomUUID(),
      answer,
      hintId: typeof hintId === "string" ? hintId : "",
      isCaseSensitive: Boolean(isCaseSensitive),
      status: "listening",
      createdAt: new Date().toISOString(),
      matchedBy: null
    };

    this.#sessions.set(session.id, session);
    return this.#toPublicSession(session);
  }

  get(sessionId) {
    const session = this.#sessions.get(sessionId);
    return session ? this.#toPublicSession(session) : null;
  }

  remove(sessionId) {
    return this.#sessions.delete(sessionId);
  }

  acceptDiscordMessage({ username, userId, channelId, content, allowedChannelIds }) {
    if (typeof content !== "string" || typeof username !== "string" || username.trim().length === 0) {
      throw new Error("username and content are required");
    }

    if (allowedChannelIds && allowedChannelIds.length > 0 && !allowedChannelIds.includes(channelId)) {
      return null;
    }

    const guess = content.trim();
    const session = [...this.#sessions.values()].find((candidate) =>
      candidate.status === "listening" && this.#answersMatch(candidate, guess));

    if (!session) {
      return null;
    }

    session.status = "matched";
    session.matchedBy = {
      username: username.trim(),
      userId: typeof userId === "string" ? userId : "",
      guess,
      matchedAt: new Date().toISOString()
    };

    return this.#toPublicSession(session);
  }

  #answersMatch(session, guess) {
    return session.isCaseSensitive
      ? session.answer === guess
      : session.answer.localeCompare(guess, undefined, { sensitivity: "accent" }) === 0;
  }

  #toPublicSession(session) {
    return {
      sessionId: session.id,
      hintId: session.hintId,
      status: session.status,
      createdAt: session.createdAt,
      matchedBy: session.matchedBy
    };
  }
}
