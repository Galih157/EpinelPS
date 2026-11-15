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

## Lobby-critical user data

The following sections specify relational tables for the 50 `Lobby-critical` user keys read during initial login → `User/Get` → lobby hydration, verified against `../save-data/db.json` and `UserModel.cs` / `DbModels.cs` / protobuf models. All user-owned tables use 26-character ULID `Id` primary keys and `UserId` (`ulong`, 64-bit integer) foreign keys to `Users.ID`.

### Currency and progress unlocks

#### UserCurrencies

One row per `(UserId, CurrencyType)` pair (`Users[*].Currency`).

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | `Users[*].ID` |
| `CurrencyType` | Integer | Not null; unique with `UserId` | `Currency` map key (`CurrencyType` enum) |
| `Amount` | Integer (64-bit) | Not null | `Currency[CurrencyType]` |

Unique constraint: (`UserId`, `CurrencyType`).

#### UserMainQuests

One row per main quest entry (`Users[*].MainQuestData`).

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | `Users[*].ID` |
| `QuestTid` | Integer | Not null; unique with `UserId` | `MainQuestData` map key |
| `ReceivedReward` | Boolean | Not null | `MainQuestData[QuestTid]` |

Unique constraint: (`UserId`, `QuestTid`).

#### UserTowerProgress

One row per corporation tower (`Users[*].TowerProgress`).

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | `Users[*].ID` |
| `CorporationType` | Integer | Not null; unique with `UserId` | `TowerProgress` map key (`CorporationTowerType` enum) |
| `ClearedFloor` | Integer | Not null | `TowerProgress[CorporationType]` |

Unique constraint: (`UserId`, `CorporationType`).

#### UserClearedTutorials

One row per cleared tutorial group (`Users[*].ClearedTutorialDataNew`).

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | `Users[*].ID` |
| `TutorialGroupId` | Integer | Not null; unique with `UserId` | `ClearedTutorialDataNew` map key |
| `TutorialId` | Integer | Not null | `ClearedTutorialData.Id` |
| `VersionGroup` | Integer | Not null | `ClearedTutorialData.VersionGroup` |

Unique constraint: (`UserId`, `TutorialGroupId`).

#### UserContentsUnlocks

One row per unlocked content entry (`Users[*].ContentsOpenUnlocked`).

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | `Users[*].ID` |
| `ContentsOpenId` | Integer | Not null; unique with `UserId` | `ContentsOpenUnlocked` map key |
| `ButtonAnimationPlayed` | Boolean | Not null | `UnlockData.ButtonAnimationPlayed` |
| `PopupAnimationPlayed` | Boolean | Not null | `UnlockData.PopupAnimationPlayed` |

Unique constraint: (`UserId`, `ContentsOpenId`).

#### UserViewedNoticePopups

One row per viewed notice popup (`Users[*].ViewedNoticePopupTableIds`).

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | `Users[*].ID` |
| `CategoryId` | Integer | Not null | `ViewedNoticePopupTableIds` map key |
| `NoticePopupTableId` | Integer | Not null | `ViewedNoticePopupTableIds[CategoryId][*]` |

Unique constraint: (`UserId`, `CategoryId`, `NoticePopupTableId`).

### Daily and weekly reset state

#### UserResetStates

One row per user combining scalar fields from `ResetableData` and `WeeklyResetableData`. `SimRoomData` (secondary mode state) is stored as JSON in `SimRoomStateJson` until Simulation Room is decomposed separately.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; unique; foreign key to `Users.ID` | `Users[*].ID` |
| `WipeoutCount` | Integer | Not null; default `0` | `ResetableData.WipeoutCount` |
| `ClearedSimulationRoom` | Boolean | Not null; default `false` | `ResetableData.ClearedSimulationRoom` |
| `InterceptionTickets` | Integer | Not null; default `3` | `ResetableData.InterceptionTickets` |
| `DailyMissionPoints` | Integer | Not null; default `0` | `ResetableData.DailyMissionPoints` |
| `DispatchCount` | Integer | Not null; default `0` | `ResetableData.DispatchCount` |
| `WeeklyMissionPoints` | Integer | Not null; default `0` | `WeeklyResetableData.WeeklyMissionPoints` |
| `SimRoomStateJson` | Text | Not null | Serialized `ResetableData.SimRoomData` |

