// Patches the Appium `windows` driver (2.x) so it works with classic WinAppDriver 1.2.x.
// The installed driver has a known incompatibility (appium/appium-windows-driver#316):
//   1. It probes GET /status to detect that WinAppDriver is up, but classic WinAppDriver
//      answers HTTP 500 to /status even when healthy, so startup always times out.
//   2. It forwards capabilities verbatim (including `appium:` prefixes) to WinAppDriver,
//      which only understands un-prefixed names like `app`, `appArguments`, `appWorkingDir`.
// Run this once after `appium driver install windows` (or after any driver update). It is
// idempotent and only touches the driver's winappdriver.js (both the `lib/` and `build/` copies).
//
// Usage:  node patch-windows-driver.js

import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { homedir } from 'node:os';
import { join } from 'node:path';

const marker = '// [broccoli-e2e-patch]';

const candidates = [
  join(homedir(), '.appium', 'node_modules', 'appium-windows-driver', 'lib', 'winappdriver.js'),
  join(homedir(), '.appium', 'node_modules', 'appium-windows-driver', 'build', 'lib', 'winappdriver.js'),
];

const statusCatchPattern = /if \(this\.proxy\?\.didProcessExit\)\s*\{\s*throw new Error\(err\.message\);\s*\}\s*return false;/;
const statusCatchReplacement = (match, indent) => `${indent}if (this.proxy?.didProcessExit) {
${indent}    throw new Error(err.message);
${indent}}
${indent}// Classic WinAppDriver (1.2.x) answers HTTP 500 to GET /status even when healthy,
${indent}// so a failed probe still means the server is listening. ${marker}
${indent}return true;`;

const sessionPattern = /await this\.proxy\?\.command\('\/session', 'POST', \{\s*desiredCapabilities\s*\}\);/;
const sessionReplacement = `// Classic WinAppDriver only understands un-prefixed capabilities. ${marker}
                const winAppDriverCaps = {};
                for (const [key, value] of Object.entries(desiredCapabilities)) {
                    winAppDriverCaps[key.replace(/^appium:/, '')] = value;
                }
                await this.proxy?.command('/session', 'POST', { desiredCapabilities: winAppDriverCaps });`;

let foundAny = false;
let patchedAny = false;

for (const target of candidates) {
  if (!existsSync(target)) {
    continue;
  }

  foundAny = true;
  let source = readFileSync(target, 'utf8');

  if (source.includes(marker)) {
    console.log(`Already patched:\n  ${target}`);
    patchedAny = true;
    continue;
  }

  const statusMatch = source.match(statusCatchPattern);
  const sessionMatch = source.match(sessionPattern);

  if (!statusMatch || !sessionMatch) {
    console.error(`Could not find patch targets in:\n  ${target}\nDriver version may differ.`);
    continue;
  }

  // Preserve the original indentation of the status catch block for tidy output.
  const lineStart = source.lastIndexOf('\n', statusMatch.index) + 1;
  const indent = source.slice(lineStart, statusMatch.index);

  source = source.replace(statusMatch[0], statusCatchReplacement(statusMatch[0], indent));
  source = source.replace(sessionPattern, sessionReplacement);
  writeFileSync(target, source, 'utf8');
  console.log(`Patched:\n  ${target}`);
  patchedAny = true;
}

if (!foundAny) {
  console.error('appium-windows-driver not found at any expected location.');
  console.error('Install it first with:  appium driver install windows');
  process.exit(1);
}

if (!patchedAny) {
  process.exit(1);
}
