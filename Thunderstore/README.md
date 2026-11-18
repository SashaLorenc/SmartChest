# Smart Chest

Give categories to your chests and to your items, then store your whole inventory in the right chests with one key or one button.

## Features

- **Chest categories, chosen in game.** Open a chest and click **Categories**: tick one or several categories (Wood and stone, Ores and metals, Food, Weapons...).
- **16 default categories** for every item of the game, computed from the item type, so items added by other mods (Therzie, Epic Loot...) are sorted too.
- **Custom categories**: create your own, rename them, change their color, hide or delete them (**Categories > Manage**).
- **Edit an item's categories**: hover an item in your inventory and press **K**. The change applies to every item of that type. **Reset to default** undoes it.
- **Store by category**: press **L**, or click **Store by category** above your inventory. Every item goes to the nearby chests of its categories, even if they don't contain it yet.
- **Store here**: only stores the items that belong to the opened chest.
- **Catch-all chest**: set a chest without categories to **Catch-all** mode. It receives everything that found no dedicated chest. A chest with categories in this mode takes its categories first, then the rest.
- **Priorities (0 to 9)**: between two chests of the same category, the one with the highest priority is filled first.
- **Full chest warning**: a message tells you when a categorized chest is full and some items could not be stored. Hovering a chest shows its categories and FULL.
- **Search**: click **Search**, type an item name. Nearby chests that contain it glow for a few seconds and show the quantity on their front.
- **Sort**: groups a chest's content by category, then by name, and merges stacks.
- Items in the hotbar (1 to 8), equipped items and Quick Stack Store favorites never move.
- Categories are saved in the world and shared with other players. Works in single player and on dedicated servers (every player needs the mod).

## Default keys

| Key | Action |
| --- | --- |
| L | Store by category in nearby chests |
| K | Edit the categories of the hovered item (inventory open) |

## Compatibility

All optional, detected at startup:

- **Quick Stack Store Sort Trash Restock**: Quick Stack also sends items to the chests of their categories, Store All respects chests in strict mode, favorites are respected.
- **AzuAutoStore**: categorized chests receive the items of their categories and, in strict mode, refuse the others.
- **Multi User Chest**, **Dynamic Storage Piles**.
- **Inventory mods** (Azu Extended Player Inventory, Extended Player Inventory, Odin's Extended Inventory, Comfy Quick Slots, Better Archery, Equipment and Quick Slots): their equipment, quick slot and quiver slots are never emptied.
- **Backpacks** (Adventure Backpacks, Backpacks, Jewelcrafting, Currency Pocket...): only chests built by a player are used, bags never receive items.
- Not supported: Project Auga (buttons may not show, the L key still works).

## Configuration

`BepInEx/config/sashalorenc.SmartChest.cfg`, or in game with Configuration Manager (F1).

| Section | Setting | Default | |
| --- | --- | --- | --- |
| Rangement | Rayon | 10 | Range (m) of the chests used for storing |
| Rangement | IgnorerBarreRaccourcis | true | Never store hotbar items |
| Rangement | AlerteCoffrePlein | true | Full chest warning |
| Touches | RangerParCategorie / EditerObjet | L / K | Keys |
| Affichage | CategoriesDansInfobulle | true | Show item categories in tooltips |
| Interface | PositionBoutons, DecalageX/Y, LargeurBoutons | Dessous | Position of the chest buttons (Dessus = top, Dessous = bottom, Gauche = left, Droite = right) |
| Interface | PositionBoutonsInventaire, InventaireDecalageX/Y | Dessus | Position of the inventory buttons |
| Recherche | Rayon / Duree | 30 / 15 | Search range (m) and glow duration (s) |
| Compatibilite | QuickStackStore / AzuAutoStore | true | Integrations |

The category catalog (custom categories, edited items) is stored in `BepInEx/config/SmartChest/catalog.json`.
The console command `smartchest_dump` writes the categories of every item to `BepInEx/config/SmartChest/items_dump.txt`.

## Languages

English and French. Translations go in `Translations/<Language>/*.json` (Jötunn format).

## Source and bug reports

https://github.com/SashaLorenc/SmartChest
