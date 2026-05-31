import express from 'express';
import session from 'express-session';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';
import { readFileSync } from 'fs';

import { queueCommand, getStatus, getTop, getCommandHistory, getPlayers, queuePlayerAction, getDuels } from './db.js';

// Minimal .env loader (no extra dependency).
try {
  const envPath = join(dirname(fileURLToPath(import.meta.url)), '.env');
  for (const line of readFileSync(envPath, 'utf8').split('\n')) {
    const m = line.match(/^\s*([A-Z0-9_]+)\s*=\s*(.*)\s*$/);
    if (m && !process.env[m[1]]) process.env[m[1]] = m[2];
  }
} catch { /* .env optional if vars are set in the environment */ }

const __dirname = dirname(fileURLToPath(import.meta.url));
const app = express();

app.use(express.json());
app.use(express.urlencoded({ extended: true }));
app.use(session({
  secret: process.env.SESSION_SECRET || 'change-me',
  resave: false,
  saveUninitialized: false,
  cookie: { maxAge: 1000 * 60 * 60 * 8 }
}));

// ---- Auth ----
function requireAuth(req, res, next) {
  if (req.session?.user) return next();
  if (req.path.startsWith('/api/')) return res.status(401).json({ error: 'unauthorized' });
  return res.redirect('/login');
}

app.get('/login', (req, res) => {
  res.sendFile(join(__dirname, 'public', 'login.html'));
});

app.post('/login', (req, res) => {
  const { user, password } = req.body;
  if (user === process.env.ADMIN_USER && password === process.env.ADMIN_PASSWORD) {
    req.session.user = user;
    return res.redirect('/');
  }
  res.redirect('/login?error=1');
});

app.post('/logout', (req, res) => {
  req.session.destroy(() => res.redirect('/login'));
});

// ---- API (all auth-gated) ----
app.use('/api', requireAuth);

app.get('/api/status', async (_req, res) => {
  try {
    const status = await getStatus();
    res.json({ status });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.get('/api/top', async (req, res) => {
  try {
    res.json({ players: await getTop(req.query.limit || 25) });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.get('/api/history', async (_req, res) => {
  try {
    res.json({ commands: await getCommandHistory(30) });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.get('/api/players', async (_req, res) => {
  try {
    res.json({ players: await getPlayers() });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.get('/api/duels', async (req, res) => {
  const steamId = String(req.query.steamId || '').trim();
  if (!/^\d{5,20}$/.test(steamId)) return res.status(400).json({ error: 'bad steamId' });
  try {
    res.json({ duels: await getDuels(steamId) });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// Per-player action. The action is validated against a fixed allowlist.
const PLAYER_ACTIONS = new Set([
  'kick', 'slay', 'respawn', 't', 'ct', 'spec', 'strip',
  'god', 'ungod', 'hp', 'freeze', 'unfreeze', 'noclip',
  'lowgrav', 'normgrav', 'speed', 'normspeed',
  'small', 'big', 'giant', 'normsize'
]);

// Curated, live-tunable config keys (must match the plugin's css_rcon_setcfg).
const CFG_KEYS = new Set([
  'instadefuse.enabled',
  'weapon.enabled', 'weapon.allowpreferences', 'weapon.allowsnipers',
  'weapon.sniperchance', 'weapon.mingrenades', 'weapon.maxgrenades', 'weapon.lonegrenades',
  'stats.enabled', 'hud.enabled', 'automessage.enabled',
  'mapvote.allowrtv', 'autoendvote.enabled', 'fun.enabled',
  'lucky.enabled', 'lucky.chance', 'lucky.minplayers',
  'pistol.enabled', 'pistol.everyx', 'pistol.minplayers',
  'endscreen.enabled'
]);

app.post('/api/setcfg', async (req, res) => {
  const key = String(req.body.key || '').trim().toLowerCase();
  const value = String(req.body.value ?? '').trim();
  if (!CFG_KEYS.has(key)) return res.status(400).json({ error: 'unknown key' });
  if (!/^[\w.\-]{1,16}$/.test(value)) return res.status(400).json({ error: 'bad value' });
  try { await queueCommand(`css_rcon_setcfg ${key} ${value}`); res.json({ ok: true }); }
  catch (e) { res.status(500).json({ error: e.message }); }
});

// Persist the live config to disk on the game server (so edits survive restart).
app.post('/api/config/save', async (_req, res) => {
  try { await queueCommand('css_rcon_savecfg'); res.json({ ok: true }); }
  catch (e) { res.status(500).json({ error: e.message }); }
});

// Force a special round next round.
app.post('/api/specialround', async (req, res) => {
  const type = String(req.body.type || '').trim().toLowerCase();
  if (type !== 'lucky' && type !== 'pistol') return res.status(400).json({ error: 'bad type' });
  try { await queueCommand(`css_rcon_specialround ${type}`); res.json({ ok: true }); }
  catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/player', async (req, res) => {
  const steamId = String(req.body.steamId || '').trim();
  const action = String(req.body.action || '').trim().toLowerCase();
  if (!/^\d{5,20}$/.test(steamId)) return res.status(400).json({ error: 'bad steamId' });
  if (!PLAYER_ACTIONS.has(action)) return res.status(400).json({ error: 'bad action' });
  try { await queuePlayerAction(steamId, action); res.json({ ok: true }); }
  catch (e) { res.status(500).json({ error: e.message }); }
});

// Generic command queue (validated server-side too).
app.post('/api/command', async (req, res) => {
  const command = String(req.body.command || '').trim();
  if (!command) return res.status(400).json({ error: 'empty command' });
  if (command.length > 500) return res.status(400).json({ error: 'too long' });
  try {
    await queueCommand(command);
    res.json({ ok: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// Convenience endpoints for common actions.
app.post('/api/changemap', async (req, res) => {
  const map = String(req.body.map || '').trim();
  if (!/^[a-z0-9_]+$/i.test(map)) return res.status(400).json({ error: 'bad map name' });
  try { await queueCommand(`changelevel ${map}`); res.json({ ok: true }); }
  catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/toggle', async (req, res) => {
  // e.g. css commands you expose; kept generic via the command queue.
  const cmd = String(req.body.command || '').trim();
  if (!cmd.startsWith('css_') && !cmd.startsWith('mp_') && !cmd.startsWith('sv_'))
    return res.status(400).json({ error: 'not allowed' });
  try { await queueCommand(cmd); res.json({ ok: true }); }
  catch (e) { res.status(500).json({ error: e.message }); }
});

// ---- Static UI (auth-gated) ----
app.use('/', requireAuth, express.static(join(__dirname, 'public')));

const port = Number(process.env.PORT) || 8080;
app.listen(port, () => {
  console.log(`CWELOWNIA panel listening on http://0.0.0.0:${port}`);
});
