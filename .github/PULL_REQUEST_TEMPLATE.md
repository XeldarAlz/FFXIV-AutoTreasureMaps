## What

<!-- One or two sentences on what this PR changes. -->

## Why

<!-- The motivating problem, linked issue, or user-visible behavior this fixes. -->

Closes #

## How to test

<!--
Minimum steps a reviewer can run to verify the change. Testing usually means carrying one timeworn map you can decipher, pressing Start, and watching the plugin dig up its coffer, fight off the guards and open it end-to-end. Make sure the dependencies listed in /atm deps are installed first. For UI-only changes, describe what to click.
-->

## Checklist

- [ ] `dotnet build -c Release` passes
- [ ] Verified in-game on at least one timeworn map in the affected expansion
- [ ] If this changes user-visible behavior, README is updated
- [ ] If this touches the automation loop, relevant `[AutoTreasureMaps]` log lines make the sequence auditable
