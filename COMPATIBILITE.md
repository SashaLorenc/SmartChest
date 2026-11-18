# Compatibilité de Smart Chest avec les autres mods

Aucun de ces mods n'est obligatoire. Smart Chest les détecte au lancement (`LogOutput.log` affiche
« Mods compatibles détectés : ... ») et n'active l'intégration que s'ils sont installés. Si un autre mod
change son code dans une mise à jour, l'intégration se coupe avec un avertissement dans le log, sans casser le reste.

## Mods avec une intégration dédiée

| Mod | Risque sans traitement | Ce que fait Smart Chest |
| --- | --- | --- |
| Quick Stack Store Sort Trash Restock (Goldenrevolver) | Le stockage rapide ignore les catégories | Stockage rapide : un coffre catégorisé reçoit les objets de ses catégories. Tout stocker : un coffre strict ne prend que ses catégories. Ses favoris ne bougent jamais. Option `[Compatibilite] QuickStackStore`. |
| AzuAutoStore (Azumatt) | Range n'importe où, sans tenir compte des catégories | Un coffre catégorisé reçoit les objets de ses catégories même s'il n'en contient pas encore ; en mode strict il refuse les autres. Option `[Compatibilite] AzuAutoStore`. |
| Multi User Chest (MaxSch) | Plusieurs joueurs dans le même coffre | Avec ce mod, la touche de rangement fonctionne même si quelqu'un a le coffre ouvert, comme Quick Stack Store. |
| Dynamic Storage Piles (MaxSch) | Ranger n'importe quoi dans un tas de bois | Les tas limités à un seul objet n'ont pas de boutons et ne reçoivent jamais d'objets par catégorie. |

## Mods d'inventaire (emplacements d'équipement, raccourcis, carquois)

Ces mods ajoutent des cases spéciales dans l'inventaire du joueur. Sans traitement, la touche de rangement
viderait l'équipement ou les raccourcis dans un coffre. Smart Chest ne touche jamais à ces cases.

| Mod | Cases protégées |
| --- | --- |
| Azu Extended Player Inventory | Rangées d'équipement et de raccourcis ajoutées en bas |
| Extended Player Inventory (Aedenthorn) | Rangée d'équipement en bas |
| Odin's Extended Inventory / Odin's QoL | Rangée d'équipement en bas |
| Comfy Quick Slots | Emplacements d'armure et de raccourcis |
| Better Archery | Rangées du carquois |
| Equipment and Quick Slots (RandyKnapp) | Utilise ses propres inventaires séparés : a priori non concerné (non vérifié en jeu) |

Si Quick Stack Store est installé, Smart Chest utilise directement sa détection de ces cases (toujours à jour).

## Sacs et inventaires portables

Adventure Backpacks, Backpacks, le sac de Jewelcrafting, Currency Pocket, Smart Containers…
Ces mods ouvrent leur inventaire comme un coffre. Smart Chest ne considère comme coffres que les pièces
**construites par un joueur** (coffres, bateaux, chariots) : pas de boutons sur les sacs, et ils ne reçoivent
jamais d'objets. Les sacs ouverts par d'autres joueurs ne sont jamais touchés.

## Mods qui ajoutent des objets

Therzie (Warfare, Armory, Monstrum…), Epic Loot, Magic Plugin, Southsil, Fantasy Armoury, etc.
Rien de spécial à faire : les catégories par défaut sont calculées à partir du type de l'objet, donc
leurs armes, armures, potions et matériaux tombent dans la bonne catégorie. Pour un objet mal classé : touche **K**.

## Interface

| Mod | État |
| --- | --- |
| Configuration Manager | Compatible : réglages modifiables en jeu (F1), boutons déplacés à chaud |
| Project Auga | Remplace toute l'interface de l'inventaire : les boutons peuvent ne pas apparaître. La touche **L** fonctionne quand même. Non pris en charge (Quick Stack Store ne l'est pas non plus). |

## Règles de sécurité en multijoueur (reprises de Quick Stack Store)

- On ne touche pas un coffre ouvert par un autre joueur (sauf avec Multi User Chest).
- On respecte les coffres privés et les balises (wards).
- En multijoueur sans Multi User Chest, les coffres de bateaux ne sont pas remplis à distance.
- Pendant le rangement à distance, le coffre est marqué « en cours d'utilisation » pour les autres joueurs.

## Non pris en charge pour l'instant

- **Item Drawers (KG)** : ce ne sont pas des coffres, ils ne peuvent pas avoir de catégories.
