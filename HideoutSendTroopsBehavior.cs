using System;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace HideoutSendTroops
{
    /// <summary>
    /// Adds a "Send troops" button to hideout menus that sends the WHOLE party into the
    /// normal auto-resolve battle simulation (same system as field battles and sieges).
    /// </summary>
    public class HideoutSendTroopsBehavior : CampaignBehaviorBase
    {
        private const string OptionId = "hst_send_troops";

        // Not saved: only tracks the current simulation so we can tidy up the menu afterwards.
        private bool _awaitingEnd;
        private bool _simSeen;
        private int _waitTicks;
        private bool _ourBattle;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnHideoutBattleCompletedEvent.AddNonSerializedListener(this, OnHideoutBattleCompleted);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Vanilla order: hideout_place = attack, assault, wait, send_troops, leave
            //                hideout_after_wait = attack, assault, send_troops, leave
            // Insert ours where vanilla's send_troops sits.
            starter.AddGameMenuOption("hideout_place", OptionId, "{=hst_send_troops}Send troops",
                SendTroopsCondition, SendTroopsConsequence, false, 3);
            starter.AddGameMenuOption("hideout_after_wait", OptionId, "{=hst_send_troops}Send troops",
                SendTroopsCondition, SendTroopsConsequence, false, 2);
        }

        private static bool SendTroopsCondition(MenuCallbackArgs args)
        {
            Settlement settlement = Settlement.CurrentSettlement;
            if (settlement == null || !settlement.IsHideout)
            {
                return false;
            }

            // Same visibility rules as vanilla's send-troops option.
            if (settlement.MapFaction == PartyBase.MainParty.MapFaction ||
                !settlement.Parties.Any(p => p.IsBandit) ||
                !settlement.Hideout.NextPossibleAttackTime.IsPast)
            {
                return false;
            }

            args.optionLeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack;
            args.Tooltip = new TextObject(
                "{=hst_tooltip}Your entire party fights the bandits in a simulated battle. Casualties, experience, loot and prisoners apply as in a normal battle.");

            if (MobileParty.MainParty.MemberRoster.TotalHealthyCount <= 0)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=hst_no_troops}You have no healthy troops to send.");
            }

            return true;
        }

        private void SendTroopsConsequence(MenuCallbackArgs args)
        {
            Settlement hideout = Settlement.CurrentSettlement;

            // Vanilla records how many bandits were inside (used for the night-time loot bonus).
            TryRefreshInitialPopulation();

            // Same setup vanilla uses before an assault mission.
            if (PlayerEncounter.IsActive)
            {
                PlayerEncounter.LeaveEncounter = false;
            }
            else
            {
                PlayerEncounter.Start();
                PlayerEncounter.Current.SetupFields(PartyBase.MainParty, hideout.Party);
            }

            if (PlayerEncounter.Battle == null)
            {
                PlayerEncounter.StartBattle();
                PlayerEncounter.Update();
            }

            _ourBattle = true;
            _awaitingEnd = true;
            _simSeen = false;
            _waitTicks = 0;

            // Same three steps vanilla's field-battle "Send troops" button performs.
            GameMenu.ExitToLast();
            PlayerEncounter.InitSimulation(null, null);
            if (PlayerEncounter.Current != null && PlayerEncounter.CurrentBattleSimulation != null)
            {
                (Game.Current.GameStateManager.ActiveState as MapState)?.StartBattleSimulation();
            }
        }

        private void OnHideoutBattleCompleted(BattleSideEnum winnerSide, HideoutEventComponent component,
            HideoutEventComponent.HideoutBattleEndState endState)
        {
            if (!_ourBattle)
            {
                return;
            }
            _ourBattle = false;

            if (endState != HideoutEventComponent.HideoutBattleEndState.Victory)
            {
                return;
            }

            // Vanilla gives nearby notables +relation when a hideout is cleared.
            try
            {
                Settlement hideout = component?.MapEvent?.MapEventSettlement;
                var vanilla = Campaign.Current.GetCampaignBehavior<HideoutCampaignBehavior>();
                if (hideout != null && vanilla != null)
                {
                    AccessTools.Method(typeof(HideoutCampaignBehavior), "SetCleanHideoutRelations")
                        ?.Invoke(vanilla, new object[] { hideout });
                }
            }
            catch (Exception)
            {
                // Cosmetic bonus only; never break the battle result.
            }
        }

        private void OnTick(float dt)
        {
            if (!_awaitingEnd)
            {
                return;
            }

            if (PlayerEncounter.CurrentBattleSimulation != null)
            {
                _simSeen = true;
                return;
            }

            if (!_simSeen)
            {
                // The simulation never started; stop waiting after a short while.
                if (++_waitTicks > 300)
                {
                    _awaitingEnd = false;
                    _ourBattle = false;
                }
                return;
            }

            // Simulation finished. If the encounter is still open at the hideout and no menu
            // is showing, bring the hideout menu back so the player isn't left on a blank map.
            _awaitingEnd = false;
            _simSeen = false;
            try
            {
                if (PlayerEncounter.Current != null &&
                    Settlement.CurrentSettlement != null &&
                    Settlement.CurrentSettlement.IsHideout &&
                    Campaign.Current.CurrentMenuContext == null)
                {
                    GameMenu.ActivateGameMenu("hideout_place");
                }
            }
            catch (Exception)
            {
            }
        }

        private static void TryRefreshInitialPopulation()
        {
            try
            {
                var vanilla = Campaign.Current.GetCampaignBehavior<HideoutCampaignBehavior>();
                if (vanilla != null)
                {
                    AccessTools.Method(typeof(HideoutCampaignBehavior), "UpdateInitialHideoutPopulation")
                        ?.Invoke(vanilla, null);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
