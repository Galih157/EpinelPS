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

Each entry has a corresponding specification in `database-design.md`. All entries below are currently **Planned** (runtime-cutover items remain **Planned** until `EpinelPS.DBMigration` and `db.json` contraction are finished).

Priority order reflects the live login-to-lobby load sequence:
1. **Runtime-cutover complete** — already routed to SQLite at runtime; migration/contraction pending.
2. **Lobby-critical** — queried during initial login → `User/Get` → lobby hydration.
3. **Secondary** — mode-specific or post-lobby features.

| Status | Priority | Table(s) | Source data |
|---|---|---|---|
| Planned | Runtime-cutover complete | `ServerSettings`, `ServerSecrets`, `EventBanners` | Server-owned values from `CoreInfo`; SQLite tables, EF migration, and runtime `ServerConfigStore` cutover implemented; `EpinelPS.DBMigration` contraction pending |
| Planned | Runtime-cutover complete | `UserBans` | User ban fields; SQLite table, EF migration, and runtime `UserDbHelper` cutover implemented; `EpinelPS.DBMigration` contraction pending |
| Planned | Runtime-cutover complete | `Users` profile and gameplay-state extension | Profile, progress, `Last*`, and selected scalar gameplay fields; SQLite columns, EF migration, and runtime cutover implemented; `EpinelPS.DBMigration` contraction pending |
| Planned | Runtime-cutover complete | `SdkUsers` account-field cutover | Account data stored in SQLite, runtime registration/CLI/login paths unified on lowercase MD5; `EpinelPS.DBMigration` contraction pending |
| Planned | Lobby-critical | `Items`, `ItemCharacterAssignments`, `Characters`, `EquipmentAwakenings` | Corresponding owned inventory and character state (`inventory/get`, `character/get`, `character/costume/get`) |
| Planned | Lobby-critical | `Outposts`, `DispatchClearItems`, `SelectableDispatches`, `Dispatches`, `InfraCoreRewards`, `SynchroSlots`, `ResearchProgress`, `OutpostBuildings` | Dispatch, Infra, and Synchro state (`outpost/dispatch/get`, `character/synchrodevice/get`, `outpost/recycleroom/get`, `User/Get`) |
| Planned | Lobby-critical | `UserCompletedScenarios` | `CompletedScenarios` (`user/scenario/exist`) |
| Planned | Lobby-critical | `UserCurrencies`, `UserMainQuests`, `UserTowerProgress`, `UserClearedTutorials`, `UserContentsUnlocks`, `UserViewedNoticePopups` | `Currency`, `MainQuestData`, `TowerProgress`, `ClearedTutorialDataNew`, `ContentsOpenUnlocked`, `ViewedNoticePopupTableIds` |
| Planned | Lobby-critical | `UserResetStates`, `UserCompletedDailyMissions`, `UserCompletedWeeklyMissions`, `UserDailyTowerCounts`, `UserDailyCounselCounts` | `ResetableData`, `WeeklyResetableData` |
| Planned | Lobby-critical | `UserTeamGroups`, `UserTeamSlots`, `UserRepresentationTeamSlots` | `UserTeams`, `RepresentationTeamDataNew` |
| Planned | Lobby-critical | `UserProfileCardDecorations`, `UserProfileCardStickers`, `UserUnlockedCosmetics`, `UserJukeboxSettings`, `UserClaimedJukeboxRewards`, `UserWallpaperSlots`, `UserWallpaperBackgrounds`, `UserWallpaperJukeboxes`, `UserWallpaperFavorites`, `UserWallpaperPlaylists` | `ProfileCardDecoration`, `CostumeList`, `FrameList`, `TitleList`, `ProfileCardsData`, `Memorial`, `LobbyMusic`, `CommanderMusic`, `ClaimedJukeboxRewardTriggers`, `WallpaperList`, `WallpaperBackground`, `WallpaperJukeboxList`, `WallpaperFavoriteList`, `WallpaperPlaylistList` |
| Planned | Lobby-critical | `UserGachaBannerPulls`, `UserGachaPityCounts`, `UserGachaSelectupChoices`, `UserGachaDailyFreePulls`, `UserGachaPaybacks`, `UserGachaPaybackRewards`, `UserNormalShopStates`, `UserNormalShopOrders`, `UserNormalShopPinnedBuys`, `UserPasses`, `UserPassRanks`, `UserPassMissions` | `GachaBannerMaxPulls` (incl. `GachaTutorialPlayCount` compatibility), `GachaPityBannerExecuteCount`, `GachaSelectupChoices`, `GachaDailyFreePulls`, `GachaPaybackData`, `NormalShopStates`, `UserPassInfo` |
| Planned | Lobby-critical | `UserEvents`, `UserEventCompletedScenarios`, `UserEventClearedStages`, `UserLoginEvents`, `UserLoginEventDays`, `UserEventMissions`, `UserEventMissionEntries`, `UserEventShopBuys` | `EventInfo`, `LoginEventInfo`, `EventMissionInfo`, `EventShopBuyCountInfo` |
| Planned | Lobby-critical | `UserBonds`, `UserBondProgressItems`, `UserFavoriteItems`, `UserFavoriteItemQuests`, `UserLiberations`, `UserLiberationMissions`, `UserOpenLiberateTypes` | `BondInfo`, `FavoriteItems`, `FavoriteItemQuests`, `LiberateDatas`, `OpenLiberateTypeIdList` |
| Planned | Lobby-critical | `UserMessages`, `UserPickedMessages`, `UserBadges`, `Triggers` | `MessengerData`, `PickedMessages`, `Badges`, `Triggers` (existing SQLite table; `TowerHelper.cs` cutover pending) |
| Planned | Lobby-critical | `UserCompletedAchievements`, `UserSideStoryStages`, `UserArchiveUnlocks`, `UserLostSectors`, `UserLostSectorClearedStages`, `UserLostSectorObjects` | `CompletedAchievements`, `CompletedSideStoryStages`, `ViewedSideStoryStages`, `UnlockedArchiveRecordIds`, `UnlockedArchiveEventQuestIds`, `LostSectorData` |
| Planned | Secondary | `SoloRaids`, `SoloRaidLevels`, `SoloRaidLogs`, `SoloRaidLogCharacters` | `SoloRaidData` (`soloraid/getperiod` / Solo Raid mode) |
| Planned | Secondary | `SoloRaidMuseumStages`, `SoloRaidMuseumModes`, `SoloRaidMuseumMissionClaims`, `SoloRaidMuseumTeams`, `SoloRaidMuseumTeamSlots`, `SoloRaidMuseumOpenTeams`, `SoloRaidMuseumLogs`, `SoloRaidMuseumLogCharacters` | `SoloRaidMuseumData` |
| Planned | Secondary | `UserMapStates` | `MapJson` |
| Planned | Secondary | `TtsSongScores`, `TtsTotalScores`, `ArcadeScores` | `RankDatas` |

