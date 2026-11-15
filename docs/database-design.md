# Server-owned table specifications

Proposed relational tables for server-owned values currently stored outside `Users` in `data/db.json`. Table names use PascalCase.

## ServerSettings

Singleton row with `Id = 1`.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | Integer | Primary key; value is `1` | New key |
| `LogLevel` | Integer | Not null | `CoreInfo.LogLevel` |
| `MaxInterceptionCount` | Integer | Not null | `CoreInfo.MaxInterceptionCount` |
| `ResetHourUtcTime` | Integer | Not null | `CoreInfo.ResetHourUtcTime` |

## ServerSecrets

Singleton row with `Id = 1`.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | Integer | Primary key; value is `1` | New key |
| `LauncherTokenKey` | Blob | Not null | `CoreInfo.LauncherTokenKey` |
| `EncryptionTokenKey` | Blob | Not null | `CoreInfo.EncryptionTokenKey` |

## EventBanners

One row per banner; active banners are selected with `IsActive = true`.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `BannerId` | String | Primary key | `CoreInfo.ActiveEventBannerIds` |
| `IsActive` | Boolean | Not null | New active-state field |

The current C# property is `List<int>`, while this table uses string IDs. Convert existing IDs to invariant decimal strings during migration, and use strings in the new application path.

## TtsSongScores

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key; generated per row | New identifier |
| `UserId` | Integer (64-bit) | Not null; part of unique constraint | `RankDatas.SongRankDatas[*].UserId` |
| `SongId` | Integer | Not null; part of unique constraint | `RankDatas.SongRankDatas[*].SongId` |
| `RankType` | Integer | Not null; part of unique constraint | `RankDatas.SongRankDatas[*].RankType` |
| `Difficulty` | Integer | Not null; part of unique constraint | `RankDatas.SongRankDatas[*].Difficulty` |
| `Score` | Integer | Not null | `RankDatas.SongRankDatas[*].Score` |
| `UpdateTime` | Integer (64-bit) | Not null | `RankDatas.SongRankDatas[*].UpdateTime` |
| `IsDeleted` | Integer | Not null | `RankDatas.SongRankDatas[*].IsDeleted` |

Unique constraint: `(UserId, SongId, RankType, Difficulty)`. Confirm this tuple identifies one logical score before applying it; add an index on `(SongId, RankType, Difficulty, Score)` for leaderboard reads.

## TtsTotalScores

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key; generated per row | New identifier |
| `UserId` | Integer (64-bit) | Not null; part of unique constraint | `RankDatas.TtsRankDatas.TtsTotalRankRecords[*].UserId` |
| `RankType` | Integer | Not null; part of unique constraint | `RankDatas.TtsRankDatas.TtsTotalRankRecords[*].RankType` |
| `Score` | Integer (64-bit) | Not null | `RankDatas.TtsRankDatas.TtsTotalRankRecords[*].Score` |
| `UpdatedAt` | Integer (64-bit) | Not null | `RankDatas.TtsRankDatas.TtsTotalRankRecords[*].UpdatedAt` |

Unique constraint: `(UserId, RankType)`. Add an index on `(RankType, Score)` for leaderboard ordering. Do not persist rank position; derive it from scores when querying.

## ArcadeScores

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | Integer | Primary key | `RankDatas.ArcadeScore[*][*].Id` |
| `GuildId` | Integer (64-bit) | Not null | `RankDatas.ArcadeScore[*][*].GuildId` |
| `UserId` | Integer (64-bit) | Not null | `RankDatas.ArcadeScore[*][*].UserId` |
| `Score` | Integer (64-bit) | Not null | `RankDatas.ArcadeScore[*][*].Score` |
| `ArcadeId` | Integer | Not null | `RankDatas.ArcadeScore[*][*].ArcadeId` |
| `ModeId` | Integer | Not null | `RankDatas.ArcadeScore[*][*].ModeId` |

Add indexes on `(ArcadeId, ModeId, Score)` and `UserId`. Confirm whether `Id` identifies score history or whether scores are upserted by a different key before implementation.

## UserBans

One row per ban record. Existing User data currently stores one set of ban fields per user.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key; generated per row | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | `Users[*].ID` |
| `IsBanned` | Boolean | Not null | `Users[*].IsBanned` |
| `BanStart` | Date/time | Not null | `Users[*].BanStart` |
| `BanEnd` | Date/time | Not null | `Users[*].BanEnd` |
| `BanId` | Integer | Not null | `Users[*].BanId` |

Add an index on `UserId`.

## User profile table design

Store the profile on the existing `Users` row (`GameUser`). This is a one-to-one extension of the user record, so a separate `UserProfile` table would add a join without a separate lifecycle. This design covers the user's displayed identity and selected cosmetics; unlocked cosmetic collections and profile-card layouts remain outside this profile table.

| Column | Type | Constraints | Current system status | Source |
|---|---|---|---|---|
| `ID` | Integer (64-bit) | Primary key | Exists | `Users[*].ID` |
| `Nickname` | String | Not null | Exists | `Users[*].Nickname` |
| `Description` | String | Not null | Exists | `Users[*].Description` |
| `ProfileIconId` | Integer | Not null | New column | `Users[*].ProfileIconId` |
| `ProfileIconIsPrism` | Boolean | Not null | New column | `Users[*].ProfileIconIsPrism` |
| `ProfileFrame` | Integer | Not null | New column | `Users[*].ProfileFrame` |
| `TitleId` | Integer | Not null | New column | `Users[*].TitleId` |
| `LastNormalStageCleared` | Integer | Not null | New column | `Users[*].LastNormalStageCleared` |
| `LastStoryStageCleared` | Integer | Not null | New column | `Users[*].LastStoryStageCleared` |
| `LastHardStageCleared` | Integer | Not null | New column | `Users[*].LastHardStageCleared` |
| `LastClearedDifficulty` | Integer | Not null | New column | `Users[*].LastClearedDifficulty` |
| `LastReset` | Date/time | Not null | New column | `Users[*].LastReset` |
| `LastWeeklyReset` | Date/time | Not null | New column | `Users[*].LastWeeklyReset` |
| `LastTriggerId` | Integer | Not null | New column | `Users[*].LastTriggerId` |
| `LastMessageId` | Integer (64-bit) | Not null | New column | `Users[*].LastMessageId` |
| `LastBadgeSeq` | Integer (64-bit) | Not null | New column | `Users[*].LastBadgeSeq` |
| `BattleTime` | Date/time | Not null | New column | `Users[*].BattleTime` |
| `CurrentLiberateCharacterId` | Integer | Not null | New column; renamed from source key | `Users[*].CurCharacterIdId` |
| `DailyDiscountUsed` | Boolean | Not null | New column | `Users[*].DailyDiscountUsed` |
| `SickPullsEnabled` | Boolean | Not null | New column; renamed from source key | `Users[*].sickpulls` |

