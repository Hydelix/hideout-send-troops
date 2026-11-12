# Hideout Send Troops (Bannerlord v1.4.8.119303)

Replaces the vanilla hideout "Send troops to clear (X%)" (a 6-hour dice roll with no loot/XP)
with a real "Send troops" button: your WHOLE party fights through the normal auto-resolve
simulation. Casualties, XP, loot and prisoners apply like any other battle.

## Requirements
- Bannerlord.Harmony mod (Nexus / Steam Workshop) installed and enabled
- .NET SDK 8 (https://dotnet.microsoft.com/download) to build

## Build + install
1. If your game is not at `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord`,
   use the -p:GameFolder option below.
2. Open a terminal in this folder and run:
       dotnet build -c Release
   or
       dotnet build -c Release -p:GameFolder="D:\Games\Bannerlord"
3. The mod installs itself to `<GameFolder>\Modules\HideoutSendTroops`.
4. In the launcher enable: Bannerlord.Harmony, then Hideout Send Troops (below Harmony).

## Test checklist (new or existing save)
- Approach a hideout with bandits inside -> "Send troops" is in the menu, vanilla one is gone.
- Click it -> simulation screen -> result screen -> you end up back at the hideout menu / map.
- Win: hideout empties, loot/prisoners/XP given.
- Lose: normal defeat flow.
