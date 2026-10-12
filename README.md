# WeatherDataService

Windows Forms service that downloads ASOS/airport weather observations, loads them into SQL
Server (`WX_Raw`), and merges them into `WX_Clean`.

## Configuration

No credentials are stored in the repository. Provide two connection strings either as
environment variables or in `appsettings.local.json` next to the executable (copy
`appsettings.local.example.json`; the file is git-ignored):

| Key | Purpose |
|---|---|
| `WX_RAW_CONNECTION` | SQL Server connection string for the `WX_Raw` database |
| `WX_CLEAN_CONNECTION` | SQL Server connection string for the `WX_Clean` database |

If a value is missing the app stops with a message naming the missing key.
Use a least-privilege SQL login (not `sa`).
