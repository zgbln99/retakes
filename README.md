# Retakes — rozszerzony plugin do Counter-Strike 2

Rozbudowana wersja trybu **Retakes** dla serwerów Counter-Strike 2 na frameworku
[CounterStrikeSharp](https://docs.cssharp.dev/).

Projekt bazuje na świetnej pracy **[B3none/cs2-retakes](https://github.com/b3none/cs2-retakes)**
i **[B3none/cs2-instadefuse](https://github.com/B3none/cs2-instadefuse)** (oba GPL-3.0),
rozszerzając je o dodatkowe funkcje. Pełna atrybucja w pliku [`NOTICE`](./NOTICE).

> ⚖️ **Fair play:** ten plugin zawiera **tryby gry** (np. asymetryczny tryb „Boss"),
> które działają **jawnie i są ogłaszane wszystkim graczom**. Projekt **nie zawiera
> i nie będzie zawierał** ukrytych cheatów (aimbot/wallhack dawanych po cichu
> wybranym graczom). Każda asymetria jest częścią ogłoszonych zasad trybu.

## Status

✅ **Faza 1 — fundament:**
- [x] Import bazy **cs2-retakes**
- [x] Wbudowany **instadefuse** (zintegrowany jako usługa, włączany z configu)
- [x] CI w GitHub Actions (budowanie obu projektów + artefakt do wgrania)
- [x] Licencja GPL-3.0 + atrybucja

✅ **Faza 2 — panel admina (GUI w grze):**
- [x] Menu admina (`!admin` / `!panel`) jako centrum dowodzenia
- [x] System przełączników on/off (rejestr funkcji + akcje rundy)
- [x] Permisje przez `AdminManager` (konfigurowalne flagi)

✅ **Faza 3 — broń:**
- [x] Wbudowany allocator (losowy przydział: pancerz, karabin, pistolet, granaty, defuser)
- [x] Preferencje gracza przez menu **`!guns`** (karabin T/CT, snajperka)
- [x] Konfigurowalne pule broni + szansa na snajperkę
- [x] Przełączniki broni w panelu admina

🔜 **Kolejne fazy (zaplanowane):**
- [ ] **Statystyki PvP** (K/D, HS%, kto-kogo) — zapis w **MySQL** (baza z DatHost); szkielet modułu już gotowy, do podłączenia
- [ ] **Tryby fun** (symetryczne, jawne): glow dla wszystkich, one-shot dla wszystkich
- [ ] **Boss / Juggernaut Mode** (1 vs reszta, jawnie ogłaszany)
- [ ] **Panel webowy** (edycja configów na hoście)

## Wymagania

- Serwer CS2 z [Metamod:Source](https://www.sourcemm.net/)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/releases) (API ≥ 345)
- .NET 8 SDK (tylko do budowania)

## Budowanie

```bash
dotnet build RetakesPluginShared/RetakesPluginShared.csproj -c Release
dotnet build RetakesPlugin/RetakesPlugin.csproj -c Release
```

CI w GitHub Actions buduje to automatycznie i wystawia gotowy artefakt
(`output/addons/...`) przy każdym pushu.

## Instalacja na serwerze (np. DatHost)

Skopiuj zawartość artefaktu CI do katalogu gry:

```
game/csgo/addons/counterstrikesharp/plugins/RetakesPlugin/
game/csgo/addons/counterstrikesharp/shared/RetakesPluginShared/
```

Spawny per-mapa znajdują się w `RetakesPlugin/map_config/`.

## Konfiguracja

Config generuje się przy pierwszym uruchomieniu w
`addons/counterstrikesharp/configs/plugins/RetakesPlugin/RetakesPlugin.json`.
Sekcja `InstadefuseSettings` steruje wbudowanym instadefuse:

```json
"InstadefuseSettings": {
  "IsEnabled": true,
  "InfernoThreatDistance": 250.0,
  "RequireAllTerroristsDead": true
}
```

## Licencja

GPL-3.0 — zgodnie z licencją projektów źródłowych. Zobacz [`LICENSE`](./LICENSE)
oraz [`NOTICE`](./NOTICE).