#### UserCompletedDailyMissions and UserCompletedWeeklyMissions

One row per completed mission ID in `ResetableData.CompletedDailyMissions` and `WeeklyResetableData.CompletedWeeklyMissions`.

- `UserCompletedDailyMissions`: `Id` (ULID PK), `UserId` (FK), `MissionId` (Integer, not null). Unique: (`UserId`, `MissionId`).
- `UserCompletedWeeklyMissions`: `Id` (ULID PK), `UserId` (FK), `MissionId` (Integer, not null). Unique: (`UserId`, `MissionId`).

#### UserDailyTowerCounts

One row per corporation tower daily count (`ResetableData.TowerCount`).

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | `Users[*].ID` |
| `CorporationType` | Integer | Not null; unique with `UserId` | `TowerCount` map key (`CorporationTowerType`) |
| `UsedCount` | Integer | Not null | `TowerCount[CorporationType]` |

Unique constraint: (`UserId`, `CorporationType`).

#### UserDailyCounselCounts

One row per entry in `ResetableData.DailyCounselCount`.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | `Users[*].ID` |
| `KeyId` | Integer | Not null; unique with `UserId` | `DailyCounselCount` map key |
| `Count` | Integer | Not null | `DailyCounselCount[KeyId]` |

Unique constraint: (`UserId`, `KeyId`).

### Teams and representation

#### UserTeamGroups

One row per team type (`Users[*].UserTeams`).

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | `Users[*].ID` |
| `TeamType` | Integer | Not null; unique with `UserId` | `UserTeams` map key / `NetUserTeamData.Type` |
| `LastContentsTeamNumber` | Integer | Not null | `NetUserTeamData.LastContentsTeamNumber` |

Unique constraint: (`UserId`, `TeamType`).

#### UserTeamSlots

One row per slot in `NetUserTeamData.Teams[*].Slots[*]`.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | `Users[*].ID` |
| `TeamGroupId` | ULID (26-character string) | Not null; foreign key to `UserTeamGroups.Id` | Parent team group |
| `TeamNumber` | Integer | Not null | `NetTeamData.TeamNumber` |
| `Slot` | Integer | Not null | `NetTeamSlot.Slot` |
| `Value` | Integer (64-bit) | Not null | `NetTeamSlot.Value` |
| `ValueType` | Integer | Not null | `NetTeamSlot.ValueType` |

Unique constraint: (`TeamGroupId`, `TeamNumber`, `Slot`).

#### UserRepresentationTeamSlots

One row per slot in `Users[*].RepresentationTeamDataNew` (ordered array of character `Csn`s).

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | `Users[*].ID` |
| `Slot` | Integer | Not null; unique with `UserId` | Array index (0-based) |
| `CharacterCsn` | Integer (64-bit) | Not null | `RepresentationTeamDataNew[Slot]` |

Unique constraint: (`UserId`, `Slot`).

### Profile, cosmetics, wallpapers, and lobby music

#### UserProfileCardDecorations and UserProfileCardStickers

One 1:1 row per user for `ProfileCardDecoration` and child rows for `StickerPlacements`.

- `UserProfileCardDecorations`: `Id` (ULID PK), `UserId` (unique FK), `ShowCharacterSpine` (Boolean, not null), `BackgroundId` (Integer, not null).
- `UserProfileCardStickers`: `Id` (ULID PK), `UserId` (FK), `Position` (Integer, not null; array index), `StickerId` (Integer, not null), `Layer` (Integer, not null), `NormalizedX` (Float/Real, not null), `NormalizedY` (Float/Real, not null), `RotationRadian` (Float/Real, not null), `Scale` (Float/Real, not null). Unique: (`UserId`, `Position`).

#### UserUnlockedCosmetics

Unified unlock set for `CostumeList`, `FrameList`, `TitleList`, `ProfileCardsData`, and `Memorial`. Avoids five identical `(UserId, ItemId)` tables; can also absorb `Standard` cosmetic lists (`BackgroundList`, `IconList`, `StickerList`, `JukeboxThemeList`, `LiveWallpaperList`, `LobbyDecoBackgroundList`) later without schema changes.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | `Users[*].ID` |
| `Category` | String | Not null | `"Costume"`, `"Frame"`, `"Title"`, `"ProfileCard"`, `"Memorial"` |
| `CosmeticId` | Integer | Not null | Element value in the corresponding list |

