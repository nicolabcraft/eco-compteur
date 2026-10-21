// Fabrique l'archive de Compteur, prete a decompresser a la racine d'un serveur Eco :
//   Mods/UserCode/Compteur/*.cs        le mod (compile par le serveur)
//   README.md, LICENSE.md, NOTICE.md
// Usage : node tools/package.mjs [version]   (version : argument, COMPTEUR_VERSION, ou VERSION)
import { cp, mkdir, readdir, readFile, rm } from 'node:fs/promises';
import { execFileSync } from 'node:child_process';
import { join } from 'node:path';

const version = (process.argv[2] ?? process.env.COMPTEUR_VERSION ?? (await readFile('VERSION', 'utf8'))).trim().replace(/^v/, '');
const SORTIE = 'release/paquet';
const zip = `release/Compteur-${version}.zip`;

await rm(SORTIE, { recursive: true, force: true });
await mkdir(join(SORTIE, 'Mods/UserCode/Compteur'), { recursive: true });
for (const f of (await readdir('usercode/Compteur')).filter((n) => n.endsWith('.cs'))) {
  await cp(join('usercode/Compteur', f), join(SORTIE, 'Mods/UserCode/Compteur', f));
}
for (const f of ['README.md', 'LICENSE.md', 'NOTICE.md']) await cp(f, join(SORTIE, f));

await rm(zip, { force: true });
try {
  execFileSync('zip', ['-qr', join('..', `Compteur-${version}.zip`), '.'], { cwd: SORTIE, stdio: 'pipe' });
} catch {
  execFileSync('powershell', ['-NoProfile', '-Command', `Compress-Archive -Path '${SORTIE}/*' -DestinationPath '${zip}' -Force`], { stdio: 'pipe' });
}
console.log(`archive : ${zip}`);