The progress, reset, and sequence checkpoint fields stay on the existing `Users` row; a separate state table is not proposed. `LastLogin` remains on `SdkUsers`, where it already exists.

### SdkUsers account table

Account and login fields stay on the existing `SdkUsers` row keyed by the same user ID. Account endpoints query by email and use these fields to build login/account responses; admin authorization queries `IsAdmin` by user ID. CLI and admin paths that currently read legacy `User.Username` should use `SdkUsers.Email` instead.

| Column | Type | Constraints | Current system status | Source |
|---|---|---|---|---|
| `ID` | Integer (64-bit) | Primary key; shared user identifier | Exists | `Users[*].ID` |
| `Email` | String | Not null; unique after case-matching policy is resolved | Exists | `SdkUsers.Email`; legacy `Users[*].Username` |
| `PasswordHash` | String | Not null; must contain a verifier hash, never plaintext | Exists; current verification paths use inconsistent formats | `SdkUsers.PasswordHash`; legacy `Users[*].Password` requires conversion |
| `PlayerName` | String | Not null | Exists | `SdkUsers.PlayerName`; legacy `Users[*].PlayerName` |
| `RegisterTime` | Integer (64-bit, Unix seconds) | Not null | Exists | `SdkUsers.RegisterTime`; legacy `Users[*].RegisterTime` |
| `IsAdmin` | Boolean | Not null | Exists | `SdkUsers.IsAdmin`; legacy `Users[*].IsAdmin` |
| `LastLogin` | Date/time | Not null | Exists | `SdkUsers.LastLogin` |

Keep one authoritative account value in `SdkUsers`; remove duplicated legacy fields from JSON only after CLI, registration, and admin code paths use this row. Credential migration has a security and compatibility dependency: `AccountController` currently compares the submitted password directly, while the admin API hashes before comparing, and the legacy registration command stores plaintext JSON without creating an `SdkUsers` row. Unify credential verification and hashing before converting or deleting those passwords.

## Outposts table design

Store Dispatch, Infra, and Synchro state in one `Outposts` row per user. `UserId` is both the primary key and a foreign key to `Users.ID`.

| Column | Type | Constraints | Current system status | Source |
|---|---|---|---|---|
| `UserId` | Integer (64-bit) | Primary key; foreign key to `Users.ID` | New column | `Users[*].ID` |
| `DispatchLv` | Integer | Not null | New column | `Users[*].DispatchLv` |
| `DispatchCollectionLv` | Integer | Not null | New column | `Users[*].DispatchCollectionLv` |
| `DispatchFavoriteLv` | Integer | Not null | New column | `Users[*].DispatchFavoriteLv` |
| `DispatchResetCount` | Integer | Not null | New column | `Users[*].DispatchResetCount` |
| `DispatchToday` | Integer | Not null | New column | `Users[*].UserDispatchData.Today` |
| `InfraCoreExp` | Integer | Not null | New column | `Users[*].InfraCoreExp` |
| `InfraCoreLvl` | Integer | Not null | New column | `Users[*].InfraCoreLvl` |
| `SynchroDeviceUpgraded` | Boolean | Not null | New column | `Users[*].SynchroDeviceUpgraded` |
| `SynchroDeviceLevel` | Integer | Not null | New column | `Users[*].SynchroDeviceLevel` |
| `OutpostBattleLevel` | Integer | Not null | New column | `Users[*].OutpostBattleLevel.Level` |
| `OutpostBattleExp` | Integer | Not null | New column | `Users[*].OutpostBattleLevel.Exp` |
| `OutpostBuffs` | JSON text (object) | Not null | New column | `Users[*].OutpostBuffs` |
| `OutpostConditionList` | JSON text (array) | Not null | New column | `Users[*].OutpostConditionList` |

None of these columns currently exist in the relational schema. `OutpostBattleLevel.IsLevelUp` is omitted because the code sets it on the response when a level-up occurs; it is transient response state, not saved progress.

### Collection and object storage decisions

The current runtime loads and saves the full `User` object through `JsonDb`; these recommendations describe the target relational design, not the current persistence implementation. Collections below are modeled separately when the code addresses their members independently. Proposed child rows use ULID primary keys and a `UserId` foreign key to `Users.ID`; uniqueness constraints are included only where current behavior establishes the natural key.

