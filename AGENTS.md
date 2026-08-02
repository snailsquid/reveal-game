# Description

This is a unity project for a game with the main goal being to gacha or open cases/lootboxes in a way.

# Rules

Everything must follow best practices of Unity and C#.
Make best use of prefabs whenever fitting.
Always make sure unity compiles
Verify Unity projects with the Unity MCP, not dotnet. If for whatever reason you cannot verify with Unity MCP ask the user to turn it back on instead of continuing
Use TextTMP instead of plain Text component for texts

# Glossary

Lootbox: The item being opened to gacha
Item: The reward of the gacha from lootbox

1. Game creation
   The game for the specified config will be generated.
2. Game play
   a. Title screen Play (Title screen -> Lootboxes Screen)
   b. Pick a lootbox (Lootboxes Screen -> Lootbox Open Screen)
   c. Player is given a randomized (new) hint game
   d. Player guesses hint correctly (See # Future Mechanic)
   e. Shows a big popup text "Player Answered Correctly\n'<answer>'"
   f. Lootbox spins
   g. Lootbox picks winner(s)
   h. Goes back to Lootboxes Screen, lootbox greyed out, winner(s) are shown on top of the lootbox

<!-- CODEGRAPH_START -->

## CodeGraph

In repositories indexed by CodeGraph (a `.codegraph/` directory exists at the repo root), reach for it BEFORE grep/find or reading files when you need to understand or locate code:

- **MCP tool** (when available): `codegraph_explore` answers most code questions in one call — the relevant symbols' verbatim source plus the call paths between them, including dynamic-dispatch hops grep can't follow. Name a file or symbol in the query to read its current line-numbered source. If it's listed but deferred, load it by name via tool search.
- **Shell** (always works): `codegraph explore "<symbol names or question>"` prints the same output.

If there is no `.codegraph/` directory, skip CodeGraph entirely — indexing is the user's decision.

<!-- CODEGRAPH_END -->