Unique constraint: (`UserId`, `Category`, `CosmeticId`).

#### UserJukeboxSettings

One row per `(UserId, Location)` covering `LobbyMusic` (`Location = 1`) and `CommanderMusic` (`Location = 0`).

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | `Users[*].ID` |
| `Location` | Integer | Not null; unique with `UserId` | `JukeBoxSetting.Location` (`NetJukeboxLocation`) |
| `BgmType` | Integer | Not null | `JukeBoxSetting.Type` (`NetJukeboxBgmType`) |
| `TableId` | Integer | Not null | `JukeBoxSetting.TableId` |
| `IsShuffle` | Boolean | Not null | `JukeBoxSetting.IsShuffle` |

Unique constraint: (`UserId`, `Location`).

#### UserClaimedJukeboxRewards

One row per claimed trigger ID in `Users[*].ClaimedJukeboxRewardTriggers`.

| Column | Type | Constraints | Source |
|---|---|---|---|
| `Id` | ULID (26-character string) | Primary key | New identifier |
| `UserId` | Integer (64-bit) | Not null; foreign key to `Users.ID` | `Users[*].ID` |
| `TriggerId` | Integer | Not null; unique with `UserId` | `ClaimedJukeboxRewardTriggers[*]` |

Unique constraint: (`UserId`, `TriggerId`).

#### Wallpaper configuration tables

Each wallpaper array is keyed by `(UserId, OrderIndex)` where `OrderIndex` is the proto `Order` field.

- **`UserWallpaperSlots`** (`WallpaperList`): `Id` (ULID PK), `UserId` (FK), `OrderIndex` (Integer), `Type` (Integer, `LiveWallpaperType`), `WallpaperId` (Integer, `Id`), `UseCharacterBackground` (Boolean). Unique: (`UserId`, `OrderIndex`).
- **`UserWallpaperBackgrounds`** (`WallpaperBackground`): `Id` (ULID PK), `UserId` (FK), `OrderIndex` (Integer), `LobbyDecoBackgroundId` (Integer). Unique: (`UserId`, `OrderIndex`).
- **`UserWallpaperJukeboxes`** (`WallpaperJukeboxList`): `Id` (ULID PK), `UserId` (FK), `OrderIndex` (Integer), `JukeboxListTableId` (Integer). Unique: (`UserId`, `OrderIndex`).
- **`UserWallpaperFavorites`** (`WallpaperFavoriteList`): `Id` (ULID PK), `UserId` (FK), `OrderIndex` (Integer), `IsShuffled` (Boolean). Unique: (`UserId`, `OrderIndex`).
- **`UserWallpaperPlaylists`** (`WallpaperPlaylistList`): `Id` (ULID PK), `UserId` (FK), `OrderIndex` (Integer), `PlaylistUId` (Integer 64-bit), `IsShuffled` (Boolean). Unique: (`UserId`, `OrderIndex`).

### Gacha, normal shop, and passes

#### Gacha per-banner and free-pull tables

- **`UserGachaBannerPulls`** (`GachaBannerMaxPulls`): `Id` (ULID PK), `UserId` (FK), `BannerId` (Integer), `MaxPulls` (Integer). Unique: (`UserId`, `BannerId`). Preserve the startup compatibility conversion from legacy `GachaTutorialPlayCount` during migration.
- **`UserGachaPityCounts`** (`GachaPityBannerExecuteCount`): `Id` (ULID PK), `UserId` (FK), `BannerId` (Integer), `ExecuteCount` (Integer). Unique: (`UserId`, `BannerId`).
- **`UserGachaSelectupChoices`** (`GachaSelectupChoices`): `Id` (ULID PK), `UserId` (FK), `BannerId` (Integer), `CharacterTid` (Integer). Unique: (`UserId`, `BannerId`).
- **`UserGachaDailyFreePulls`** (`GachaDailyFreePulls`): `Id` (ULID PK), `UserId` (FK), `GachaId` (Integer). Unique: (`UserId`, `GachaId`).

#### UserGachaPaybacks and UserGachaPaybackRewards

