# Compiler et tester Smart Chest sur ton PC

## 0. Récupérer le code (une seule fois)

1. Installe **Git pour Windows** : https://git-scm.com/download/win (options par défaut).
2. Ouvre un terminal (PowerShell) dans le dossier où tu veux le projet, par ex. `C:\Dev`, puis :

```
git clone https://github.com/SashaLorenc/SmartChest.git
cd SmartChest
```

3. Recopie ton `Environment.props` à la racine de ce nouveau dossier (étape 2 plus bas).

## À chaque modification

Dans le dossier `SmartChest` :

```
git pull
dotnet build SmartChest/SmartChest.csproj -c Release
```

`git pull` récupère mes derniers changements, la compilation copie la DLL dans ton profil r2modman. Ton `Environment.props` n'est jamais écrasé.

## 1. Installer une seule fois

1. **SDK .NET 8** (ou plus récent) : https://dotnet.microsoft.com/download. Il compile aussi les projets .NET Framework 4.6.2 grâce au paquet `Microsoft.NETFramework.ReferenceAssemblies` déjà inclus dans le projet.
2. **Un éditeur** : Visual Studio Community (cocher « Développement .NET Desktop ») ou VS Code avec l'extension C# Dev Kit. Facultatif si tu compiles en ligne de commande.
3. **r2modman** avec un profil Valheim où sont installés :
   - BepInExPack Valheim
   - Jötunn (ValheimModding-Jotunn)
   - (facultatif) Quick Stack Store Sort Trash Restock, pour tester la compatibilité
4. Lance le jeu une fois via r2modman, pour que BepInEx crée ses dossiers.

## 2. Indiquer tes chemins

Copie `Environment.props.example` en `Environment.props` (à la racine) et remplace les trois chemins :

- `ValheimDir` : dossier qui contient `valheim.exe` (Steam → clic droit sur Valheim → Gérer → Parcourir les fichiers locaux).
- `BepInExDir` : dans r2modman, Settings → Browse profile folder, puis le dossier `BepInEx`.
- `JotunnDll` : vérifie que `Jotunn.dll` existe bien à ce chemin (le nom du dossier dans `plugins` peut varier).

Ce fichier n'est jamais envoyé sur GitHub.

## 3. Compiler

Dans un terminal, à la racine du projet :

```
dotnet build SmartChest/SmartChest.csproj -c Release
```

Si tout va bien, la DLL est copiée automatiquement dans `BepInEx/plugins/SmartChest/` de ton profil.

## 4. Tester

1. Lance Valheim depuis r2modman (bouton « Start modded »).
2. Ouvre `BepInEx/LogOutput.log` : tu dois voir `Smart Chest 0.1.0 chargé.`
3. Le fichier de config apparaît dans `BepInEx/config/sashalorenc.SmartChest.cfg`.

## En cas d'erreur

Copie-colle l'erreur complète (terminal ou `LogOutput.log`) dans le fil du projet, je t'aide à corriger.

Erreurs fréquentes :
- `Could not find file ... assembly_valheim.dll` : `ValheimDir` est faux.
- `Could not find ... Jotunn.dll` : `JotunnDll` est faux.
- `The reference assemblies for .NETFramework,Version=v4.6.2 were not found` : relance `dotnet restore`, il faut Internet pour télécharger les paquets.

## À vérifier (non confirmé)

- Le GUID de Quick Stack Store dans `Plugin.cs` (`goldenrevolver.quick_stack_store`) est une supposition : quand le mod est installé, le log BepInEx affiche son vrai GUID au chargement, envoie-le moi.
- Les versions des dépendances dans `manifest.json` sont à aligner sur celles que tu as dans r2modman.

## Tester l'étape 2 (catégories des objets)

