using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace HideoutSendTroops
{
    // Hides the vanilla "Send troops to clear (X%)" option (the 6-hour dice roll)
    // in both hideout menus. Our own "Send troops" option replaces it.
    [HarmonyPatch(typeof(HideoutCampaignBehavior), "game_menu_send_troops_hideout_on_condition")]
    internal static class HideVanillaSendTroopsPatch
    {
        private static void Postfix(ref bool __result)
        {
            __result = false;
        }
    }
}