- **`UserGachaPaybacks`** (`GachaPaybackData`): `Id` (ULID PK), `UserId` (FK), `GachaId` (Integer), `GachaCount` (Integer). Unique: (`UserId`, `GachaId`).
- **`UserGachaPaybackRewards`** (`GachaPaybackData.RewardedStepList`): `Id` (ULID PK), `UserId` (FK), `PaybackId` (ULID FK to `UserGachaPaybacks.Id`), `StepId` (Integer). Unique: (`PaybackId`, `StepId`).

#### UserNormalShopStates, UserNormalShopOrders, and UserNormalShopPinnedBuys

Normalizes `Users[*].NormalShopStates` (keyed by `ShopCategory`).

- **`UserNormalShopStates`**: `Id` (ULID PK), `UserId` (FK), `ShopCategory` (Integer), `RenewCount` (Integer), `RenewAt` (Integer 64-bit ticks), `NextRenewAt` (Integer 64-bit ticks). Unique: (`UserId`, `ShopCategory`).
- **`UserNormalShopOrders`**: `Id` (ULID PK), `UserId` (FK), `ShopStateId` (ULID FK), `OrderIndex` (Integer), `ProductId` (Integer, from `ProductIdsByOrder`), `BuyCount` (Integer, from `BuyCountsByOrder`, default `0`). Unique: (`ShopStateId`, `OrderIndex`).
- **`UserNormalShopPinnedBuys`**: `Id` (ULID PK), `UserId` (FK), `ShopStateId` (ULID FK), `ProductId` (Integer, key of `PinnedBuyCounts`), `BuyCount` (Integer). Unique: (`ShopStateId`, `ProductId`).

#### UserPasses, UserPassRanks, and UserPassMissions

Normalizes `Users[*].UserPassInfo` (keyed by `PassId`).

- **`UserPasses`**: `Id` (ULID PK), `UserId` (FK), `PassId` (Integer), `PassPoint` (Integer), `PassSkipCount` (Integer), `PremiumActive` (Boolean), `LastCompleteAt` (String, not null, default `""`). Unique: (`UserId`, `PassId`).
- **`UserPassRanks`**: `Id` (ULID PK), `UserId` (FK), `UserPassId` (ULID FK), `PassRank` (Integer), `IsNormalRewarded` (Boolean), `IsPremiumRewarded` (Boolean). Unique: (`UserPassId`, `PassRank`).
- **`UserPassMissions`**: `Id` (ULID PK), `UserId` (FK), `UserPassId` (ULID FK), `PassMissionId` (Integer), `IsComplete` (Boolean). Unique: (`UserPassId`, `PassMissionId`).

### Events and login events

- **`UserEvents`** (`EventInfo`): `Id` (ULID PK), `UserId` (FK), `EventId` (Integer), `Diff` (Integer), `LastStage` (Integer), `LastDay` (Integer), `FreeTicket` (Integer). Unique: (`UserId`, `EventId`).
- **`UserEventCompletedScenarios`** (`EventInfo[*].CompletedScenarios`): `Id` (ULID PK), `UserId` (FK), `UserEventId` (ULID FK), `ScenarioId` (String). Unique: (`UserEventId`, `ScenarioId`).
- **`UserEventClearedStages`** (`EventInfo[*].ClearedStages`): `Id` (ULID PK), `UserId` (FK), `UserEventId` (ULID FK), `StageId` (Integer). Unique: (`UserEventId`, `StageId`).
- **`UserLoginEvents`** (`LoginEventInfo`): `Id` (ULID PK), `UserId` (FK), `EventId` (Integer), `LastDay` (Integer), `LastDate` (Integer 64-bit). Unique: (`UserId`, `EventId`).
- **`UserLoginEventDays`** (`LoginEventInfo[*].Days`): `Id` (ULID PK), `UserId` (FK), `UserLoginEventId` (ULID FK), `Day` (Integer). Unique: (`UserLoginEventId`, `Day`).
- **`UserEventMissions`** (`EventMissionInfo`): `Id` (ULID PK), `UserId` (FK), `EventId` (Integer), `LastDay` (Integer), `LastDate` (Integer 64-bit), `AllClear` (Boolean). Unique: (`UserId`, `EventId`).
- **`UserEventMissionEntries`** (`EventMissionInfo[*].MissionIdList` / `DailyMissionIdList`): `Id` (ULID PK), `UserId` (FK), `UserEventMissionId` (ULID FK), `IsDaily` (Boolean), `MissionId` (Integer). Unique: (`UserEventMissionId`, `IsDaily`, `MissionId`).
- **`UserEventShopBuys`** (`EventShopBuyCountInfo`): `Id` (ULID PK), `UserId` (FK), `EventId` (Integer), `ProductTid` (Integer), `BuyCount` (Integer). Unique: (`UserId`, `EventId`, `ProductTid`).