1. Recompile (`dotnet build SmartChest/SmartChest.csproj -c Release`) et lance le jeu.
2. Survole des objets dans l'inventaire : l'infobulle se termine par une ligne « Catégories : ... » en couleur.
3. Le fichier `BepInEx/config/SmartChest/catalog.json` est créé avec les 15 catégories par défaut.
4. Pour voir la catégorie de **tous** les objets d'un coup : ouvre la console avec F5 (il faut l'option de lancement `-console` : Steam → Valheim → Propriétés → Options de lancement, ou dans r2modman Settings → Set launch parameters), puis tape `smartchest_dump`. Le résultat est dans `BepInEx/config/SmartChest/items_dump.txt`. Envoie-le-moi pour qu'on corrige les objets mal rangés.

## Tester l'étape 3 (coffres et rangement)

1. `git pull`, recompile, lance le jeu.
2. Ouvre un coffre : deux boutons apparaissent au-dessus, **Catégories** et **Ranger ici**.
3. **Catégories** ouvre un panneau : coche une ou plusieurs catégories. Le bouton **Mode** bascule entre *Strict* (seulement ses catégories) et *Trop-plein* (prend aussi ce qui n'a trouvé aucun coffre). En bas, tape un nom et clique **+ Nouvelle catégorie** pour en créer une (elle est cochée pour ce coffre).
4. Survole le coffre fermé : ses catégories s'affichent sous son nom.
5. **Ranger ici** dépose dans ce coffre seulement les objets de ses catégories.
6. Hors de l'inventaire, la touche **L** range tout l'inventaire dans les coffres catégorisés à moins de 10 m. La barre de raccourcis (1 à 8) et l'équipement porté ne bougent pas (réglable dans la config).
7. Quitte et relance le monde : les catégories des coffres doivent être conservées.

## Déplacer les boutons du coffre

Si les boutons **Catégories** / **Ranger ici** chevauchent un autre menu, change dans `BepInEx/config/sashalorenc.SmartChest.cfg`, section `[Interface]` :

- `PositionBoutons` : `Dessus`, `Dessous` (par défaut), `Gauche` ou `Droite` du coffre.
- `DecalageX` / `DecalageY` : décalage fin en pixels.
- `LargeurBoutons` : largeur de chaque bouton.

Avec le mod Configuration Manager (touche F1 en jeu), les changements s'appliquent directement, sans relancer.

## Tester l'intégration Quick Stack Store Sort Trash Restock

Au lancement, `LogOutput.log` doit afficher `Intégration Quick Stack Store : active.`

1. Donne une catégorie à un coffre **vide** (ex. Minerais et métaux), mets du cuivre dans ton inventaire.
2. Ouvre un autre coffre à côté et clique **Stockage rapide** (ou sa touche) : le cuivre doit partir dans le coffre catégorisé, même s'il n'en contenait pas.
3. Ouvre un coffre catégorisé en mode **Strict** et clique **Tout stocker** : seuls les objets de ses catégories y entrent.
4. Mets un objet en favori (Alt + clic) : ni Quick Stack ni la touche L ne doivent le déplacer.

Désactivable dans la config : `[Compatibilite] QuickStackStore = false`.

## Tester l'étape 4 (éditeur de catégories)

1. `git pull`, recompile, lance le jeu.
2. **Catégories d'un objet** : ouvre l'inventaire, survole un objet et appuie sur **K**. Coche ou décoche des catégories : ça vaut pour tous les objets de ce type. L'infobulle affiche une `*` quand les catégories ont été modifiées. **Rétablir le défaut** annule tes changements.
3. **Gérer les catégories** : ouvre un coffre → **Catégories** → **Gérer**. Pour chaque catégorie :
   - clique la pastille de couleur pour changer de couleur ;
   - modifie le nom puis appuie sur Entrée ;
   - **Masquer / Afficher** pour les catégories par défaut, **Supprimer** (deux clics) pour les tiennes.
   **Retour** revient au panneau du coffre.
4. Tout est enregistré dans `BepInEx/config/SmartChest/catalog.json`.
