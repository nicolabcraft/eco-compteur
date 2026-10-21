# Contribuer à Compteur

Merci de votre aide ! Corrections, idées : tout est bienvenu.
*[English below](#english)*

## Signaler un problème ou proposer une idée

Ouvrez une [issue](../../issues/new/choose) avec le modèle qui correspond (bug,
idée). Pour un bug, indiquez la version d'Eco et de Compteur, le véhicule
concerné, le sol, ce que vous avez fait et ce qui s'est passé. Une capture
d'écran du compteur aide beaucoup.

## Branches et versions

| Branche | Rôle |
|---|---|
| `main` | **Versions publiées** uniquement. Chaque version y porte un tag `vX.Y.Z`, qui déclenche la release automatique. On n'y pousse jamais directement. |
| `dev` | **Intégration** : la prochaine version en préparation. Toutes les pull requests visent `dev`. |
| `feature/<sujet>` | Une **nouvelle fonction**, partie de `dev` (ex. `feature/unite-mph`). |
| `fix/<sujet>` | Une **correction**, partie de `dev` (ex. `fix/vitesse-bateau`). |

Travailler sur une fonction :

```bash
git checkout dev && git pull
git checkout -b feature/mon-sujet
# ... modifications, commits ...
git push -u origin feature/mon-sujet
# puis pull request vers dev
```

La CI vérifie chaque pull request et chaque push sur `main` et `dev`, et fournit
un paquet de test dans l'onglet *Actions*.

Publier une version (mainteneurs) :

1. mettre à jour `CHANGELOG.md` et le numéro de version (`VERSION`) sur `dev` ;
2. pull request `dev` → `main`, puis fusion ;
3. tag sur `main` : `git tag vX.Y.Z && git push origin vX.Y.Z` ; le workflow
   *Release* fabrique l'archive et publie la release.

Une correction urgente peut partir de `main` (`fix/…`), puis être fusionnée dans
`main` **et** `dev`.

## Proposer du code

1. Forkez le dépôt et créez une branche `feature/…` ou `fix/…` à partir de `dev`.
2. Outils : .NET 10 SDK et Node.js 20 ou plus. **Aucun serveur Eco n'est
   nécessaire** : les assemblies d'Eco viennent du paquet NuGet officiel
   `Eco.ReferenceAssemblies`.
3. Avant d'envoyer :
   ```bash
   dotnet build -c Release
   node tools/package.mjs
   ```
4. Testez sur un serveur Eco local : une erreur dans `Mods/UserCode` empêche le
   serveur de démarrer. Le journal doit afficher `[Compteur] speedometer started`.
5. Ouvrez une pull request **vers `dev`** : la CI refait ces vérifications et fabrique un
   paquet de test, téléchargeable depuis l'onglet *Actions*.

### Règles

- Le compteur est partagé par tous ceux qui voient le véhicule : il ne contient
  pas de mots à traduire, seulement des nombres et `km/h`.
- Les messages du journal passent par `Localizer.NotLocalizedStr`.
- Pas de `Console` ni de bibliothèque externe : le serveur compile
  `Mods/UserCode` avec des références limitées.

## Sécurité

Ne signalez jamais une faille dans une issue publique : voir [SECURITY.md](SECURITY.md).

## Licence

En contribuant, vous acceptez que votre contribution soit publiée sous la
licence du projet, [PolyForm Noncommercial 1.0.0](LICENSE.md) : gratuit,
réutilisable, jamais commercial.

---

## English

Thanks for helping! Fixes and ideas are all welcome.

- **Report a bug or suggest an idea**: open an [issue](../../issues/new/choose)
  with the matching template (Eco and mod versions, vehicle, ground, and a
  screenshot of the gauge if you can).
- **Code**: .NET 10 SDK and Node.js 20+; no Eco server needed to build. Run
  `dotnet build -c Release` and `node tools/package.mjs` before opening a pull
  request, and test on a local Eco server (the log must show
  `[Compteur] speedometer started`). The gauge is shared by everyone viewing the
  vehicle, so it holds no words to translate: numbers and `km/h` only. No
  `Console` and no external libraries: the server compiles `Mods/UserCode` with
  limited references.
- **Branches**: `main` only holds released versions (each tagged `vX.Y.Z`,
  which triggers the automatic release); `dev` is the integration branch for the
  next version, and **every pull request targets `dev`**; work in
  `feature/<topic>` or `fix/<topic>` branches created from `dev`. To release,
  maintainers update `CHANGELOG.md` and `VERSION` on `dev`, merge `dev` into
  `main` through a pull request, then push a `vX.Y.Z` tag on `main`.
- **Security**: report vulnerabilities privately, see [SECURITY.md](SECURITY.md).
- **License**: contributions are published under the project's
  [PolyForm Noncommercial 1.0.0](LICENSE.md) license.