`SdkUsers` and `Triggers` already exist in SQLite. Their overlapping JSON state and any remaining compatibility behavior must be tracked to full cutover before considering those fields Implemented.

## Remaining JSON keys

These are the keys still in the JSON inventory without target designs. They remain `Unplanned` until assigned to a designed table in dedicated planning sessions. Keys marked `Lobby-critical` are read during the initial login → `User/Get` → lobby hydration flow and should be planned first. The shapes are from the Release publish sample; confirm against the actual database before migration because the sample contained one user.

### Top-level objects

| Status | Priority | Key | JSON type |
|---|---|---|---|
| Unplanned | Standard | `ArcadeInTheMirrorData` | Object |
| Unplanned | Standard | `BBQInfoData` | Object |
| Unplanned | Standard | `BtgData` | Object |
| Unplanned | Standard | `BubbleMarchDatas` | Object |
| Unplanned | Standard | `DDRDatas` | Object |
| Unplanned | Standard | `DessertRushData` | Object |
| Unplanned | Standard | `FavoriteSongs` | Object |
| Unplanned | Standard | `FieldInfo` | Object |
| Unplanned | Standard | `FieldInfoNew` | Object |
| Unplanned | Standard | `Guild` | Object |
| Unplanned | Standard | `IsLandBreakerDatas` | Object |
| Unplanned | Standard | `MailDatas` | Object |
| Unplanned | Standard | `MiniGameAzxInfo` | Object |
| Unplanned | Standard | `MiniGameScenarios` | Object |
| Unplanned | Standard | `MiniGameStoryChoice` | Object |
| Unplanned | Standard | `Nksv2Datas` | Object |
| Unplanned | Standard | `PirateCafeData` | Object |
| Unplanned | Standard | `RebuildEdenDatas` | Object |
| Unplanned | Standard | `SoloRaidData` | Object |
| Unplanned | Standard | `SoloRaidMuseumData` | Object |
| Unplanned | Standard | `SortOutData` | Object |
| Unplanned | Standard | `StellarBladeDatas` | Object |
| Unplanned | Standard | `SubQuestData` | Object |
| Unplanned | Standard | `TTSGameData` | Object |
| Unplanned | Standard | `TowerDefenseDatas` | Object |
| Unplanned | Standard | `userPointData` | Object |

### Top-level arrays

| Status | Priority | Key | JSON type |
|---|---|---|---|
| Unplanned | Standard | `ArcadePlaySodaInfoList` | Array |
| Unplanned | Standard | `BackgroundList` | Array |
| Unplanned | Standard | `CharacterWishlist` | Array |
| Unplanned | Standard | `ClearedOutpostScenarioIds` | Array |
| Unplanned | Standard | `CompletedTacticAcademyLessons` | Array |
| Unplanned | Standard | `IconList` | Array |
| Unplanned | Standard | `JukeboxBgm` | Array |
| Unplanned | Standard | `JukeboxThemeList` | Array |
| Unplanned | Standard | `LiveWallpaperList` | Array |
| Unplanned | Standard | `LobbyDecoBackgroundList` | Array |
| Unplanned | Standard | `LobbyPrivateBannerIds` | Array |
| Unplanned | Standard | `MessengerRepairHistory` | Array |
| Unplanned | Standard | `PlayLists` | Array |
| Unplanned | Standard | `PlaySodaInfoData` | Array |
| Unplanned | Standard | `StageClearHistorys` | Array |
| Unplanned | Standard | `StickerList` | Array |

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
| 2026-11-15 | Implemented SQLite tables, EF Core migration `20261115222500_AddServerConfigUserProfileAndBans`, lowercase MD5 credential unification, and runtime cutover for `ServerSettings`, `ServerSecrets`, `EventBanners`, `SdkUsers`, `UserBans`, and `Users` profile/gameplay-state columns. | Runtime cutover complete; `EpinelPS.DBMigration` & `db.json` contraction deferred. |
| 2026-11-15 | Prioritized `Planned` tables and marked `Lobby-critical` `Unplanned` JSON keys based on live login → `User/Get` → lobby hydration logs. | Tracker updated; `Lobby-critical` unplanned keys queued for separate planning sessions. |
| 2026-11-15 | Planned all 50 `Lobby-critical` user keys into relational table designs using `../save-data/db.json`, `../save-data/epinelps.db`, `UserModel.cs`, `DbModels.cs`, and protobuf definitions. | All 50 `Lobby-critical` keys moved to `Planned` in `database-design.md` and tracker. |
