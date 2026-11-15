# EpinelPS database migration

Run once before starting the updated server:

```sh
dotnet run --project EpinelPS.DBMigration -- /path/to/app-root
```

The tool applies the server's EF migrations to `<app-root>/data/epinelps.db`, copies compatible rows from the legacy `SdkUsers`, `Users`, and `Triggers` tables, and imports account, user, and trigger records from `db.json` into those relational tables. Existing SQLite rows take precedence on ID conflicts. Imports are additive and safe to retry; the source files are left unchanged and remain available for rollback.

The source paths are `<app-root>/epinelps.db` and `<app-root>/db.json` (or `<app-root>/data/db.json` when it already exists). The legacy JSON file is copied to `<app-root>/data/db.json` when needed so the current server can continue reading settings and game state during this first migration phase. Nested gameplay state remains in that JSON file for a later migration. The migrated server database is `<app-root>/data/epinelps.db`.
