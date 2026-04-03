# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Run the game
dotnet run --project Invaders

# Build only
dotnet build

# Build a specific project
dotnet build Infrastructure
dotnet build Invaders
```

Prerequisites: .NET 8 SDK and MonoGame SDK.

There are no automated tests in this codebase.

## Architecture

The solution has two projects:

- **Infrastructure** — a reusable game framework library (outputs a DLL). Contains base classes, managers, and the object model.
- **Invaders** — the game executable. References Infrastructure and contains all game-specific logic.

### Core framework flow

`Program.cs` → `InvadersManager` (extends `BaseGame`) → manages screens via `ScreensMananger`.

`BaseGame` (Infrastructure) instantiates four singleton managers registered as `Game.Services`:
- `InputManager` — keyboard state polling, accessed per-screen via `IInputManager`
- `CollisionsManager` — tracks collidable objects and raises collision events
- `SoundsManager` — wraps MonoGame audio for SFX and background music
- `ScreensMananger` — maintains a stack of `GameScreen` instances

### Screen system

`ScreensMananger` uses a `Stack<GameScreen>`. Screens are pushed/popped; the top of the stack is active. When a screen closes (calls `ExitScreen()`), it fires a `Closed` event which pops it and reactivates the previous screen. When the stack empties, the game exits.

`GameScreen` has two boolean flags:
- `IsModal` — if true, the previous screen's `Update` is suppressed
- `IsOverlayed` — if true, the previous screen's `Draw` is called first (for overlay effects)

Screens get a `DummyInputManager` (no-ops all input) when they don't have focus, so only the active screen handles input.

### Game-specific screens (Invaders/Screens/)

Screen flow managed by `InvadersManager`:
1. `WelcomeScreen` → main menu with player count selection
2. `NameEntryScreen` → collects player names (shown once per session per player count)
3. `LevelTransitionScreen` → "Level N" splash between levels
4. `PlayScreen` → active gameplay
5. `GameOverScreen` → shows scores, offers restart or leaderboard
6. `LeaderboardScreen` → reads top scores from SQLite
7. `SoundMenuScreen` / `ScreenMenuScreen` / `GamePauseScreen` — overlaid settings/pause menus

### Object model hierarchy

```
IGameComponent
└── GameComponent (MonoGame)
    └── RegisteredComponent       (auto-registers with Game.Services)
        └── GameService
    └── DrawableGameComponent
        └── LoadableDrawableComponent   (manages ContentManager)
            └── Component2D             (adds Position, Rotation, Scale)
                └── Sprite              (texture, color, animations)
                    └── game sprites (Ship, Enemy, Bullet, Barrier, MotherShip...)
        └── CompositeDrawableComponent<T>
            └── GameScreen
                └── MenuScreen          (menu item list with keyboard navigation)
                └── concrete screens
```

### Sprite animations

`Sprite` has a `CompositeAnimator` which chains multiple `SpriteAnimator` instances. Concrete animators in `Infrastructure/ObjectModel/Animators/ConcreteAnimators/`: `CellAnimator` (sprite sheet), `BlinkAnimator`, `FadeAnimator`, `ShrinkAnimator`, `PulseAnimator`, `RotationAnimator`, `WaypointsAnimator`, `SequentialAnimator`.

### Data persistence

`ScoresDatabase` (Infrastructure) wraps SQLite via `Microsoft.Data.Sqlite`. The database file `scores.db` is created in the working directory. Schema: `Scores(Name TEXT, Score INTEGER, Level INTEGER, Date TEXT)`.

### Facebook integration

`FacebookManager` opens an OAuth URL in the system browser. It currently simulates login by setting `UserName = "Facebook User"` — real token exchange is not implemented. The App ID is read from `Invaders/appconfig.json`.

### Content pipeline

Game assets (textures, sounds) are defined in `Content/Content.mgcb` files and compiled by the MonoGame Content Builder Task at build time. Assets are loaded via `ContentManager` using paths like `@"Sprites\BG_Space01_1024x768"`.
