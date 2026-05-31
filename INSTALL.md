# Instalacja na serwerze CS2 (np. DatHost)

## Wymagania wstępne (najpierw!)

Plugin **nie zadziała** bez tych dwóch komponentów — zainstaluj je przed pluginem:

1. **Metamod:Source** — w panelu DatHost zwykle do włączenia jednym kliknięciem.
2. **CounterStrikeSharp** (API ≥ 345) — wersja **z runtime** ("with runtime").
   Część hostingów (w tym DatHost) ma go w panelu; jeśli nie, pobierz z
   [releases](https://github.com/roflmuffin/CounterStrikeSharp/releases).
3. **CS2MenuManager** — biblioteka menu w stylu SimpleAdmin (WASD: W/S/E/R).
   Pobierz najnowszy ZIP z
   [releases CS2MenuManager](https://github.com/schwarper/CS2MenuManager/releases)
   i rozpakuj do `addons/counterstrikesharp/` — powstanie folder
   `addons/counterstrikesharp/shared/CS2MenuManager/` (z `CS2MenuManager.dll` i
   `config.toml`). Bez tego menu (`!admin`, `!guns`, głosowanie) **nie otworzą się**.

   > W `config.toml` można ustawić domyślny typ menu na `WasdMenu` — wtedy wszystkie
   > menu wyglądają dokładnie jak w SimpleAdmin (W góra / S dół / E wybór / R wyjście).

Weryfikacja w konsoli serwera:
- `meta list` → powinien pokazać `CounterStrikeSharp`
- `css_plugins list` → lista załadowanych pluginów

## Instalacja pluginu

1. Pobierz `Retakes-vX.Y.Z.zip` z zakładki **Releases** tego repo.
2. **Rozpakuj** archiwum — w środku jest folder `addons/`.
3. Wgraj zawartość (folder `addons/`) do katalogu gry serwera, tak aby powstało:

```
csgo/addons/counterstrikesharp/plugins/RetakesPlugin/
    RetakesPlugin.dll
    MySqlConnector.dll        ← potrzebny tylko do statystyk
    lang/
    map_config/
csgo/addons/counterstrikesharp/shared/RetakesPluginShared/
    RetakesPluginShared.dll
```

   Na DatHost użyj **menedżera plików** lub FTP. To jest plugin „custom",
   więc nie pojawi się na liście gotowych do kliknięcia — wgrywasz pliki ręcznie.

4. Zrestartuj serwer **lub** zmień mapę, aby plugin się załadował.

## Pierwsza konfiguracja

Przy pierwszym starcie wygeneruje się config:

```
csgo/addons/counterstrikesharp/configs/plugins/RetakesPlugin/RetakesPlugin.json
```

Najważniejsze sekcje:

- `InstadefuseSettings` — wbudowany instant defuse
- `WeaponSettings` — losowa broń + preferencje `!guns`
- `AdminMenuSettings` — panel admina (`!admin`), flagi uprawnień
- `StatsSettings` — statystyki PvP (MySQL z DatHost), domyślnie wyłączone

### Statystyki (MySQL z DatHost)

W panelu DatHost utwórz bazę (zakładka „Databases"), a dane wpisz do configu:

```json
"StatsSettings": {
  "IsEnabled": true,
  "Database": {
    "Host": "twoj-host.dathost.net",
    "Port": 3306,
    "User": "uzytkownik",
    "Password": "haslo",
    "Name": "nazwa_bazy",
    "TablePrefix": "retakes_"
  }
}
```

> ⚠️ Nie commituj prawdziwych haseł do repozytorium — wpisuj je tylko w configu
> na serwerze. Jeśli baza jest nieosiągalna, statystyki same się wyłączą i nie
> wpłyną na grę.

## Komendy

| Komenda | Opis | Uprawnienia |
|---------|------|-------------|
| `!guns`, `!gun`, `!weapon` | Wybór preferowanej broni | gracz |
| `!rank`, `!stats` | Twoje statystyki PvP | gracz |
| `!top` | Ranking serwera | gracz |
| `!admin`, `!panel` | Panel admina (GUI) | `@css/root` |

Uprawnienia admina ustawia się w `addons/counterstrikesharp/configs/admins.json`.

## Najczęstsze problemy

- **Plugin się nie ładuje** → sprawdź `meta list` i `css_plugins list`; brak CSS = brak pluginu.
- **Statystyki nie działają** → sprawdź dane MySQL i czy `MySqlConnector.dll` jest w folderze pluginu.
- **`!admin` mówi o braku uprawnień** → dodaj swoje SteamID z flagą `@css/root` w `admins.json`.
