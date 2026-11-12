using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HideoutSendTroops
{
    public class SubModule : MBSubModuleBase
    {
        private const string HarmonyId = "hydelix.hideoutsendtroops";

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                new Harmony(HarmonyId).PatchAll();
            }
            catch (Exception ex)
            {
                // If the patch fails, the vanilla button simply stays next to ours.
                InformationManager.DisplayMessage(new InformationMessage(
                    "Hideout Send Troops: could not hide the vanilla button (" + ex.GetType().Name + ")."));
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (game.GameType is Campaign && gameStarterObject is CampaignGameStarter starter)
            {
                starter.AddBehavior(new HideoutSendTroopsBehavior());
            }
        }
    }
}