### Bonds, favorite items, and liberation

- **`UserBonds`** (`BondInfo`): `Id` (ULID PK), `UserId` (FK), `NameCode` (Integer), `Lv` (Integer), `Exp` (Integer), `CanCounselToday` (Boolean), `IsFavorites` (Boolean), `CounseledCount` (Integer), `CompleteRewardStatus` (Integer). Unique: (`UserId`, `NameCode`).
- **`UserBondProgressItems`** (`BondInfo[*].FlushableWatchedDialogIds`, `ObtainedRewardLevels`, `CounselDialogCompleteIds`): `Id` (ULID PK), `UserId` (FK), `BondId` (ULID FK to `UserBonds.Id`), `Kind` (String: `"FlushableWatchedDialog"`, `"ObtainedRewardLevel"`, `"CounselDialogComplete"`), `ItemId` (Integer). Unique: (`BondId`, `Kind`, `ItemId`).
- **`UserFavoriteItems`** (`FavoriteItems`): `Id` (ULID PK), `UserId` (FK), `FavoriteItemId` (Integer 64-bit), `Tid` (Integer), `Csn` (Integer 64-bit), `Lv` (Integer), `Exp` (Integer). Unique: (`UserId`, `FavoriteItemId`).
- **`UserFavoriteItemQuests`** (`FavoriteItemQuests`): `Id` (ULID PK), `UserId` (FK), `QuestId` (Integer), `Clear` (Boolean), `Received` (Boolean). Unique: (`UserId`, `QuestId`).
- **`UserLiberations`** (`LiberateDatas`): `Id` (ULID PK), `UserId` (FK), `CharacterId` (Integer), `Step` (Integer), `ProgressPoint` (Integer), `RewardedCount` (Integer), `IsCompleted` (Boolean). Unique: (`UserId`, `CharacterId`).
- **`UserLiberationMissions`** (`LiberateDatas[*].MissionData`): `Id` (ULID PK), `UserId` (FK), `LiberationId` (ULID FK), `MissionId` (Integer 64-bit), `MissionTid` (Integer), `LiberateCharacterId` (Integer), `MissionState` (Integer), `CreatedAt` (Integer 64-bit), `TriggerStartAt` (Integer 64-bit), `TriggerEndAt` (Integer 64-bit), `ReceivedAt` (Integer 64-bit). Unique: (`LiberationId`, `MissionId`).
- **`UserOpenLiberateTypes`** (`OpenLiberateTypeIdList`): `Id` (ULID PK), `UserId` (FK), `LiberateTypeId` (Integer). Unique: (`UserId`, `LiberateTypeId`).

### Messenger, badges, and triggers

- **`UserMessages`** (`MessengerData`): `Id` (ULID PK), `UserId` (FK), `Seq` (Integer 64-bit), `ConversationId` (String), `MessageId` (String), `CreatedAt` (Integer 64-bit ticks), `State` (Integer). Unique: (`UserId`, `Seq`).
- **`UserPickedMessages`** (`PickedMessages`): `Id` (ULID PK), `UserId` (FK), `ConversationId` (String), `CreatedAtSeconds` (Integer 64-bit), `CreatedAtNanos` (Integer). Unique: (`UserId`, `ConversationId`).
- **`UserBadges`** (`Badges`): `Id` (ULID PK), `UserId` (FK), `Seq` (Integer 64-bit), `Location` (String), `BadgeContent` (Integer, `BadgeContents` enum), `BadgeGuid` (String). Unique: (`UserId`, `Seq`).
- **`Triggers`** (existing SQLite table `TriggerModelNew`): `Id` (Integer 64-bit PK), `UserId` (FK), `Type` (Integer, `Trigger` enum), `CreatedAt` (Integer 64-bit ticks), `ConditionId` (Integer), `Value` (Integer). Most handlers already read/write `GameContext.Triggers`; `TowerHelper.cs` still reads the `[Obsolete]` `user.Triggers` JSON list and must be cut over to `GameContext.Triggers` before removing `Triggers` from `db.json`.