| Current value | How the code uses it | Keep embedded pros and cons | Recommendation |
|---|---|---|---|
| `DispatchClearList` (array of dispatch IDs) | Checks membership, adds unique IDs, and resets the list each dispatch day. | JSON is compact and easy to load with dispatch state; membership and uniqueness are application-only and cannot be constrained or queried with ordinary relational joins. | Extract to `DispatchClearItems` (`DispatchId`; unique `UserId` + `DispatchId`). It is a set in behavior, so a relational unique constraint directly protects that invariant. |
| `SelectableDispatchData` (array of selectable dispatch records) | Records are added, filtered, and removed by dispatch/task IDs; reset with the daily list. | JSON preserves the existing response shape and avoids row mapping; individual state changes rewrite the full value and cannot be indexed or independently constrained. | Extract to `SelectableDispatches`, with the record fields (`DispatchGroupId`, `SelectTid`, `SelectSlotId`, `IsRun`, `StartAt`, `EndAt`). Add a non-unique index on `UserId` + `SelectTid`; current handlers append entries without checking for duplicates. |
| `UserDispatchData` (object with `Today` and `dispatchDatas`) | `Today` controls daily reset; dispatch rows are added, filtered, updated, and removed individually. | One JSON value makes the aggregate easy to save atomically; it mixes a scalar checkpoint with a mutable collection and forces whole-object updates. | Flatten `Today` to `Outposts.DispatchToday`; extract `dispatchDatas` to `Dispatches` with `Tid`, `IsRun`, `StartAt`, and `EndAt`. `UserId` + `Tid` should be unique if one task can appear only once per user. |
| `InfraCoreRewardReceived` (map of level to bool) | Checks and marks the current level's reward as received. | JSON is very small and directly represents the dictionary; it cannot enforce one record per user/level or support reporting claimed levels. | Extract received entries to `InfraCoreRewards` (`InfraCoreLevel`); store only claimed levels, so a boolean column is unnecessary. Unique `UserId` + `InfraCoreLevel` prevents duplicate claims. |
| `SynchroSlots` (array of slot records) | Looked up by slot number and character serial; slots are registered, changed, and unregistered independently. | JSON avoids loading slot rows and preserves order; however slots have stable identifiers and are mutated individually. | Extract to `SynchroSlots` with `Slot`, `CharacterSerialNumber`, and `AvailableAt`; unique `UserId` + `Slot`. |
| `ResearchProgress` (map of research ID to progress) | Read and updated by research ID; progress level and stats change separately; returned as individual research entries. | JSON is convenient for loading the complete research state; map keys and values are independently addressed and cannot have foreign-key or uniqueness enforcement. | Extract to `ResearchProgress` with `ResearchId`, `Level`, `Attack`, `Defense`, and `Hp`; unique `UserId` + `ResearchId`. |
| `OutpostBuildings` (array of building records) | Found, replaced, and moved by `SlotId`; the lobby returns entries individually. | JSON is easy to send as one list; slot identity is already a natural key, and edits rewrite the whole collection. | Extract to `OutpostBuildings` with `SlotId`, `BuildingId`, `IsDone`, `StartAt`, and `CompleteAt`; unique `UserId` + `SlotId`. |
| `OutpostBattleLevel` (object) | Code reads and increments `Level` and `Exp`; `IsLevelUp` is set while building a response. | Keeping the object matches the protocol shape, but adds JSON handling for two persistent scalar values and mixes in a transient flag. | Store `Level` and `Exp` as columns on `Outposts`; do not persist `IsLevelUp`. |
| `OutpostBuffs` (object containing four integer arrays) | Appends reward values to a category, sums them for rewards, and emits each entry in responses. | JSON preserves category and entry ordering with minimal mapping; individual values are not currently searched or edited, though JSON limits SQL aggregation and constraints. | Keep as JSON text for now. Its current behavior uses each category as an append-only aggregate; a child table adds row/order management without a demonstrated relational query need. Revisit if admins need per-entry queries or edits. |
| `OutpostConditionList` (array of integer IDs) | No reads or writes found outside its model declaration in the current code. | JSON avoids inventing constraints and preserves unknown semantics; relational modeling could improve querying later, but the invariant and access pattern are not established. | Keep as JSON text pending discovery of its behavior. |

The clear relational candidates are the independently addressed Dispatch, Synchro, Research, and Building records, plus claimed Infra Core levels. Keep only the buff aggregate and currently unused condition list embedded. These are design recommendations; implementing them requires migration of existing JSON data and updating `JsonDb`/user persistence as one coordinated change.

### Proposed child table specifications

Every child table has a ULID primary key and a required `UserId` foreign key to `Outposts.UserId` (which references `Users.ID`). Unique constraints listed below preserve the collection's natural identity. These tables and columns are new to the relational schema.

#### DispatchClearItems

One row per dispatch task the user has cleared for the current dispatch day.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Outposts.UserId` | `Users[*].ID` |
| `DispatchId` | Integer | Not null; unique with `UserId` | `Users[*].DispatchClearList[*]` |

Unique constraint: (`UserId`, `DispatchId`). Clear these rows when `DispatchToday` rolls over.

#### SelectableDispatches

One row per selectable dispatch entry.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Outposts.UserId` | `Users[*].ID` |
| `DispatchGroupId` | Integer | Not null | `Users[*].SelectableDispatchData[*].DispatchGroupId` |
| `SelectTid` | Integer | Not null | `Users[*].SelectableDispatchData[*].SelectTid` |
| `SelectSlotId` | Integer | Not null | `Users[*].SelectableDispatchData[*].SelectSlotId` |
| `IsRun` | Boolean | Not null | `Users[*].SelectableDispatchData[*].IsRun` |
| `StartAt` | Integer (64-bit) | Not null | `Users[*].SelectableDispatchData[*].StartAt` |
| `EndAt` | Integer (64-bit) | Not null | `Users[*].SelectableDispatchData[*].EndAt` |

Add a non-unique index on (`UserId`, `SelectTid`) for the code's lookup and removal pattern. Do not enforce uniqueness until duplicate behavior is resolved: current handlers append entries without checking for an existing `SelectTid`.

#### Dispatches

One row per dispatch task in the user's daily dispatch list.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Outposts.UserId`; unique with `Tid` | `Users[*].ID` |
| `Tid` | Integer | Not null; unique with `UserId` | `Users[*].UserDispatchData.dispatchDatas[*].Tid` |
| `IsRun` | Integer | Not null | `Users[*].UserDispatchData.dispatchDatas[*].IsRun` |
| `StartAt` | Integer (64-bit) | Not null | `Users[*].UserDispatchData.dispatchDatas[*].StartAt` |
| `EndAt` | Integer (64-bit) | Not null | `Users[*].UserDispatchData.dispatchDatas[*].EndAt` |

