# FreePair

A cross-platform desktop app for chess tournament pairing.

- **UI**: Avalonia UI 11
- **Runtime**: .NET 10 (LTS)
- **Pairing engine**: [BBP Pairings](https://github.com/BieremaBoyzProgramming/bbpPairings) (Apache 2.0) — external executable, configurable path.

## Build

``` 
dotnet build
dotnet run --project src/FreePair.App
```

## Project layout

- `src/FreePair.Core` — domain models, pairing engine integration, TRF I/O.
- `src/FreePair.App` — Avalonia desktop UI.
- `src/FreePair.App/PairingEngine/` — drop the BBP binary here for dev.
- `tests/FreePair.Core.Tests` — xUnit tests.

## License

TBD