### Achievements, side story, archive, and Lost Sector

- **`UserCompletedAchievements`** (`CompletedAchievements`): `Id` (ULID PK), `UserId` (FK), `AchievementId` (Integer). Unique: (`UserId`, `AchievementId`).
- **`UserSideStoryStages`** (`CompletedSideStoryStages` + `ViewedSideStoryStages`): `Id` (ULID PK), `UserId` (FK), `StageId` (Integer), `IsCompleted` (Boolean), `IsViewed` (Boolean). Unique: (`UserId`, `StageId`).
- **`UserArchiveUnlocks`** (`UnlockedArchiveRecordIds` + `UnlockedArchiveEventQuestIds`): `Id` (ULID PK), `UserId` (FK), `Kind` (String: `"Record"`, `"EventQuest"`), `TargetId` (Integer). Unique: (`UserId`, `Kind`, `TargetId`).
- **`UserLostSectors`** (`LostSectorData`): `Id` (ULID PK), `UserId` (FK), `SectorId` (Integer), `IsOpen` (Boolean), `IsPlaying` (Boolean), `StateJson` (Text, `LostSectorData.Json`), `TeamPositionsJson` (Text, serialized `LostSectorData.TeamPositions`), `ObtainedRewards` (Integer), `ReceivedFinalReward` (Boolean), `CompletedPerfectly` (Boolean). Unique: (`UserId`, `SectorId`).
- **`UserLostSectorClearedStages`** (`LostSectorData[*].ClearedStages`): `Id` (ULID PK), `UserId` (FK), `LostSectorId` (ULID FK), `StageKey` (String), `ClearState` (Integer). Unique: (`LostSectorId`, `StageKey`).
- **`UserLostSectorObjects`** (`LostSectorData[*].Objects`): `Id` (ULID PK), `UserId` (FK), `LostSectorId` (ULID FK), `ObjectKey` (String), `ActionAt` (Integer 64-bit), `Type` (Integer), `PositionId` (String), `Json` (Text), `TeamPositionsJson` (Text). Unique: (`LostSectorId`, `ObjectKey`).

### Minigames and leaderboards

- **`UserMinigames`** (`ArcadeInTheMirrorData`, `BBQInfoData`, `BtgData`, `BubbleMarchDatas`, `DDRDatas`, `DessertRushData`, `IsLandBreakerDatas`, `MiniGameAzxInfo`, `MiniGameScenarios`, `MiniGameStoryChoice`, `Nksv2Datas`, `PirateCafeData`, `RebuildEdenDatas`, `SortOutData`, `StellarBladeDatas`, `TTSGameData`, `TowerDefenseDatas`, `ArcadePlaySodaInfoList`, `PlaySodaInfoData`): `Id` (ULID PK), `UserId` (FK to `Users.ID`), `MinigameType` (String), `ArcadeId` (Integer, `0` for single-instance minigames or dictionary key for keyed minigames), `DataJson` (Text, serialized using `JsonDb`-compatible settings for `Timestamp`/`Duration`/protobuf types), `UpdatedAt` (Integer 64-bit). Unique: (`UserId`, `MinigameType`, `ArcadeId`).
- **`MiniGameLeaderboards`** (`RankDatas.ArcadeScore` and non-TTS minigame guild/server rankings): `Id` (ULID PK), `MinigameType` (String, e.g., `"Arcade"`, `"Nksv2"`, `"StellarBlade"`, `"TowerDefense"`, `"PlaySoda"`), `ArcadeId` (Integer, `0` if unused), `ModeId` (Integer, `0` if unused), `UserId` (Integer 64-bit, FK to `Users.ID`), `GuildId` (Integer 64-bit, `0` when not guild-scoped), `Score` (Integer 64-bit), `UpdatedAt` (Integer 64-bit). Indexes on (`MinigameType`, `ArcadeId`, `ModeId`, `Score`) and (`MinigameType`, `GuildId`, `ArcadeId`, `Score`). TTS leaderboards remain in `TtsSongScores` and `TtsTotalScores`.

### Remaining user data (field maps, mail, jukebox, wishlist, and misc)