Unique constraint: (`UserId`, `Tid`). The current code selects and removes dispatch entries by `Tid`, treating it as unique within one user's list.

#### InfraCoreRewards

One row per claimed Infra Core level reward; unclaimed levels have no row.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Outposts.UserId`; unique with `InfraCoreLevel` | `Users[*].ID` |
| `InfraCoreLevel` | Integer | Not null; unique with `UserId` | `Users[*].InfraCoreRewardReceived` map key where value is `true` |

Unique constraint: (`UserId`, `InfraCoreLevel`).

#### SynchroSlots

One row per Synchro slot.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Outposts.UserId`; unique with `Slot` | `Users[*].ID` |
| `Slot` | Integer | Not null; unique with `UserId` | `Users[*].SynchroSlots[*].Slot` |
| `CharacterSerialNumber` | Integer (64-bit) | Not null | `Users[*].SynchroSlots[*].CharacterSerialNumber` |
| `AvailableAt` | Integer (64-bit) | Not null | `Users[*].SynchroSlots[*].AvailableAt` |

Unique constraint: (`UserId`, `Slot`).

#### ResearchProgress

One row per research ID with its current progress.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Outposts.UserId`; unique with `ResearchId` | `Users[*].ID` |
| `ResearchId` | Integer | Not null; unique with `UserId` | `Users[*].ResearchProgress` map key |
| `Level` | Integer | Not null | `Users[*].ResearchProgress[ResearchId].Level` |
| `Attack` | Integer | Not null | `Users[*].ResearchProgress[ResearchId].Attack` |
| `Defense` | Integer | Not null | `Users[*].ResearchProgress[ResearchId].Defense` |
| `Hp` | Integer | Not null | `Users[*].ResearchProgress[ResearchId].Hp` |

Unique constraint: (`UserId`, `ResearchId`).

#### OutpostBuildings

One row per building slot.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Outposts.UserId`; unique with `SlotId` | `Users[*].ID` |
| `SlotId` | Integer | Not null; unique with `UserId` | `Users[*].OutpostBuildings[*].SlotId` |
| `BuildingId` | Integer | Not null | `Users[*].OutpostBuildings[*].BuildingId` |
| `IsDone` | Boolean | Not null | `Users[*].OutpostBuildings[*].IsDone` |
| `StartAt` | Integer (64-bit) | Not null | `Users[*].OutpostBuildings[*].StartAt` |
| `CompleteAt` | Integer (64-bit) | Not null | `Users[*].OutpostBuildings[*].CompleteAt` |

Unique constraint: (`UserId`, `SlotId`).

`OutpostBuffs` and `OutpostConditionList` remain JSON text columns on `Outposts`; their current behavior does not justify child-table row management or establish a relational key.

## SoloRaid table design

The Release publish `data/db.json` contains `SoloRaidData` keyed by raid ID. Each raid stores counters and a list of levels; each level stores progress and battle logs; each log stores a team snapshot. These are independently looked up and replaced by raid, level, type, and open/closed state in `SoloRaidHelper`, so they should be relational rows rather than one JSON column. ULIDs are row primary keys; natural keys preserve the identities used by the current behavior.

### SoloRaids

One row per user and raid. `RaidId` remains the source/game identifier, while `Id` is the generated row identifier.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID`; unique with `RaidId` | `Users[*].ID` |
| `RaidId` | Integer | Not null; unique with `UserId` | `SoloRaidData` map key / `RaidId` |
| `RaidOpenCount` | Integer | Not null | `SoloRaidData[*].RaidOpenCount` |
| `TrialCount` | Integer | Not null | `SoloRaidData[*].TrialCount` |
| `LastDateDay` | Integer | Not null | `SoloRaidData[*].LastDateDay` |

Unique constraint: (`UserId`, `RaidId`). The counts reset when `LastDateDay` advances; this reset remains an application operation in the target design.

### SoloRaidLevels

One row per raid level, mode/type, and open-state variant. The code can retain a closed cleared-level record and a separate open attempt, so `IsOpen` belongs in the unique key.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `SoloRaidId` | ULID (26-character string) | Not null; foreign key to `SoloRaids.Id` | Parent raid row |
| `RaidLevel` | Integer | Not null; part of unique constraint | `SoloRaidLevels[*].RaidLevel` |
| `RaidJoinCount` | Integer | Not null | `SoloRaidLevels[*].RaidJoinCount` |
| `Hp` | Integer (64-bit) | Not null | `SoloRaidLevels[*].Hp` |
| `TotalDamage` | Integer (64-bit) | Not null | `SoloRaidLevels[*].TotalDamage` |
| `IsClear` | Boolean | Not null | `SoloRaidLevels[*].IsClear` |
| `Status` | Integer / enum | Not null | `SoloRaidLevels[*].Status` |
| `Type` | Integer / enum | Not null; part of unique constraint | `SoloRaidLevels[*].Type` |
| `IsOpen` | Boolean | Not null; part of unique constraint | `SoloRaidLevels[*].IsOpen` |

Unique constraint: (`SoloRaidId`, `RaidLevel`, `Type`, `IsOpen`). Keep `Status` as the stored game enum value rather than duplicating it as multiple booleans.

### SoloRaidLogs and SoloRaidLogCharacters

Logs are append-only per level attempt and returned in their original order. Store one row per log and one child row per character in that log; the character fields are snapshots, not foreign keys to the user's current character state.

| Table | Column | Type | Constraints | Source |
|---|---|---|---|---|
| `SoloRaidLogs` | `Id` | ULID (26-character string) | Primary key | New identifier |
| `SoloRaidLogs` | `SoloRaidLevelId` | ULID (26-character string) | Not null; foreign key to `SoloRaidLevels.Id` | Parent level row |
| `SoloRaidLogs` | `Sequence` | Integer | Not null; unique with `SoloRaidLevelId` | Array order in `Logs` |
| `SoloRaidLogs` | `Damage` | Integer (64-bit) | Not null | `Logs[*].Damage` |
| `SoloRaidLogs` | `Kill` | Boolean | Not null | `Logs[*].Kill` |
| `SoloRaidLogCharacters` | `Id` | ULID (26-character string) | Primary key | New identifier |
| `SoloRaidLogCharacters` | `SoloRaidLogId` | ULID (26-character string) | Not null; foreign key to `SoloRaidLogs.Id` | Parent log row |
| `SoloRaidLogCharacters` | `Slot` | Integer | Not null; unique with `SoloRaidLogId` | `Team[*].Slot` |
| `SoloRaidLogCharacters` | `Csn` | Integer (64-bit) | Not null | `Team[*].Csn` |
| `SoloRaidLogCharacters` | `Tid` | Integer | Not null | `Team[*].Tid` |
| `SoloRaidLogCharacters` | `Lv` | Integer | Not null | `Team[*].Lv` |
| `SoloRaidLogCharacters` | `Combat` | Integer | Not null | `Team[*].Combat` |
| `SoloRaidLogCharacters` | `CostumeId` | Integer | Not null | `Team[*].CostumeId` |

Use unique constraints (`SoloRaidLevelId`, `Sequence`) and (`SoloRaidLogId`, `Slot`). This preserves log order and the slot identity while allowing the same slot across separate battles.

## SoloRaidMuseum table design

`SoloRaidMuseumData` is keyed by stage. Each stage has stage-level state, two independent mode states (`Challenge` and `NoLimit`), claimed mission IDs, saved team layouts, and both current-run and best-run log snapshots. Current logs can be seeded from the prior best logs, updated team-by-team, and copied to best logs only when a complete run sets a record. The schema keeps these two log sets explicit so updates cannot accidentally overwrite the best snapshot.

### SoloRaidMuseumStages

One row per user and museum stage.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID`; unique with `StageId` | `Users[*].ID` |
| `StageId` | Integer | Not null; unique with `UserId` | `SoloRaidMuseumData` map key / `StageId` |
| `StageMode` | Integer / enum | Not null | `StageMode` |
| `IsNoLimitUnlocked` | Boolean | Not null | `IsNoLimitUnlocked` |
| `DebugDamageMultiplier` | Decimal | Not null; test-only setting | `DebugDamageMultiplier` |

