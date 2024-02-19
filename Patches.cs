using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnhollowerBaseLib;
using UnityEngine;

namespace CstiCheatConsoleMobile
{
    public static class Patches
    {
        private static readonly Stack<InGameCardBase[]> GiveOperations = new Stack<InGameCardBase[]>();

        private static void ClearEmptyCardOperation()
        {
            if (GiveOperations.Count <= 0) return;
            var peek = GiveOperations.Peek();
            while (peek.Length == 0 || peek[0].CardModel == null)
            {
                GiveOperations.Pop();
                if (GiveOperations.Count == 0) break;
                peek = GiveOperations.Peek();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CheatsManager), "CheatsActive", MethodType.Getter)]
        public static bool PatchCheatsActive(ref bool __result)
        {
            __result = Plugin.Enabled;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CheatsManager), "CardsGUI")]
        public static bool PatchCardsGUI(CheatsManager __instance)
        {
            if (!__instance.GM)
            {
                return false;
            }

            if (__instance.AllCards == null)
            {
                __instance.FillCards();
            }

            if (__instance.AllCards == null)
            {
                return false;
            }

            if (__instance.AllCards.Length == 0)
            {
                return false;
            }

            GUILayout.BeginVertical("box", new Il2CppReferenceArray<GUILayoutOption>(0));
            GUILayout.Label(new LocalizedString
            {
                LocalizationKey = "CstiCheatMode.Cards",
                DefaultText = "Cards"
            }, new Il2CppReferenceArray<GUILayoutOption>(0));
            ClearEmptyCardOperation();
            if (GUILayout.Button(
                    $"{new LocalizedString { LocalizationKey = "CstiCheatMode.Undo", DefaultText = "Undo last operation" }.ToString()}{(GiveOperations.Count > 0 ? $": {GiveOperations.Peek()[0].CardModel.CardName}*{GiveOperations.Peek().Length}" : " (None)")}",
                    new Il2CppReferenceArray<GUILayoutOption>(0)))
            {
                if (GiveOperations.Count != 0)
                {
                    var lastOperation = GiveOperations.Pop();
                    foreach (var card in lastOperation)
                    {
                        if (!card || !__instance.GM.AllCards.Contains(card)) continue;
                        GameManager.PerformAction(card.CardModel.DefaultDiscardAction, card, true);
                    }
                }
            }

            GUILayout.BeginHorizontal(new Il2CppReferenceArray<GUILayoutOption>(0));
            GUILayout.Label(new LocalizedString
            {
                LocalizationKey = "CstiCheatMode.Search",
                DefaultText = "Search"
            }, new Il2CppReferenceArray<GUILayoutOption>(0));
            __instance.SearchedCardString =
                GUILayout.TextField(__instance.SearchedCardString, new Il2CppReferenceArray<GUILayoutOption>(0));
            GUILayout.EndHorizontal();
            __instance.CardsListScrollView =
                GUILayout.BeginScrollView(__instance.CardsListScrollView,
                    new Il2CppReferenceArray<GUILayoutOption>(new[] { GUILayout.ExpandHeight(true) }));
            __instance.SearchedCardString = __instance.SearchedCardString ?? "";
            for (var i = 0; i < __instance.AllCards.Length; i++)
            {
                if (!__instance.AllCards[i].name.ToLower().Contains(__instance.SearchedCardString.ToLower()) &&
                    !__instance.AllCards[i].CardName.ToString().ToLower()
                        .Contains(__instance.SearchedCardString.ToLower())) continue;
                if (i / 150 != __instance.CurrentPage && string.IsNullOrEmpty(__instance.SearchedCardString))
                {
                    if (i >= 150 * __instance.CurrentPage)
                    {
                        break;
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal("box", new Il2CppReferenceArray<GUILayoutOption>(0));
                    var card = __instance.AllCards[i];
                    GUILayout.Label($"{card.CardName.ToString()} ({card.name})", new Il2CppReferenceArray<GUILayoutOption>(0));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new LocalizedString
                        {
                            LocalizationKey = "CstiCheatMode.Give",
                            DefaultText = "Give"
                        }, new Il2CppReferenceArray<GUILayoutOption>(0)))
                    {
                        GiveCardsAndStack(card, false);
                    }

                    if (card.CardType != CardTypes.EnvImprovement)
                    {
                        if (GUILayout.Button(new LocalizedString
                            {
                                LocalizationKey = "CstiCheatMode.Give5",
                                DefaultText = "Give 5"
                            }, new Il2CppReferenceArray<GUILayoutOption>(0)))
                            GiveCardsAndStack(card, false, 5);
                        if (card.CardType == CardTypes.Blueprint)
                        {
                            if (GUILayout.Button(new LocalizedString
                                {
                                    LocalizationKey = "CstiCheatMode.Unlock",
                                    DefaultText = "Unlock"
                                }, new Il2CppReferenceArray<GUILayoutOption>(0)))
                            {
                                GameManager.GiveCard(card, false);
                                __instance.GM.BlueprintModelStates.set_Item(card, BlueprintModelState.Available);
                                if (__instance.GM.PurchasableBlueprintCards.Contains(card))
                                    __instance.GM.PurchasableBlueprintCards.Remove(card);
                            }
                        }
                        else if (card.CardType == CardTypes.Item)
                        {
                            if (GUILayout.Button(new LocalizedString
                                {
                                    LocalizationKey = "CstiCheatMode.Give10",
                                    DefaultText = "Give 10"
                                }, new Il2CppReferenceArray<GUILayoutOption>(0)))
                                GiveCardsAndStack(card, false, 10);
                            if (GUILayout.Button(new LocalizedString
                                {
                                    LocalizationKey = "CstiCheatMode.Give20",
                                    DefaultText = "Give 20"
                                }, new Il2CppReferenceArray<GUILayoutOption>(0)))
                                GiveCardsAndStack(card, false, 20);
                        }
                    }
                    else if (GUILayout.Button(new LocalizedString
                             {
                                 LocalizationKey = "CstiCheatMode.Complete",
                                 DefaultText = "Give and complete"
                             }, new Il2CppReferenceArray<GUILayoutOption>(0)))
                    {
                        GiveCardsAndStack(card, true);
                    }

                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();
            if (string.IsNullOrEmpty(__instance.SearchedCardString))
            {
                GUILayout.BeginHorizontal(new Il2CppReferenceArray<GUILayoutOption>(0));
                if (__instance.CurrentPage == 0)
                {
                    GUILayout.Box("<", new Il2CppReferenceArray<GUILayoutOption>(new[] { GUILayout.Width(25f) }));
                }
                else if (GUILayout.Button("<",
                             new Il2CppReferenceArray<GUILayoutOption>(new[] { GUILayout.Width(25f) })))
                {
                    __instance.CurrentPage--;
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label($"{(__instance.CurrentPage + 1).ToString()}/{__instance.MaxPages.ToString()}",
                    new Il2CppReferenceArray<GUILayoutOption>(0));
                GUILayout.FlexibleSpace();
                if (__instance.CurrentPage == __instance.MaxPages - 1)
                {
                    GUILayout.Box(">", new Il2CppReferenceArray<GUILayoutOption>(new[] { GUILayout.Width(25f) }));
                }
                else if (GUILayout.Button(">",
                             new Il2CppReferenceArray<GUILayoutOption>(new[] { GUILayout.Width(25f) })))
                {
                    __instance.CurrentPage++;
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            return false;
        }

        private static void GiveCardsAndStack(CardData card, bool complete, int amount = 1)
        {
            var cards = new InGameCardBase[amount];
            for (var i = 0; i < amount; i++)
            {
                GameManager.GiveCard(card, complete);
                var gameCard = MBSingleton<GameManager>.Instance.FindLatestCreatedCard(card);
                cards[i] = gameCard;
            }

            GiveOperations.Push(cards);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CheatsManager), "GeneralOptionsGUI")]
        public static bool PatchGeneralOptionsGUI(CheatsManager __instance)
        {
            GUILayout.BeginVertical("box", new Il2CppReferenceArray<GUILayoutOption>(0));
            CheatsManager.ShowFPS = GUILayout.Toggle(CheatsManager.ShowFPS, new GUIContent(new LocalizedString
            {
                LocalizationKey = "CstiCheatMode.FPSCounter",
                DefaultText = "FPS Counter"
            }.ToString()), new Il2CppReferenceArray<GUILayoutOption>(0));
            CheatsManager.CanDeleteAllCards = GUILayout.Toggle(CheatsManager.CanDeleteAllCards, new GUIContent(
                new LocalizedString
                {
                    LocalizationKey = "CstiCheatMode.TrashAll",
                    DefaultText = "All cards can be trashed"
                }.ToString()), new Il2CppReferenceArray<GUILayoutOption>(0));
            Plugin.FastExploration = GUILayout.Toggle(Plugin.FastExploration, new GUIContent(new LocalizedString
            {
                LocalizationKey = "CstiCheatMode.FastExploration",
                DefaultText = "Fast exploration"
            }.ToString()), new Il2CppReferenceArray<GUILayoutOption>(0));
            Plugin.CombatInvincible = GUILayout.Toggle(Plugin.CombatInvincible, new GUIContent(new LocalizedString
            {
                LocalizationKey = "CstiCheatMode.EncounterInvincible",
                DefaultText = "Be invincible in all encounters"
            }.ToString()), new Il2CppReferenceArray<GUILayoutOption>(0));
            GUILayout.BeginHorizontal(new Il2CppReferenceArray<GUILayoutOption>(0));
            GUILayout.Label(
                $"{new LocalizedString { LocalizationKey = "CstiCheatMode.Suns", DefaultText = "Suns" }.ToString()} ({GameLoad.Instance.SaveData.Suns.ToString()})",
                new Il2CppReferenceArray<GUILayoutOption>(0));
            if (GUILayout.Button("+10", new Il2CppReferenceArray<GUILayoutOption>(0)))
            {
                GameLoad.Instance.SaveData.Suns += 10;
            }

            if (GUILayout.Button("+100", new Il2CppReferenceArray<GUILayoutOption>(0)))
            {
                GameLoad.Instance.SaveData.Suns += 100;
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(new Il2CppReferenceArray<GUILayoutOption>(0));
            GUILayout.Label(
                $"{new LocalizedString { LocalizationKey = "CstiCheatMode.Moons", DefaultText = "Moons" }.ToString()} ({GameLoad.Instance.SaveData.Moons.ToString()})",
                new Il2CppReferenceArray<GUILayoutOption>(0));
            if (GUILayout.Button("+10", new Il2CppReferenceArray<GUILayoutOption>(0)))
            {
                GameLoad.Instance.SaveData.Moons += 10;
            }

            if (GUILayout.Button("+100", new Il2CppReferenceArray<GUILayoutOption>(0)))
            {
                GameLoad.Instance.SaveData.Moons += 100;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CheatsManager), "TimeGUI")]
        public static bool TimeGUI(CheatsManager __instance)
        {
            if (!__instance.GM)
            {
                return false;
            }

            GUILayout.BeginVertical("box", new Il2CppReferenceArray<GUILayoutOption>(0));
            GUILayout.Label(new LocalizedString
            {
                LocalizationKey = "CstiCheatMode.SetTimeTitle",
                DefaultText = "Set time to:"
            }.ToString(), new Il2CppReferenceArray<GUILayoutOption>(0));
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            GUILayout.Label(new LocalizedString
            {
                LocalizationKey = "CstiCheatMode.Days",
                DefaultText = "Days:"
            }.ToString(), new Il2CppReferenceArray<GUILayoutOption>(0));
            GUILayout.FlexibleSpace();
            if (GUILayout.RepeatButton("-",
                    new Il2CppReferenceArray<GUILayoutOption>(new[] { GUILayout.Width(25f) })) &&
                Time.frameCount % 4 == 0)
            {
                __instance.SetTimeDay--;
            }

            GUILayout.Label(__instance.SetTimeDay.ToString(),
                new Il2CppReferenceArray<GUILayoutOption>(new[] { GUILayout.Width(37.5f) }));
            if (GUILayout.RepeatButton("+",
                    new Il2CppReferenceArray<GUILayoutOption>(new[] { GUILayout.Width(25f) })) &&
                Time.frameCount % 4 == 0)
            {
                __instance.SetTimeDay++;
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            GUILayout.Label(
                $"{new LocalizedString { LocalizationKey = "CstiCheatMode.Ticks", DefaultText = "Tick" }.ToString()} ({GameManager.TotalTicksToHourOfTheDayString(GameManager.HoursToTick(__instance.GM.DaySettings.DayStartingHour) + __instance.SetTimeTick, 0)}):",
                new Il2CppReferenceArray<GUILayoutOption>(0));
            GUILayout.FlexibleSpace();
            if (GUILayout.RepeatButton("-",
                    new Il2CppReferenceArray<GUILayoutOption>(new[] { GUILayout.Width(25f) })) &&
                Time.frameCount % 4 == 0)
            {
                __instance.SetTimeTick--;
            }

            GUILayout.Label(__instance.SetTimeTick.ToString(),
                new Il2CppReferenceArray<GUILayoutOption>(new[] { GUILayout.Width(37.5f) }));
            if (GUILayout.RepeatButton("+",
                    new Il2CppReferenceArray<GUILayoutOption>(new[] { GUILayout.Width(25f) })) &&
                Time.frameCount % 4 == 0)
            {
                __instance.SetTimeTick++;
            }

            GUILayout.EndHorizontal();
            if (__instance.GM)
            {
                __instance.SetTimeTick = Mathf.Clamp(__instance.SetTimeTick, 0, __instance.GM.DaySettings.DailyPoints);
                __instance.SetTimeDay = Mathf.Max(0, __instance.SetTimeDay);
            }
            else
            {
                GUILayout.Label("No GameManager found", new Il2CppReferenceArray<GUILayoutOption>(0));
            }

            if (GUILayout.Button(new LocalizedString
                {
                    LocalizationKey = "CstiCheatMode.SetTime",
                    DefaultText = "Set time!"
                }.ToString(), new Il2CppReferenceArray<GUILayoutOption>(0)) && __instance.GM)
            {
                __instance.GM.SetTimeTo(__instance.SetTimeDay, __instance.SetTimeTick);
            }

            GUILayout.EndVertical();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CheatsManager), "Update")]
        public static bool PatchCheatsUpdate(CheatsManager __instance)
        {
            if (!Plugin.Enabled) return false;
            if (!__instance.GM)
            {
                __instance.GM = MBSingleton<GameManager>.Instance;
            }

            // if (__instance.GM)
            // {
            //     if (Input.GetKeyDown(Plugin.ForceLoseGameKey))
            //     {
            //         MBSingleton<GameManager>.Instance.OpenEndgameJournal(true);
            //     }
            //     else if (Input.GetKeyDown(Plugin.ForceWinGameKey))
            //     {
            //         MBSingleton<GameManager>.Instance.OpenEndgameJournal(false);
            //     }
            // }

            if (Input.touchCount == 2)
            {
                var touchZero = Input.GetTouch(0);
                var touchOne = Input.GetTouch(1);

                if (touchZero.phase == TouchPhase.Moved && touchOne.phase == TouchPhase.Moved)
                {
                    var touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                    var touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                    var prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                    var touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                    var touchDiff = prevTouchDeltaMag - touchDeltaMag;
                    const float threshold = 0f;

                    if (touchZero.deltaPosition.x > 0 && touchOne.deltaPosition.x > 0 && touchDiff > threshold)
                    {
                        // Detected two fingers swipe to the right
                        __instance.ShowGUI = false;
                        __instance.CheatsMenuBGObject.SetActive(__instance.ShowGUI);
                    }
                    else if (touchZero.deltaPosition.x < 0 && touchOne.deltaPosition.x < 0 && touchDiff > threshold)
                    {
                        // Detected two fingers swipe to the left
                        __instance.ShowGUI = true;
                        __instance.CheatsMenuBGObject.SetActive(__instance.ShowGUI);
                    }
                }
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ExplorationBar), "ShouldUnlockExplorationResults")]
        public static bool PatchShouldUnlockExplorationResults(ExplorationBar __instance, ref bool __result)
        {
            if (!Plugin.FastExploration) return true;
            __result = true;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EncounterPopup), "RollForClash")]
        public static void PatchInvincibleCombatClash(EncounterPopup __instance)
        {
            var encounter = __instance.CurrentEncounter;
            if (Plugin.CombatInvincible)
                encounter.CurrentRoundClashResult = ClashResults.PlayerHits;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EncounterPopup), "GenerateEnemyWound")]
        public static void PatchInvincibleCombatDamage(EncounterPopup __instance)
        {
            if (!Plugin.CombatInvincible) return;
            var encounter = __instance.CurrentEncounter;
            var action = encounter.CurrentPlayerAction;
            if (action.DoesNotAttack || action.Damage.y <= 0f) return;
            encounter.CurrentRoundEnemyWoundSeverity = WoundSeverity.Serious;
            encounter.CurrentRoundEnemyWoundLocation = BodyLocations.Head;
            var wound = encounter.EncounterModel.EnemyBodyTemplate.GetBodyLocation(BodyLocations.Head)
                .GetWoundsForSeverityDamageType(WoundSeverity.Serious,
                    __instance.CurrentRoundPlayerDamageReport.DmgTypes)
                .OrderBy(w => w.EnemyValuesModifiers.BloodModifier.y).First();
            encounter.CurrentRoundEnemyWound = wound;
            __instance.CurrentRoundPlayerDamageReport.AttackSeverity = WoundSeverity.Serious;
        }
    }
}