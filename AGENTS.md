# Description

This is a unity project for a game with the main goal being to gacha or open cases/lootboxes in a way.

# Rules

Everything must follow best practices of Unity and C#.
Make best use of prefabs whenever fitting.
Always make sure unity compiles

# Glossary

Lootbox: The item being opened to gacha
Item: The reward of the gacha from lootbox

# YAML driven

This game is driven by a single .zip file containing a yaml file and other supporting files
yaml file contains the following

1. A list of items (required)
   An item includes:

- id (required)
- video/image path (optional, default: broken.png)
- title (required)
- description (required)
- isWinner (optional, default: false)

2. Settings

- nLootbox (required)

3. List of hints
   A hint includes:

- title (optional)
- hintType (required, "image")
- answer (required)
- isCaseSensitive (optional, default: false)
  if hintType=="image"
- list of video/image paths

# Mechanic

1. Insertion
   User inserts a zip file containing all the items and information for the game.
   The yaml will be validated first (including the image paths) before continuing to the next step
2. Game creation
   The game for the specified yaml will be generated.
3. Game play
   a. Title screen Play (Title screen -> Lootboxes Screen)
   b. Pick a lootbox (Lootboxes Screen -> Lootbox Open Screen)
   c. Player is given a randomized (new) hint game
   d. Player guesses hint correctly (See # Future Mechanic)
   e. Shows a big popup text "Player Answered Correctly\n'<answer>'"
   f. Lootbox spins
   g. Lootbox picks winner(s)
   h. Goes back to Lootboxes Screen, lootbox greyed out, winner(s) are shown on top of the lootbox
