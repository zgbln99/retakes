import mysql from 'mysql2/promise';

const prefix = process.env.DB_PREFIX || 'retakes_';

export const tables = {
  commands: `${prefix}remote_commands`,
  status: `${prefix}server_status`,
  stats: `${prefix}player_stats`,
  players: `${prefix}server_players`
};

export const serverId = process.env.SERVER_ID || 'cwelownia1';

const pool = mysql.createPool({
  host: process.env.DB_HOST,
  port: Number(process.env.DB_PORT) || 3306,
  user: process.env.DB_USER,
  password: process.env.DB_PASSWORD,
  database: process.env.DB_NAME,
  waitForConnections: true,
  connectionLimit: 5,
  // Don't crash the panel if the DB blips.
  enableKeepAlive: true
});

export default pool;

/** Queue a command for the plugin to execute in-game. */
export async function queueCommand(command) {
  await pool.query(
    `INSERT INTO \`${tables.commands}\` (server_id, command, status) VALUES (?, ?, 'pending')`,
    [serverId, command]
  );
}

/** Latest server status row (map / players). */
export async function getStatus() {
  const [rows] = await pool.query(
    `SELECT server_id, map, players, max_players, updated_at FROM \`${tables.status}\` WHERE server_id = ?`,
    [serverId]
  );
  return rows[0] || null;
}

/** Top players by kills (read-only stats view). */
export async function getTop(limit = 25) {
  const [rows] = await pool.query(
    `SELECT name, kills, deaths, headshots, assists, rounds
     FROM \`${tables.stats}\` ORDER BY kills DESC LIMIT ?`,
    [Number(limit)]
  );
  return rows;
}

/** Players currently on the server (published by the plugin). */
export async function getPlayers() {
  const [rows] = await pool.query(
    `SELECT steam_id, name, team, alive, updated_at
     FROM \`${tables.players}\` WHERE server_id = ? ORDER BY team DESC, name ASC`,
    [serverId]
  );
  return rows;
}

/** Queue a per-player action (executed in-game by css_rcon_player). */
export async function queuePlayerAction(steamId, action) {
  await queueCommand(`css_rcon_player ${steamId} ${action}`);
}

/** Recent command history. */
export async function getCommandHistory(limit = 30) {
  const [rows] = await pool.query(
    `SELECT id, command, status, created_at, processed_at
     FROM \`${tables.commands}\` WHERE server_id = ? OR server_id = ''
     ORDER BY id DESC LIMIT ?`,
    [serverId, Number(limit)]
  );
  return rows;
}