Unique constraint: (`UserId`, `StageId`). `DebugDamageMultiplier` exists for local-server testing; omit it from a production schema if that test control is not meant to be persistent.

### SoloRaidMuseumModes

One row per stage and mode. `ModeType` distinguishes Challenge from NoLimit.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `SoloRaidMuseumStageId` | ULID (26-character string) | Not null; foreign key to `SoloRaidMuseumStages.Id`; unique with `ModeType` | Parent stage row |
| `ModeType` | Integer / enum | Not null; unique with `SoloRaidMuseumStageId` | `Challenge` or `NoLimit` |
| `StageJoinCount` | Integer | Not null | Mode `StageJoinCount` |
| `TotalDamage` | Integer (64-bit) | Not null | Mode `TotalDamage` |
| `TotalStep` | Integer | Not null | Mode `TotalStep` |
| `BestDamage` | Integer (64-bit) | Not null | Mode `BestDamage` |
| `BestStep` | Integer | Not null | Mode `BestStep` |
| `IsInProgress` | Boolean | Not null | Mode `IsInProgress` |

Unique constraint: (`SoloRaidMuseumStageId`, `ModeType`). `TotalDamage` and `TotalStep` represent the currently open run, while best values change only after a completed five-team record.

### SoloRaidMuseumMissionClaims

One row per claimed mission. Storing only claimed missions replaces the two integer arrays without a redundant boolean.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `SoloRaidMuseumStageId` | ULID (26-character string) | Not null; foreign key to `SoloRaidMuseumStages.Id` | Parent stage row |
| `ModeType` | Integer / enum | Not null; part of unique constraint | Challenge or NoLimit claim list |
| `MissionId` | Integer | Not null; part of unique constraint | `ReceivedChallengeMissions[*]` / `ReceivedNoLimitMissions[*]` |

Unique constraint: (`SoloRaidMuseumStageId`, `ModeType`, `MissionId`).

### SoloRaidMuseumTeams and SoloRaidMuseumTeamSlots

Saved teams are stage-scoped; the runtime falls back to legacy user-wide teams only when the stage has no configured team slots. Slot rows preserve the team layout and character assignment.

| Table | Column | Type | Constraints | Source |
|---|---|---|---|---|
| `SoloRaidMuseumTeams` | `Id` | ULID (26-character string) | Primary key | New identifier |
| `SoloRaidMuseumTeams` | `SoloRaidMuseumStageId` | ULID (26-character string) | Not null; foreign key to `SoloRaidMuseumStages.Id` | Parent stage row |
| `SoloRaidMuseumTeams` | `TeamNumber` | Integer | Not null; unique with `SoloRaidMuseumStageId` | `Teams[*].TeamNumber` |
| `SoloRaidMuseumTeamSlots` | `Id` | ULID (26-character string) | Primary key | New identifier |
| `SoloRaidMuseumTeamSlots` | `SoloRaidMuseumTeamId` | ULID (26-character string) | Not null; foreign key to `SoloRaidMuseumTeams.Id` | Parent team row |
| `SoloRaidMuseumTeamSlots` | `Slot` | Integer | Not null; unique with `SoloRaidMuseumTeamId` | `Teams[*].Slots[*].Slot` |
| `SoloRaidMuseumTeamSlots` | `Value` | Integer (64-bit) | Not null | `Teams[*].Slots[*].Value` |
| `SoloRaidMuseumTeamSlots` | `ValueType` | Integer | Not null | `Teams[*].Slots[*].ValueType` |

Unique constraints: (`SoloRaidMuseumStageId`, `TeamNumber`) and (`SoloRaidMuseumTeamId`, `Slot`).

### SoloRaidMuseumOpenTeams

