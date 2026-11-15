# Database migration tracker

Tracks the move from the Release `data/db.json` user state to relational tables in SQLite. The table designs are in [database-design.md](database-design.md). This tracker describes the current state; it does not implement or run a data migration.

Last reconciled: 2026-11-15.

## Status definitions

| Status | Meaning |
|---|---|
| Planned | A target table is designed, but the full table and its SQLite read/write path are not implemented. |
| Unplanned | A JSON key or field has no agreed target design yet. |
| Implemented | The target SQLite table exists, application reads and writes are routed to it, migrated data is verified, and its old `db.json` key is absent or empty. |

An existing SQLite table or a partial set of columns does not by itself count as Implemented. Do not delete JSON data before the SQLite copy and routed behavior have been verified.

## Current implementation snapshot

The application still loads complete game `User` records from `JsonDb`, and many handlers mutate those objects and call `JsonDb.Save()`. SQLite currently contains `Users`, `SdkUsers`, and `Triggers`. Account login uses `SdkUsers`; the `Users` table stores only a subset of the designed profile; trigger operations use SQLite, while the legacy `Triggers` value remains in the user JSON. These are partial or overlapping paths, not complete migrations under the definition above.

No planned user-state table is currently marked Implemented. In particular, do not treat the presence of `Users` or `SdkUsers` as proof that the corresponding user JSON fields have been removed or that all application reads and writes use SQLite.

## Planned tables

Each entry has a corresponding specification in `database-design.md`. All entries below are currently **Planned**.

| Status | Table(s) | Source data |
|---|---|---|
| Planned | `ServerSettings`, `ServerSecrets`, `EventBanners` | Server-owned values from `CoreInfo` |
| Planned | `TtsSongScores`, `TtsTotalScores`, `ArcadeScores` | `RankDatas` |
| Planned | `UserBans` | User ban fields |
| Planned | `Users` profile and gameplay-state extension | Profile, progress, `Last*`, and selected scalar gameplay fields; base SQLite table exists but target columns and ownership are incomplete |
| Planned | `SdkUsers` account-field cutover | Account data already stored in SQLite, but the legacy JSON copies and some registration/authentication paths remain; credential migration depends on a single verification format |
| Planned | `Outposts`, `DispatchClearItems`, `SelectableDispatches`, `Dispatches`, `InfraCoreRewards`, `SynchroSlots`, `ResearchProgress`, `OutpostBuildings` | Dispatch, Infra, and Synchro state |
| Planned | `SoloRaids`, `SoloRaidLevels`, `SoloRaidLogs`, `SoloRaidLogCharacters` | `SoloRaidData` |
| Planned | `SoloRaidMuseumStages`, `SoloRaidMuseumModes`, `SoloRaidMuseumMissionClaims`, `SoloRaidMuseumTeams`, `SoloRaidMuseumTeamSlots`, `SoloRaidMuseumOpenTeams`, `SoloRaidMuseumLogs`, `SoloRaidMuseumLogCharacters` | `SoloRaidMuseumData` |
| Planned | `UserCompletedScenarios` | `CompletedScenarios` |
| Planned | `UserMapStates` | `MapJson` |
| Planned | `Items`, `ItemCharacterAssignments`, `Characters`, `EquipmentAwakenings` | Corresponding owned inventory and character state |

`SdkUsers` and `Triggers` already exist in SQLite. Their overlapping JSON state and any remaining compatibility behavior must be tracked to full cutover before considering those fields Implemented.

## Remaining JSON keys

These are the keys still in the JSON inventory without target designs. They remain Unplanned until assigned to a designed table. The shapes are from the Release publish sample; confirm against the actual database before migration because the sample contained one user.

### Top-level objects

| Status | Key | JSON type |
|---|---|---|
| Unplanned | `ArcadeInTheMirrorData` | Object |
| Unplanned | `BBQInfoData` | Object |
| Unplanned | `BtgData` | Object |
| Unplanned | `BubbleMarchDatas` | Object |
| Unplanned | `ClearedTutorialDataNew` | Object |
| Unplanned | `CommanderMusic` | Object |
| Unplanned | `ContentsOpenUnlocked` | Object |
| Unplanned | `Currency` | Object |
| Unplanned | `DDRDatas` | Object |
| Unplanned | `DessertRushData` | Object |
| Unplanned | `EventInfo` | Object |
| Unplanned | `EventMissionInfo` | Object |
| Unplanned | `EventShopBuyCountInfo` | Object |
| Unplanned | `FavoriteSongs` | Object |
| Unplanned | `FieldInfo` | Object |
| Unplanned | `FieldInfoNew` | Object |
| Unplanned | `GachaBannerMaxPulls` | Object |
| Unplanned | `GachaPaybackData` | Object |
| Unplanned | `GachaPityBannerExecuteCount` | Object |
| Unplanned | `GachaSelectupChoices` | Object |
| Unplanned | `Guild` | Object |
| Unplanned | `IsLandBreakerDatas` | Object |
| Unplanned | `LiberateDatas` | Object |
| Unplanned | `LobbyMusic` | Object |
| Unplanned | `LoginEventInfo` | Object |
| Unplanned | `LostSectorData` | Object |
| Unplanned | `MailDatas` | Object |
| Unplanned | `MainQuestData` | Object |
| Unplanned | `MiniGameAzxInfo` | Object |
| Unplanned | `MiniGameScenarios` | Object |
| Unplanned | `MiniGameStoryChoice` | Object |
| Unplanned | `Nksv2Datas` | Object |
| Unplanned | `NormalShopStates` | Object |
| Unplanned | `PirateCafeData` | Object |
| Unplanned | `ProfileCardDecoration` | Object |
| Unplanned | `RebuildEdenDatas` | Object |
| Unplanned | `ResetableData` | Object |
| Unplanned | `SoloRaidData` | Object |
| Unplanned | `SoloRaidMuseumData` | Object |
| Unplanned | `SortOutData` | Object |
| Unplanned | `StellarBladeDatas` | Object |
| Unplanned | `SubQuestData` | Object |
| Unplanned | `TTSGameData` | Object |
| Unplanned | `TowerDefenseDatas` | Object |
| Unplanned | `TowerProgress` | Object |
| Unplanned | `UserPassInfo` | Object |
| Unplanned | `UserTeams` | Object |
| Unplanned | `ViewedNoticePopupTableIds` | Object |
| Unplanned | `WeeklyResetableData` | Object |
| Unplanned | `userPointData` | Object |

