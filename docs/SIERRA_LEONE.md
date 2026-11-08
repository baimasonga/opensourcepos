# Sierra Leone Configuration

This fork ships with defaults for shops in Sierra Leone. They are applied by the
`SierraLeoneLocaleDefaults` migration on new installs, and on upgrades only for
settings still on the stock (US) defaults, so values you already changed stay as they are.

| Setting                | Value                       |
|------------------------|-----------------------------|
| Currency               | Leone, symbol `Le`, ISO code `SLE` (2 decimals) |
| Number locale          | `en_SL` (e.g. `Le 1,234.50`) |
| Timezone               | `Africa/Freetown` (GMT, no daylight saving) |
| Date format            | `d/m/Y`                     |
| Language               | English (United Kingdom), `en-GB` |
| Address lookup country | `sl`                        |
| Default tax            | GST at 15%                  |

The Docker compose files also set `PHP_TIMEZONE=Africa/Freetown`.

All of these can be changed under **Configuration → Locale** and **Configuration → Tax**.
Check the GST rate and whether your prices include tax (**Tax Included**) against current
National Revenue Authority guidance for your business.