The selected teams for an active run are a distinct list (up to four before the new team is entered). Keep them as rows so membership and order are explicit.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `SoloRaidMuseumModeId` | ULID (26-character string) | Not null; foreign key to `SoloRaidMuseumModes.Id`; unique with `TeamNumber` | Parent mode row |
| `TeamNumber` | Integer | Not null; unique with `SoloRaidMuseumModeId` | `OpenTeams[*]` |
| `Sequence` | Integer | Not null; unique with `SoloRaidMuseumModeId` | Array order in `OpenTeams` |

The unique key prevents duplicate team selections; `Sequence` preserves request order.

### SoloRaidMuseumLogs and SoloRaidMuseumLogCharacters

Store both mode log collections in one log table, tagged `LogSet = Current` or `Best`. Current logs can be replaced by team number; best logs are replaced as a set only when the completed run beats the record. Snapshot character fields to preserve historical results if a character later changes.

| Table | Column | Type | Constraints | Source |
|---|---|---|---|---|
| `SoloRaidMuseumLogs` | `Id` | ULID (26-character string) | Primary key | New identifier |
| `SoloRaidMuseumLogs` | `SoloRaidMuseumModeId` | ULID (26-character string) | Not null; foreign key to `SoloRaidMuseumModes.Id` | Parent mode row |
| `SoloRaidMuseumLogs` | `LogSet` | String / enum | Not null; part of unique constraint (`Current` or `Best`) | `CurrentLogs` / `Logs` |
| `SoloRaidMuseumLogs` | `TeamNumber` | Integer | Not null; unique with `SoloRaidMuseumModeId` and `LogSet` | `TeamNumber` |
| `SoloRaidMuseumLogs` | `Sequence` | Integer | Not null; unique with `SoloRaidMuseumModeId` and `LogSet` | Array order in `CurrentLogs` / `Logs` |
| `SoloRaidMuseumLogs` | `Damage` | Integer (64-bit) | Not null | `Damage` |
| `SoloRaidMuseumLogs` | `Step` | Integer | Not null | `Step` |
| `SoloRaidMuseumLogCharacters` | `Id` | ULID (26-character string) | Primary key | New identifier |
| `SoloRaidMuseumLogCharacters` | `SoloRaidMuseumLogId` | ULID (26-character string) | Not null; foreign key to `SoloRaidMuseumLogs.Id` | Parent log row |
| `SoloRaidMuseumLogCharacters` | `Slot` | Integer | Not null; unique with `SoloRaidMuseumLogId` | `Team[*].Slot` |
| `SoloRaidMuseumLogCharacters` | `Csn` | Integer (64-bit) | Not null | `Team[*].Csn` |
| `SoloRaidMuseumLogCharacters` | `Tid` | Integer | Not null | `Team[*].Tid` |
| `SoloRaidMuseumLogCharacters` | `Lv` | Integer | Not null | `Team[*].Lv` |
| `SoloRaidMuseumLogCharacters` | `Combat` | Integer | Not null | `Team[*].Combat` |
| `SoloRaidMuseumLogCharacters` | `CostumeId` | Integer | Not null | `Team[*].CostumeId` |

Enforce unique (`SoloRaidMuseumModeId`, `LogSet`, `TeamNumber`), (`SoloRaidMuseumModeId`, `LogSet`, `Sequence`), and (`SoloRaidMuseumLogId`, `Slot`). Replacing a best record must replace its `Best` log rows and character snapshots in the same transaction as updating best totals.

### Embedded-versus-relational trade-offs

| Collection or object | Keep embedded pros | Keep embedded cons | Recommendation |
|---|---|---|---|
| `SoloRaidData[*].SoloRaidLevels` | Raid state loads and saves as one object; simple mapping to the current model. | Code looks up, replaces, and removes a level using level, type, and open state; a whole-object update can overwrite concurrent changes. | Extract to `SoloRaidLevels` because it has stable query identity and its logs have their own lifecycle. |
| `SoloRaidLevels[*].Logs[*].Team[*]` | JSON naturally preserves order and nested shape; minimal schema surface. | Logs and character snapshots are returned independently and can grow; SQL cannot efficiently filter by run or slot. | Extract logs and snapshot characters into child tables; retain sequence and slot columns. |
| Museum `Challenge` / `NoLimit` objects | One stage object loads both modes without joins. | Each mode has independent counters, run lifecycle, and logs; updates can replace unrelated state for the other mode. | Extract each to `SoloRaidMuseumModes`. |
| Museum mission claim arrays | Compact and easy to check with list membership. | Duplicate claims are possible in storage, with no foreign key or uniqueness guard. | Extract to `SoloRaidMuseumMissionClaims`; unique stage/mode/mission key. |
| Museum saved teams and logs | Preserves protocol-like nested objects directly. | Teams and logs are edited by team number and contain independently meaningful slots and historical snapshots. | Extract to team, log, and slot tables; use snapshot columns for historical characters. |
| `OpenTeams` | Small list, simple to serialize with the mode. | Set behavior (distinct team numbers) and ordering are useful invariants during an active run. | Extract to `SoloRaidMuseumOpenTeams`; unique team per mode and retain sequence. |

The Release sample has one user and shows populated Solo Raid levels and logs, while museum stages are initialized with empty mode/log collections. The runtime still persists whole `User` objects through `JsonDb`; these specifications describe a target relational design. Migration must preserve current and best museum logs separately, their team snapshots and order, and should update counters and replaced log sets transactionally.

## Completed scenarios

The source key is `CompletedScenarios` (the user-owned data list, sometimes referred to as `UserCompletedScenarios`). It contains string identifiers, including values with `_s` / `_e` suffixes and a `"0"` value in the Release sample. Preserve these strings exactly; their meaning is not established well enough to constrain them to a static scenario table. Model the collection as a set because the code checks membership before adding entries.

### UserCompletedScenarios

