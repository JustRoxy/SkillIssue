# Unfair

`Unfair` is a service responsible for openskill calculations and judgments in general.

# Basic Score Filtering

The following rules apply to scores for them to be considered valid:

1. Accuracy must be greater than 40%.
2. The following mods are not permitted: `Relax`, `Autopilot`, `Autoplay`, `SpunOut`.

# MP Filtering

`Unfair` judges the MP lobby based on the following flag enum:

```csharp
public enum CalculationErrorFlag
{
    NoError = 0,
    NameRegexFailed = 1,
    NotStandardMatchType = 2,
    BannedAcronym = 4,
    TooManyWarmups = 8,
    TooManyHosts = 16,
    NoStandardScores = 32,
    InGameHostedMatch = 64,
    BigHeadOnHeadGame = 128,
    NonSymmetricalTeams = 256,
    TooManyPlayers = 512,
    InsufficientAmountOfGames = 1024,
    TooManyGames = 2048,
    IncorrectAmountOfPlayers = 4096
}
```

### NoError

#### Success

No errors were found.

### NameRegexFailed

#### Failure, match is skipped

**Regular Expression:**

```regexp
(?'acronym'.+):\s*(?'red'\(*.+\)*)\s*vs\s*(?'blue'\(*.+\)*)
```

**General Transcription:**

`<acronym>:<whitespace>`
`(<optional any amount of opening brackets><red team name><optional any amount of closing brackets>)`
`<whitespace> vs <whitespace>`
`(<optional any amount of opening brackets><blue team name><optional any amount of closing brackets>)`

### NotStandardMatchType

#### Failure, match is skipped

There are three official match types: `Qualifications`, `Tryouts`, and `Standard`.

##### Tryouts

**Regular Expression:**

```regexp
Tryouts
```

**General Transcription:**

If the match name contains the string `tryouts` (case-insensitive), then it's marked as a tryouts match.

##### Qualifications

###### Regular Expression 1 (Applied to the red team)

```regexp
^Qual
```

**Comment:**
If the red team name does not start with `Qual`, it's not a qualifiers lobby.

###### Regular Expression 2 (Applied to the whole name)

```regexp
(Lobby|Match)
```

**Comment:**
`xxx: (Qual...) vs (Lobby xx)` is likely a qualifiers lobby.

###### Regular Expression 3 (Applied to the blue team)

```regexp
\d
```

**Comment:**
`xxx: (Qual...) vs (...DIGIT...)` is likely a qualifiers lobby. For example:

* `WDTWE: (Qualifiers) vs (SUN-22)`
* `BRF: (Qualifiers) vs (X10)`

### `BannedAcronym`

#### Failure, match is skipped

To backtrack exceptional lobbies, a system for reporting and blacklisting specific acronyms is required.

**Banned Acronyms:**

1. `ETX` - Matchmaking bot, not a real tournament.
2. `o!mm` - Matchmaking bot, not a real tournament.
3. `PSK` - Reported tournament: Polish scrims, not a real tournament.
4. `TGC` - Reported tournament: Unconventional tournament mappools, making skillsets impossible to calculate.
5. `NDC2` - Reported tournament, unsupported processing: Non-standard scoring system.
6. `FEM2` - Reported tournament, unsupported processing: The tournament involved playing for challenges set by the
   tournament host instead of playing for score. Achieving specific miss counts or similar challenges meant players
   rarely aimed for "good" scores by the normal definition.
7. `ROMAI` - Matchmaking bot, not a real tournament.
8. `MEM` - Reported tournament, unsupported processing: Multimode mappools.

### TooManyWarmups

#### Assumption, some games are skipped

Warmup detection is an important part of score filtering. Warmups are detected using two osu! multiplayer API events:
`host-changed` and `player-left`. If `host-changed` changes to `user_id = 0`, it signifies that `!mp clearhost` was
invoked. The amount of maps played with `user_id != 0` is calculated, and the first 2 scores are ignored, assuming they
are warmups. This flag is set only if there are **more than 2 warmup-detected scores**.

### TooManyHosts

#### Failure, match is skipped

If the warmup detection system finds that **more than 3 players** set the beatmap, it's assumed to be **not** a
tournament lobby, adhering to the one warmup per side rule.

### NoStandardScores

#### Failure, match is skipped

No standard game mode scores were found in the lobby. SkillIssue does not support other game modes.

### InGameHostedMatch

#### Failure, match is skipped

Using the host-changed detection system, lobbies that were **not created with an IRC client** but were created in-game
can be filtered out.

### BigHeadOnHeadGame

#### Failure, game is skipped

**Head-on-head games should only have 2 players participating.** This rule was created to improve the qualification
detection system, as arena-format tournaments were not popular. This rule may be revised later.

### NonSymmetricalTeams

#### Failure, game is skipped

Team vs Team matches must have an equal number of players on the red team and the blue team.

### TooManyPlayers

#### Failure, game is skipped

A game should not have more than 8 players participating.

### InsufficientAmountOfGames

#### Failure, match is skipped

A match must have more than 3 valid games.

### TooManyGames

#### Failure, match is skipped

A match should not have more than 22 games.

### IncorrectAmountOfPlayers

#### Failure, game is skipped

A game should have more than 1 player.