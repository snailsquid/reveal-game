export async function startDiscordGateway({ token, store, allowedChannelIds, setConnected }) {
  if (!token) {
    return null;
  }

  const { Client, GatewayIntentBits } = await import("discord.js");
  const client = new Client({
    intents: [GatewayIntentBits.Guilds, GatewayIntentBits.GuildMessages, GatewayIntentBits.MessageContent]
  });

  client.once("ready", () => {
    setConnected(true);
    console.log("Discord gateway connected.");
  });

  client.on("disconnect", () => setConnected(false));
  client.on("error", (error) => {
    setConnected(false);
    console.error("Discord gateway error:", error.message);
  });

  client.on("messageCreate", (message) => {
    if (message.author.bot) {
      return;
    }

    console.log(`Discord message from ${message.member?.displayName ?? message.author.globalName ?? message.author.username}: ${message.content}`);

    const matched = store.acceptDiscordMessage({
      username: message.member?.displayName ?? message.author.globalName ?? message.author.username,
      userId: message.author.id,
      channelId: message.channelId,
      content: message.content,
      allowedChannelIds
    });

    if (matched) {
      console.log(`Matched Discord answer for hint ${matched.hintId || "(unnamed)"}.`);
    }
  });

  await client.login(token);
  return client;
}