One row per completed scenario identifier for a user. There is no existing relational table; the current collection is in the user JSON.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID`; unique with `ScenarioId` | `Users[*].ID` |
| `ScenarioId` | String | Not null; unique with `UserId`; preserve exact source text | `Users[*].CompletedScenarios[*]` |

Unique constraint: (`UserId`, `ScenarioId`). This makes duplicate completion entries impossible and supports membership queries. Keep the value as a string, including `"0"`; do not coerce it to an integer or add a foreign key until scenario identifier semantics are confirmed.

## MapJson

`MapJson` is a dictionary from map ID to a string containing client-owned map state. Campaign and event handlers read or replace one entry at a time, and the admin progression command removes selected map entries to force the client to fetch fresh state. The Release sample has two entries; their strings contain JSON with map-wide arrays such as trigger and interaction state. This is opaque payload data, not a server-owned relational model.

### UserMapStates

One row per user and map ID. Keep the inner payload as text rather than decomposing the client JSON. Text is required because a missing map can be initialized to an empty string, which is not valid JSON; the server also returns saved payloads without interpreting their structure.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID`; unique with `MapId` | `Users[*].ID` |
| `MapId` | String | Not null; unique with `UserId` | `Users[*].MapJson` map key |
| `StateJson` | Text | Not null; may be empty; preserve payload exactly | `Users[*].MapJson[MapId]` |

Unique constraint: (`UserId`, `MapId`).

Keeping the whole dictionary embedded in `Users` is simpler and avoids joins, but every map update rewrites the aggregate and a relational store cannot index or upsert one map row independently. Extracting the outer map key into `UserMapStates` enables per-map reads, writes, and deletion while keeping payload internals opaque. The payload remains unqueryable by relational columns; revisit only if the server begins enforcing or querying individual trigger/object state.

## Inventory, characters, and equipment awakenings

Split the owned inventory and character records into user-owned rows. Item and character identifiers (`Isn` and `Csn`) are current domain identifiers; ULIDs are relational primary keys, with per-user unique constraints preserving identity lookups. Static catalog IDs (`ItemType` and `Tid`) are attributes, not keys for owned rows.

### Items

One row per owned item instance. Stackable items may share `ItemType`, so do not merge rows by type. `Csn` remains as the legacy/primary character association in the item model. Extract multi-character `CsnList` membership while preserving its order.

| Column | Type | Constraints | Current system status | Source |
|---|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key; generated per row | New column | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | New column | `Users[*].ID` |
| `Isn` | Integer (64-bit) | Not null; unique with `UserId` | Exists in JSON/model | `Users[*].Items[*].Isn` |
| `ItemType` | Integer | Not null | Exists in JSON/model | `Users[*].Items[*].ItemType` |
| `Csn` | Integer (64-bit) | Not null; default `0`; `0` is the unassigned sentinel | Exists in JSON/model | `Users[*].Items[*].Csn` |
| `Count` | Integer | Not null | Exists in JSON/model | `Users[*].Items[*].Count` |
| `Level` | Integer | Not null | Exists in JSON/model | `Users[*].Items[*].Level` |
| `Exp` | Integer | Not null | Exists in JSON/model | `Users[*].Items[*].Exp` |
| `Position` | Integer | Not null | Exists in JSON/model | `Users[*].Items[*].Position` |
| `Corp` | Integer | Not null | Exists in JSON/model | `Users[*].Items[*].Corp` |

Unique constraint: (`UserId`, `Isn`). Add an index on (`UserId`, `ItemType`) for type-based inventory operations. Preserve `Isn` because requests and related state refer to it.

### ItemCharacterAssignments

One row per character in an item's `CsnList`. `Position` preserves list order, which current code uses when selecting a primary assignment. Keep `Items.Csn` during migration as current code reads both fields and emits a combined view. Removing that compatibility field requires an explicit conversion rule.

| Column | Type | Constraints | Current system status | Source |
|---|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key; generated per row | New column | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | New column | Parent item owner |
| `ItemId` | ULID (26-character string) | Not null; foreign key to `Items.Id`; unique with `Position` | New column | Parent item row |
| `CharacterCsn` | Integer (64-bit) | Not null; composite foreign key (`UserId`, `CharacterCsn`) to `Characters`; unique with `ItemId` | New column | `Users[*].Items[*].CsnList[*]` |
| `Position` | Integer | Not null; unique with `ItemId` | New column | Array order in `CsnList` |

### Characters

One row per owned character instance. A user may own multiple instances of one catalog character, so `Csn` identifies the instance and `Tid` is not unique.

| Column | Type | Constraints | Current system status | Source |
|---|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key; generated per row | New column | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | New column | `Users[*].ID` |
| `Csn` | Integer (64-bit) | Not null; unique with `UserId` | Exists in JSON/model | `Users[*].Characters[*].Csn` |
| `Tid` | Integer | Not null | Exists in JSON/model | `Users[*].Characters[*].Tid` |
| `CostumeId` | Integer | Not null | Exists in JSON/model | `Users[*].Characters[*].CostumeId` |
| `Level` | Integer | Not null | Exists in JSON/model | `Users[*].Characters[*].Level` |
| `UltimateLevel` | Integer | Not null | Exists in JSON/model | `Users[*].Characters[*].UltimateLevel` |
| `Skill1Lvl` | Integer | Not null | Exists in JSON/model | `Users[*].Characters[*].Skill1Lvl` |
| `Skill2Lvl` | Integer | Not null | Exists in JSON/model | `Users[*].Characters[*].Skill2Lvl` |
| `Grade` | Integer | Not null | Exists in JSON/model | `Users[*].Characters[*].Grade` |
| `IsMainForce` | Boolean | Not null | Exists in JSON/model | `Users[*].Characters[*].IsMainForce` |

Unique constraint: (`UserId`, `Csn`). `Tid` refers conceptually to static game data, but should not have a relational foreign key unless that catalog is stored here too.

### EquipmentAwakenings

Awakening state belongs to an item instance and is matched by its `Isn`. The runtime supports pending (`IsNewData`) option state alongside confirmed state for the same item, so multiple rows per item are allowed; do not enforce uniqueness on `ItemId` or `IsNewData`.