- **`Users` profile/guild extension** (`userPointData`, `Guild`): Add `UserLevel` (Integer, default `1`), `ExperiencePoint` (Integer, default `0`), `GuildId` (Integer, nullable), and `GuildLeaveAt` (Integer 64-bit, nullable) directly to `Users`.
- **`UserFieldMaps`** (`FieldInfoNew`; legacy `FieldInfo` is backwards-compatibility only and dropped from migration): `Id` (ULID PK), `UserId` (FK to `Users.ID`), `MapId` (String), `DataJson` (Text, serialized `FieldInfoNew`), `UpdatedAt` (Integer 64-bit). Unique: (`UserId`, `MapId`).
- **`UserSubQuests`** (`SubQuestData`): `Id` (ULID PK), `UserId` (FK), `SubQuestId` (Integer), `ReceivedReward` (Boolean). Unique: (`UserId`, `SubQuestId`).
- **`UserStageClearHistory`** (`StageClearHistorys`): `Id` (ULID PK), `UserId` (FK), `TeamCombat` (Integer), `ClearedAt` (Integer 64-bit), `UserJson` (Text, serialized `NetWholeUserData`), `SlotsJson` (Text, serialized `NetStageClearInfoTeam[]`). Index on (`UserId`, `ClearedAt`).
- **`UserClearedOutpostScenarios`** (`ClearedOutpostScenarioIds`): `Id` (ULID PK), `UserId` (FK), `ScenarioId` (Integer). Unique: (`UserId`, `ScenarioId`).
- **`UserTacticAcademyLessons`** (`CompletedTacticAcademyLessons`): `Id` (ULID PK), `UserId` (FK), `LessonId` (Integer). Unique: (`UserId`, `LessonId`).
- **`UserUnlockedCosmetics` extension** (`BackgroundList`, `IconList`, `StickerList`, `LiveWallpaperList`, `LobbyDecoBackgroundList`, `JukeboxBgm`, `JukeboxThemeList`): Reuse existing `UserUnlockedCosmetics` table with `Kind` values `"Background"`, `"Icon"`, `"Sticker"`, `"LiveWallpaper"`, `"LobbyDecoBackground"`, `"JukeboxBgm"`, and `"JukeboxTheme"`.
- **`UserLobbyPrivateBanners`** (`LobbyPrivateBannerIds`): `Id` (ULID PK), `UserId` (FK), `BannerId` (Integer). Unique: (`UserId`, `BannerId`).
- **`UserJukeboxPlaylists`** (`PlayLists` + `FavoriteSongs`): `Id` (ULID PK), `UserId` (FK), `PlaylistUid` (Integer 64-bit, `0` for `FavoriteSongs`), `IsFavorite` (Boolean), `Title` (String, default `""`), `SongsJson` (Text, serialized `NetJukeboxPlaylistSong[]`). Unique: (`UserId`, `IsFavorite`, `PlaylistUid`).
- **`UserCharacterWishlists`** (`CharacterWishlist`): `Id` (ULID PK), `UserId` (FK), `BannerId` (Integer), `CharacterId` (Integer). Unique: (`UserId`, `BannerId`, `CharacterId`).
- **`UserMails`** (`MailDatas`): `Id` (ULID PK), `UserId` (FK), `Msn` (Integer 64-bit), `Sender` (Integer), `Type` (Integer), `State` (Integer), `TitleJson` (Text, serialized `LocaleableText`), `TextJson` (Text, serialized `LocaleableText`), `CreatedAt` (Integer 64-bit), `Nickname` (String), `AppNoticeId` (String), `HasReward` (Boolean), `ItemsJson` (Text, serialized `NetMailRewardItem[]`), `Period` (Integer). Unique: (`UserId`, `Msn`).
- **`UserMessengerRepairHistory`** (`MessengerRepairHistory`): `Id` (ULID PK), `UserId` (FK), `AuditId` (String), `CreatedAtUtc` (DateTime / Integer 64-bit ticks), `Action` (String), `Note` (String), `MessageJson` (Text, serialized `NetMessage`). Unique: (`UserId`, `AuditId`).

## User JSON key inventory

All top-level `db.json` user keys have now been assigned to target relational columns or tables (with legacy `FieldInfo` intentionally dropped as unused backwards-compatibility data). No unplanned top-level keys remain.
