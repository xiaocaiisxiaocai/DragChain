import { build } from 'esbuild';
import { readdir, rm, writeFile } from 'node:fs/promises';
import { join, relative, resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

const root = resolve('.');
const srcDir = join(root, 'src');
const tempDir = join(root, 'node_modules', '.cache', 'logic-tests');

async function findLogicTests(dir) {
  const entries = await readdir(dir, { withFileTypes: true });
  const files = await Promise.all(entries.map(async entry => {
    const fullPath = join(dir, entry.name);
    if (entry.isDirectory()) {
      return findLogicTests(fullPath);
    }
    return entry.isFile() && entry.name.endsWith('.logic-test.ts') ? [fullPath] : [];
  }));
  return files.flat();
}

await rm(tempDir, { recursive: true, force: true });

const testFiles = await findLogicTests(srcDir);

for (const file of testFiles) {
  const outfile = join(tempDir, relative(srcDir, file)).replace(/\.ts$/, '.mjs');
  await build({
    entryPoints: [file],
    outfile,
    bundle: true,
    format: 'esm',
    platform: 'node',
    sourcemap: 'inline',
    logLevel: 'silent'
  });

  await import(pathToFileURL(outfile).href);
  console.log(`PASS ${relative(root, file)}`);
}

await writeFile(join(tempDir, 'last-run.txt'), new Date().toISOString());