| Column | Type | Constraints | Current system status | Source |
|---|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key; generated per row | New column | New identifier |
| `ItemId` | ULID (26-character string) | Not null; foreign key to `Items.Id` | New column | Match awakening `Isn` to `Items.Isn` |
| `IsNewData` | Boolean | Not null | Exists in JSON/model | `Users[*].EquipmentAwakenings[*].IsNewData` |
| `Option1Id` | Integer | Not null | Exists in JSON/model | `Option.Option1Id` |
| `Option1Lock` | Boolean | Not null | Exists in JSON/model | `Option.Option1Lock` |
| `IsOption1DisposableLock` | Boolean | Not null | Exists in JSON/model | `Option.IsOption1DisposableLock` |
| `Option2Id` | Integer | Not null | Exists in JSON/model | `Option.Option2Id` |
| `Option2Lock` | Boolean | Not null | Exists in JSON/model | `Option.Option2Lock` |
| `IsOption2DisposableLock` | Boolean | Not null | Exists in JSON/model | `Option.IsOption2DisposableLock` |
| `Option3Id` | Integer | Not null | Exists in JSON/model | `Option.Option3Id` |
| `Option3Lock` | Boolean | Not null | Exists in JSON/model | `Option.Option3Lock` |
| `IsOption3DisposableLock` | Boolean | Not null | Exists in JSON/model | `Option.IsOption3DisposableLock` |

The protocol has exactly three option slots, read and updated together, so fixed columns avoid a child table and joins. Resolve each awakening's `Isn` to its item row during migration; preserve or reconcile orphan awakenings explicitly rather than dropping them.

## User JSON key inventory

Observed from the Release publish `data/db.json` copy (1 user record). This lists the remaining top-level keys not yet assigned to relational columns or tables. Keys assigned to `UserBans`, `Users`, and `SdkUsers` above are omitted. Object and array contents are not expanded. Keys are grouped by observed JSON shape.

### Top-level object keys

| Key | JSON type |
|---|---|
| `ArcadeInTheMirrorData` | Object |
| `BBQInfoData` | Object |
| `BtgData` | Object |
| `BubbleMarchDatas` | Object |
| `ClearedTutorialDataNew` | Object |
| `CommanderMusic` | Object |
| `ContentsOpenUnlocked` | Object |
| `Currency` | Object |
| `DDRDatas` | Object |
| `DessertRushData` | Object |
| `EventInfo` | Object |
| `EventMissionInfo` | Object |
| `EventShopBuyCountInfo` | Object |
| `FavoriteSongs` | Object |
| `FieldInfo` | Object |
| `FieldInfoNew` | Object |
| `GachaBannerMaxPulls` | Object |
| `GachaPaybackData` | Object |
| `GachaPityBannerExecuteCount` | Object |
| `GachaSelectupChoices` | Object |
| `Guild` | Object |
| `IsLandBreakerDatas` | Object |
| `LiberateDatas` | Object |
| `LobbyMusic` | Object |
| `LoginEventInfo` | Object |
| `LostSectorData` | Object |
| `MailDatas` | Object |
| `MainQuestData` | Object |
| `MiniGameAzxInfo` | Object |
| `MiniGameScenarios` | Object |
| `MiniGameStoryChoice` | Object |
| `Nksv2Datas` | Object |
| `NormalShopStates` | Object |
| `PirateCafeData` | Object |
| `ProfileCardDecoration` | Object |
| `RebuildEdenDatas` | Object |
| `ResetableData` | Object |
| `SoloRaidData` | Object |
| `SoloRaidMuseumData` | Object |
| `SortOutData` | Object |
| `StellarBladeDatas` | Object |
| `SubQuestData` | Object |
| `TTSGameData` | Object |
| `TowerDefenseDatas` | Object |
| `TowerProgress` | Object |
| `UserPassInfo` | Object |
| `UserTeams` | Object |
| `ViewedNoticePopupTableIds` | Object |
| `WeeklyResetableData` | Object |
| `userPointData` | Object |

When designing/migrating `GachaBannerMaxPulls`, preserve the startup compatibility conversion from legacy `GachaTutorialPlayCount`: if this map is empty and the legacy count is positive, derive the tutorial and premium counts as `JsonDb.ValidateDb` currently does. The Release sample count is `0`; do not add a persistent column for the legacy counter.

### Top-level array keys

| Key | JSON type |
|---|---|
| `ArcadePlaySodaInfoList` | Array |
| `BackgroundList` | Array |
| `Badges` | Array |
| `BondInfo` | Array |
| `CharacterWishlist` | Array |
| `ClaimedJukeboxRewardTriggers` | Array |
| `ClearedOutpostScenarioIds` | Array |
| `CompletedAchievements` | Array |
| `CompletedSideStoryStages` | Array |
| `CompletedTacticAcademyLessons` | Array |
| `CostumeList` | Array |
| `FavoriteItemQuests` | Array |
| `FavoriteItems` | Array |
| `FrameList` | Array |
| `GachaDailyFreePulls` | Array |
| `IconList` | Array |
| `JukeboxBgm` | Array |
| `JukeboxThemeList` | Array |
| `LiveWallpaperList` | Array |
| `LobbyDecoBackgroundList` | Array |
| `LobbyPrivateBannerIds` | Array |
| `Memorial` | Array |
| `MessengerData` | Array |
| `MessengerRepairHistory` | Array |
| `OpenLiberateTypeIdList` | Array |
| `PickedMessages` | Array |
| `PlayLists` | Array |
| `PlaySodaInfoData` | Array |
| `ProfileCardsData` | Array |
| `RepresentationTeamDataNew` | Array |
| `StageClearHistorys` | Array |
| `StickerList` | Array |
| `TitleList` | Array |
| `Triggers` | Array |
| `UnlockedArchiveEventQuestIds` | Array |
| `UnlockedArchiveRecordIds` | Array |
| `ViewedSideStoryStages` | Array |
| `WallpaperBackground` | Array |
| `WallpaperFavoriteList` | Array |
| `WallpaperJukeboxList` | Array |
| `WallpaperList` | Array |
| `WallpaperPlaylistList` | Array |
