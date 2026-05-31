# CWELOWNIA — pełna lista funkcji

Plugin retakes do CS2 (CounterStrikeSharp), oparty na B3none/cs2-retakes (GPL-3.0),
rozbudowany o dodatkowe moduły. Poniżej wszystkie funkcje wraz z komendami,
domyślnymi ustawieniami i miejscem w configu.

> Config: `addons/counterstrikesharp/configs/plugins/RetakesPlugin/RetakesPlugin.json`
> Prefiks czatu: `[CWELOWNIA]`

---

## 1. Tryb Retakes (baza)

Rdzeń rozgrywki: rozstawianie graczy, kolejka, balans drużyn, podkładanie bomby.

| Funkcja | Komenda | Uprawnienia |
|---|---|---|
| Wymuś bombsite A/B | `!forcebombsite A` / `B` | `@css/root` |
| Przestań wymuszać bombsite | `!forcebombsitestop` | `@css/root` |
| Scramble drużyn (następna runda) | `!scramble` / `!scrambleteams` | `@css/admin` |
| Debug kolejek | `!debugqueues` | `@css/root` |
| Wł./wył. ogłoszeń głosowych bombsite | `!voices` | gracz |

**Edytor spawnów** (admin, `@css/root`): `!edit`/`!spawns`, `!add`, `!remove`,
`!nearest`, `!done`. Konfiguracja per-mapa w `map_config/`.

**Config:** `GameSettings`, `QueueSettings`, `TeamSettings`, `MapConfigSettings`, `BombSettings`.

---

## 2. Instadefuse (wbudowany)

Gdy ostatni CT zostaje sam z bombą i zaczyna rozbrajać — rozbrojenie kończy się
natychmiast, o ile nie ma zagrożenia (HE/molotov/ogień przy bombie).

**Config — `InstadefuseSettings`:**
- `IsEnabled` (true) — wł./wył.
- `InfernoThreatDistance` (250) — w jakiej odległości ogień blokuje instadefuse
- `RequireAllTerroristsDead` (true) — instadefuse tylko gdy wszyscy T martwi

Sterowanie też z panelu admina (toggle).

---

## 3. Broń — losowość + preferencje

Wbudowany allocator: pancerz/hełm, defuser dla CT, karabin, pistolet, granaty, nóż.

| Funkcja | Komenda | Uprawnienia |
|---|---|---|
| Menu wyboru broni (GUI) | `!guns` / `!gun` / `!weapon` | gracz |

**Losowość:**
- Karabin losowany z puli drużyny; szansa na snajperkę (`SniperChance`, domyślnie 12%)
- Preferencje gracza (`!guns`): ulubiony karabin T/CT, „preferuj snajperkę"

**Granaty (losowa liczba):**
- `MinGrenades` (1) … `MaxGrenades` (3) — losowo tyle granatów na rundę
- `LonePlayerExtraGrenades` (2) — bonus, gdy gracz zostaje sam na drużynie
- `GrenadeHardCap` (4) — twardy limit; respektuje limity CS2 (2 flashe, reszta po 1)

**Config — `WeaponSettings`:** pule broni/pistoletów/granatów per drużyna, toggle
`IsEnabled`, `AllowPreferences`, `AllowSnipers`, opcje pancerza/defusera.

Toggles w panelu admina: allocator on/off, preferencje on/off.

---

## 4. Statystyki PvP (MySQL)

Zliczanie zabójstw / śmierci / asyst / HS% / rund. Zapis do MySQL (baza z DatHost).
Całe I/O asynchroniczne — nie blokuje serwera; awaria bazy → moduł sam się wyłącza.

| Funkcja | Komenda | Uprawnienia |
|---|---|---|
| Twoje statystyki | `!rank` / `!stats` | gracz |
| Ranking serwera | `!top` | gracz |

**Config — `StatsSettings`:** `IsEnabled` (domyślnie false — włącz po wpisaniu danych
MySQL), `Database` (Host/Port/User/Password/Name/TablePrefix), `FlushIntervalSeconds`,
`LeaderboardSize`. Toggle w panelu admina.

---

## 5. Głosowanie na mapę (Rock The Vote)

Gracze sami decydują o zmianie mapy.

| Funkcja | Komenda | Uprawnienia |
|---|---|---|
| Zagłosuj za zmianą mapy | `!rtv` / `!votemap` | gracz |
| Wymuś głosowanie | panel admina → „Rozpocznij głosowanie" | `@css/root` |

Gdy odpowiedni odsetek graczy wpisze `!rtv`, otwiera się głosowanie (menu) dla
wszystkich, a wygrana mapa się ładuje.

**Config — `MapVoteSettings`:** `IsEnabled`, `RtvRatio` (0.6 = 60%),
`VoteDurationSeconds` (25), `MapsInVote` (5), `ChangeDelaySeconds` (5), `Maps` (pula).
Toggle w panelu admina.

---

## 6. Panel admina (GUI w grze)

| Funkcja | Komenda | Uprawnienia |
|---|---|---|
| Otwórz panel | `!admin` / `!panel` | `@css/root` (konfig.) |

**Zawiera:**
- **Funkcje (on/off):** instadefuse, allocator broni, preferencje broni, statystyki, głosowanie na mapę
- **Akcje rundy:** scramble, force bombsite A/B, stop, rozpocznij głosowanie na mapę
- **Wybór zestawu broni:** wymuś globalny loadout (karabiny / pistolety / deagle / AWP / scout) lub powrót do losowego — symetrycznie dla wszystkich, ogłaszane na czacie

**Config — `AdminMenuSettings`:** `IsEnabled`, `PermissionFlags` (`@css/root`),
`OpenCommands` (`css_admin`, `css_panel`).

> Uprawnienia admina ustawiasz w `addons/counterstrikesharp/configs/admins.json`.

---

## Szybka ściąga komend

**Gracz:** `!guns` · `!rank` · `!stats` · `!top` · `!rtv` · `!voices`
**Admin:** `!admin` · `!scramble` · `!forcebombsite A/B` · `!forcebombsitestop` · `!edit`/`!add`/`!remove`

---

## W planach (do ustalenia)

- Wybór broni klawiszami 1/2/3 + automatyczne podpowiedzi na starcie rundy
- Komunikaty na ekranie (HUD): pozycja bomby, „X dominuje Y", serie zabójstw, info o trybach
- Panel webowy na VPS (zdalne zarządzanie serwerem)
- Tryby fun (symetryczne): glow dla wszystkich, one-shot dla wszystkich
- Boss / Juggernaut Mode (1 vs reszta, jawnie ogłaszany)
