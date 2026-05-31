# CWELOWNIA — panel webowy (VPS)

Panel do zarządzania serwerem CS2 z **osobnego VPS**, połączony z grą przez
**wspólną bazę MySQL** (tę samą, której używa plugin / statystyki na DatHost).
Nie wymaga otwierania żadnych portów na serwerze gry.

## Jak to działa

```
[Panel na VPS] --zapis--> [MySQL (DatHost)] <--odczyt/wykonanie-- [Plugin w grze]
```

- Panel **zapisuje** komendy do tabeli `retakes_remote_commands`.
- Plugin (moduł RemoteControl) **odpytuje** tę tabelę co kilka sekund i wykonuje
  komendy w grze, oznaczając je jako wykonane.
- Plugin **publikuje** status (mapa, liczba graczy) do `retakes_server_status`,
  a panel go pokazuje. Ranking czyta z `retakes_player_stats`.

## Wymagania

- Node.js 18+ na VPS
- Dostęp do tej samej bazy MySQL co serwer gry (dane z DatHost → „Databases")
- W pluginie na serwerze: w `RemoteControlSettings` ustaw `IsEnabled: true`
  oraz `ServerId` taki sam jak `SERVER_ID` w panelu.

## Instalacja na VPS

```bash
cd panel
cp .env.example .env      # uzupełnij dane MySQL, login i hasło panelu
npm install
npm start                 # domyślnie http://VPS_IP:8080
```

Zalecane: uruchom przez `pm2` lub jako usługa systemd, i postaw przed tym
nginx z HTTPS.

```bash
npm install -g pm2
pm2 start server.js --name cwelownia-panel
pm2 save
```

## Konfiguracja pluginu (serwer gry)

W `addons/counterstrikesharp/configs/plugins/RetakesPlugin/RetakesPlugin.json`:

```json
"RemoteControlSettings": {
  "IsEnabled": true,
  "PollIntervalSeconds": 3.0,
  "StatusIntervalSeconds": 10.0,
  "ServerId": "cwelownia1",
  "AllowedCommandPrefixes": ["css_", "changelevel", "map", "mp_", "sv_", "bot_", "kickid", "say", "exec"]
}
```

Baza brana jest z `StatsSettings.Database` (te same dane MySQL).

## Bezpieczeństwo

- Zmień `ADMIN_USER` / `ADMIN_PASSWORD` / `SESSION_SECRET` w `.env`.
- Panel wykonuje tylko komendy z dozwolonych prefiksów (walidacja po stronie
  pluginu w `AllowedCommandPrefixes` i po stronie panelu).
- Postaw panel za HTTPS (nginx + certbot).
- Nie wystawiaj bazy MySQL publicznie bez potrzeby.

## Funkcje panelu

- Status serwera na żywo (mapa, gracze)
- Zmiana mapy, scramble, force bombsite, restart
- Dowolna komenda (przez kolejkę)
- Ranking graczy (TOP, tylko odczyt)
- Historia wykonanych komend
