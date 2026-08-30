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

## 3. Broń — stały zestaw (AK-47 / M4A1-S + Deagle)

Wbudowany allocator: pancerz/hełm, defuser dla CT, karabin, pistolet, granaty, nóż.

**Domyślnie losowanie broni jest całkowicie wyłączone.** Każdy dostaje ten sam
zestaw dla swojej drużyny:

| Drużyna | Karabin | Pistolet |
|---|---|---|
| T | AK-47 (`weapon_ak47`) | Deagle (`weapon_deagle`) |
| CT | M4A1-S (`weapon_m4a1_silencer`) | Deagle (`weapon_deagle`) |

Bez snajperek, bez Scouta, bez losowych karabinów i pistoletów. `!guns` jest
wyłączone (nie ma czego wybierać) i odpowiada komunikatem o stałym zestawie.

**Granaty zostają bez zmian (dalej losowe):**
- `MinGrenades` (1) … `MaxGrenades` (2) — losowo tyle granatów na rundę
- `ExtraGrenadeChance` (0.25) — szansa na każdy kolejny granat ponad minimum
- `LonePlayerExtraGrenades` (1) — bonus, gdy gracz zostaje sam na drużynie
- `GrenadeHardCap` (3) — twardy limit; respektuje limity CS2 (2 flashe, reszta po 1)

**Config — `WeaponSettings`:**
- `RandomWeapons` (domyślnie `false`) — główny przełącznik losowania broni.
  `false` = stały zestaw poniżej; `true` = stary tryb (pule, snajperki, `!guns`).
- `TerroristPrimary` / `CounterTerroristPrimary` — stała broń per drużyna.
- `GivePistol` (domyślnie `true`) + `TerroristPistol` / `CounterTerroristPistol` —
  pistolet w zestawie, domyślnie Deagle dla obu drużyn. `false` = sam karabin.
- `AllowPreferences`, `AllowSnipers`, `AllowScout`, `SniperChance` — domyślnie
  wyłączone / zero; działają tylko przy `RandomWeapons = true`.
- Pule `TerroristRifles` / `CounterTerroristRifles` / `Snipers` / `*Pistols` są
  używane wyłącznie w trybie losowym.
- Pule granatów per drużyna oraz opcje pancerza/defusera — bez zmian.

Toggles w panelu admina i w menu admina: allocator on/off, „Losowe bronie" on/off,
„Pistolet w zestawie" on/off, preferencje on/off.

**Powrót do losowania:** `weapon.random` = `1` (panel / `css_rcon_setcfg`) albo
`"RandomWeapons": true` w configu.

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