The legacy `GachaTutorialPlayCount` scalar is a migration input for `GachaBannerMaxPulls`, not a standalone table or remaining field-design item. Preserve its startup conversion when that object is designed and migrated; see [database-design.md](database-design.md#user-json-key-inventory).

### Top-level arrays

| Status | Key | JSON type |
|---|---|---|
| Unplanned | `ArcadePlaySodaInfoList` | Array |
| Unplanned | `BackgroundList` | Array |
| Unplanned | `Badges` | Array |
| Unplanned | `BondInfo` | Array |
| Unplanned | `CharacterWishlist` | Array |
| Unplanned | `ClaimedJukeboxRewardTriggers` | Array |
| Unplanned | `ClearedOutpostScenarioIds` | Array |
| Unplanned | `CompletedAchievements` | Array |
| Unplanned | `CompletedSideStoryStages` | Array |
| Unplanned | `CompletedTacticAcademyLessons` | Array |
| Unplanned | `CostumeList` | Array |
| Unplanned | `FavoriteItemQuests` | Array |
| Unplanned | `FavoriteItems` | Array |
| Unplanned | `FrameList` | Array |
| Unplanned | `GachaDailyFreePulls` | Array |
| Unplanned | `IconList` | Array |
| Unplanned | `JukeboxBgm` | Array |
| Unplanned | `JukeboxThemeList` | Array |
| Unplanned | `LiveWallpaperList` | Array |
| Unplanned | `LobbyDecoBackgroundList` | Array |
| Unplanned | `LobbyPrivateBannerIds` | Array |
| Unplanned | `Memorial` | Array |
| Unplanned | `MessengerData` | Array |
| Unplanned | `MessengerRepairHistory` | Array |
| Unplanned | `OpenLiberateTypeIdList` | Array |
| Unplanned | `PickedMessages` | Array |
| Unplanned | `PlayLists` | Array |
| Unplanned | `PlaySodaInfoData` | Array |
| Unplanned | `ProfileCardsData` | Array |
| Unplanned | `RepresentationTeamDataNew` | Array |
| Unplanned | `StageClearHistorys` | Array |
| Unplanned | `StickerList` | Array |
| Unplanned | `TitleList` | Array |
| Unplanned | `Triggers` | Array |
| Unplanned | `UnlockedArchiveEventQuestIds` | Array |
| Unplanned | `UnlockedArchiveRecordIds` | Array |
| Unplanned | `ViewedSideStoryStages` | Array |
| Unplanned | `WallpaperBackground` | Array |
| Unplanned | `WallpaperFavoriteList` | Array |
| Unplanned | `WallpaperJukeboxList` | Array |
| Unplanned | `WallpaperList` | Array |
| Unplanned | `WallpaperPlaylistList` | Array |

`Triggers` has a SQLite table used by current trigger operations, but it remains Unplanned as a JSON field until the legacy array's contents and compatibility role are reconciled and the JSON value is empty or removed.

## Migration path and rollback

Migrate one coherent table group at a time. The source remains `db.json` until the new table and application path are ready.

1. **Plan:** move a key from Unplanned to Planned only after its shape, read/write behavior, invariants, and target table are documented.
2. **Expand:** add the SQLite schema and persistence path without deleting the JSON source. Avoid two independent writable sources of truth. Do not cut over while an older application instance can still write that group's JSON state; drain it or keep the group on its old path until all writers are compatible.
3. **Copy:** back up the source file, then run an idempotent import keyed by stable existing identifiers. For generated ULIDs, persist the source-to-target ID mapping or upsert by a documented natural key. Record per-group progress, row counts, and failures so a partial import can be retried safely.
4. **Verify:** compare source and SQLite values, including child counts, null/sentinel handling, ordering, and relationships. Do not cut over when any source row is unaccounted for.
5. **Cut over:** route all reads and writes for the group to SQLite. Verify the running path, then remove or empty only the migrated JSON key and mark the group Implemented.

Before SQLite accepts writes after cutover, rollback can restore the backed-up JSON source and old routing. After SQLite has accepted new writes, rollback requires exporting those writes back into the JSON shape and verifying them before routing back; otherwise data would be lost. Keep the backup until that rollback window is deliberately closed. No automatic `db.json` contraction or migration utility exists yet.

## Progress log

| Date | Change | Status |
|---|---|---|
| 2026-11-15 | Created tracker from current design document, SQLite models/migrations, runtime ownership, and Release JSON inventory. | Current snapshot recorded; no data migration performed. |
