# Compiler et tester Smart Chest sur ton PC

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
