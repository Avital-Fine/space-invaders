# Space Invaders

A classic Space Invaders game built with MonoGame.

## Features

- **Single Player** mode
- **Two Player** mode with shared lives
- **4 Levels** with increasing difficulty
- **Mother Ship** that appears randomly
- **3 Barriers** that degrade with use
- **Sound Effects** for all actions
- **Pause Menu** with game instructions
- **Settings Menu** to adjust volumes
- **Keyboard Controls** for navigation and gameplay

## Controls

### Gameplay Controls

| Action | Player 1 | Player 2 |
|--------|----------|----------|
| Move Left | ← | A |
| Move Right | → | D |
| Shoot | Space | K |
| Pause | P | P |
| Mute Sound | M | M |

### Menu Controls

| Action | Keys |
|--------|------|
| Navigate Up | ↑ |
| Navigate Down | ↓ |
| Select Option | Enter |
| Back / Exit | Escape |

## Installation

1. **Prerequisites**
   - [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
   - [MonoGame SDK](https://www.monogame.net/downloads/)

2. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd space-invaders
   ```

3. **Run the game**
   ```bash
   dotnet run --project Invaders
   ```

## Project Structure

```
space-invaders/
├── Invaders/              # Game code
│   ├── Content/           # Game assets
│   ├── Screens/           # Game screens
│   ├── Sprites/           # Game sprites
│   └── ...
├── Infrastructure/        # Reusable components
│   ├── Managers/          # Managers (Input, Sounds, etc.)
│   ├──ObjectModel/        # Game objects (Screens, Sprites, etc.)
│   └── ...
├── Invaders.sln           # Solution file
└── README.md              # This file
```

## License

[MIT License](LICENSE)