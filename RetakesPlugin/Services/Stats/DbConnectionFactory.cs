using MySqlConnector;
using RetakesPlugin.Configs;

namespace RetakesPlugin.Services.Stats;

/// <summary>
/// Builds the MySQL connection string and table prefix from config, with a
/// fallback to environment variables. This lets operators keep the database
/// password out of the JSON config (e.g. set it in the DatHost panel as an env
/// var) — env values take precedence when present.
///
/// Environment variables:
///   RETAKES_DB_HOST, RETAKES_DB_PORT, RETAKES_DB_USER,
///   RETAKES_DB_PASSWORD, RETAKES_DB_NAME, RETAKES_DB_PREFIX
/// </summary>
public static class DbConnectionFactory
{
    public static string Host(DatabaseSettings s) => Env("RETAKES_DB_HOST", s.Host);
    public static string TablePrefix(DatabaseSettings s) =>
        Env("RETAKES_DB_PREFIX", string.IsNullOrEmpty(s.TablePrefix) ? "retakes_" : s.TablePrefix);

    /// <summary>True when a host is configured (via config or env) and the DB can be used.</summary>
    public static bool IsConfigured(DatabaseSettings s) => !string.IsNullOrWhiteSpace(Host(s));

    public static string BuildConnectionString(DatabaseSettings s)
    {
        var portText = Env("RETAKES_DB_PORT", s.Port.ToString());
        if (!uint.TryParse(portText, out var port)) port = 3306;

        return new MySqlConnectionStringBuilder
        {
            Server = Host(s),
            Port = port,
            UserID = Env("RETAKES_DB_USER", s.User),
            Password = Env("RETAKES_DB_PASSWORD", s.Password),
            Database = Env("RETAKES_DB_NAME", s.Name),
            Pooling = true,
            MinimumPoolSize = 0,
            MaximumPoolSize = 5,
            ConnectionTimeout = 10
        }.ConnectionString;
    }

    private static string Env(string key, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
