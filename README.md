# Retakes — plugin do Counter-Strike 2

Plugin trybu **Retakes** dla serwerów Counter-Strike 2, napisany w C# na frameworku
[CounterStrikeSharp](https://docs.cssharp.dev/).

## Status

🚧 Wczesny etap — działający szkielet pluginu (rejestracja, komenda `!retakes`,
hook na start rundy). Logika rozstawiania graczy, podkładania bomby i alokacji
broni jest do zaimplementowania.

## Wymagania

- Serwer CS2 z [Metamod:Source](https://www.sourcemm.net/)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/releases)
- .NET 8 SDK (do budowania)

## Budowanie

```bash
dotnet build -c Release
```

Skompilowany plik `Retakes.dll` (z folderu `bin/Release/net8.0/`) skopiuj do:

```
csgo/addons/counterstrikesharp/plugins/Retakes/
```

## Użycie

W konsoli serwera lub na czacie:

```
!retakes      # wyświetla wersję i status pluginu
```

## Plan rozwoju

- [ ] Konfiguracja spawnów per-mapa (T / CT)
- [ ] Automatyczne podkładanie bomby (bombsite A / B)
- [ ] Alokator broni (pistol / force-buy / full-buy)
- [ ] Balans drużyn po rundzie
- [ ] Edytor pozycji w grze
