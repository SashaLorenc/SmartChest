# Smart Chest

Mod Valheim (BepInEx + Jötunn) : donne des catégories aux coffres et aux objets, puis range chaque objet dans le bon coffre en une touche.

- Plusieurs catégories par coffre, choisies en jeu.
- Catégories personnalisées.
- Catégories par défaut pour tous les objets du jeu, modifiables.
- Fonctionne seul, en solo et sur serveur.
- Compatible (optionnel) avec Quick Stack Store Sort Trash Restock.

Voir `COMPILER.md` pour compiler, tester et créer le zip Thunderstore (`package.ps1`), et `COMPATIBILITE.md` pour les autres mods.

## Organisation du code

Chaque dossier a un seul rôle, et les dépendances vont dans un seul sens (UI → Storing → Chests → Catalog) :

| Dossier | Rôle |
|---|---|
| `Catalog/` | Catégories, règles par défaut des objets, fichier `catalog.json`. Aucune dépendance au reste du mod. |
| `Chests/` | Données d'un coffre stockées dans son ZDO (catégories, mode, priorité), liste des coffres chargés, description courte. |
| `Storing/` | Actions : rangement par catégorie, tri d'un coffre, recherche. Pas d'interface. |
| `Compat/` | Détection des autres mods et patchs optionnels (Quick Stack Store, AzuAutoStore, inventaires étendus...). Tout passe par réflexion. |
| `UI/` | Boutons, fenêtres, infobulles et surbrillance. `UiKit` regroupe les éléments réutilisés, `Panels.CloseAll()` garantit une seule fenêtre ouverte. |
