﻿using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using System.Linq;
using TabletopTweaks.Core.NewActions;
using TabletopTweaks.Core.NewComponents;
using TabletopTweaks.Core.Utilities;
using static TabletopTweaks.Reworks.Main;

namespace TabletopTweaks.Reworks.Reworks {
    static class Lich {
        [HarmonyPatch(typeof(BlueprintsCache), "Init")]
        static class BlueprintsCache_Init_Patch {
            static bool Initialized;
            static BlueprintUnitPropertyReference LichDCProperty = BlueprintTools.GetModBlueprintReference<BlueprintUnitPropertyReference>(TTTContext, "LichDCProperty");

            static void Postfix() {
                if (Initialized) return;
                Initialized = true;
                TTTContext.Logger.LogHeader("Lich Rework");
                PatchDeadlyMagic();
                PatchDecayingTouch();
                PatchEclipseChill();
                PatchFearControl();
                PatchTainedSneakAttack();
            }

            static void PatchDeadlyMagic() {
                if (TTTContext.Homebrew.MythicReworks.Lich.IsDisabled("DeadlyMagic")) { return; }

                var DeadlyMagicFeature = BlueprintTools.GetBlueprint<BlueprintFeature>("47a8a7fa7d4198f449db71cdbe4b8d3e");
                var DeadlyMagicToggleAbility = BlueprintTools.GetBlueprint<BlueprintActivatableAbility>("e72727ff8f28cae47a4cd56655ce7b10");
                var DeadlyMagicBuff = BlueprintTools.GetBlueprint<BlueprintBuff>("27ebfae71cce46045814eb3ba4fefa6b");
                var DeadlyMagicResource = BlueprintTools.GetBlueprint<BlueprintAbilityResource>("a3441d150c5fec54bbbc04efdefaf6aa");

                DeadlyMagicFeature.SetDescription(TTTContext, "For a number of rounds equal to 3 + half his mythic rank, " +
                    "a Lich can make all spells he casts to ignore spell resistance and spell immunity. " +
                    "Any creature affected by such spell can't cast spells for 1d3+1 rounds.");
                DeadlyMagicToggleAbility.m_Description = DeadlyMagicFeature.m_Description;
                DeadlyMagicBuff.m_Description = DeadlyMagicFeature.m_Description;

                DeadlyMagicResource.TemporaryContext(bp => {
                    bp.m_MaxAmount = new BlueprintAbilityResource.Amount() {
                        m_Class = new BlueprintCharacterClassReference[0],
                        m_Archetypes = new BlueprintArchetypeReference[0],
                        m_ClassDiv = Game.Instance.BlueprintRoot.Progression.m_CharacterMythics,
                        BaseValue = 3,
                        IncreasedByLevelStartPlusDivStep = true,
                        LevelStep = 2,
                        PerStepIncrease = 1
                    };
                });
            }
            static void PatchDecayingTouch() {
                if (TTTContext.Homebrew.MythicReworks.Lich.IsDisabled("DecayingTouch")) { return; }

                var DecayingTouchFeature = BlueprintTools.GetBlueprint<BlueprintFeature>("3eb8922c8a9e25048b6689322c5ae131");
                var PlantType = BlueprintTools.GetBlueprintReference<BlueprintUnitFactReference>("706e61781d692a042b35941f14bc41c5");

                DecayingTouchFeature.SetComponents();
                DecayingTouchFeature.TemporaryContext(bp => {
                    bp.AddComponent<AddAdditionalWeaponDamage>(c => {
                        c.CheckWeaponGroup = true;
                        c.Group = WeaponFighterGroup.Natural;
                        c.Value = new ContextDiceValue() {
                            DiceType = DiceType.D6,
                            DiceCountValue = 1,
                            BonusValue = new ContextValue() {
                                ValueType = ContextValueType.Rank
                            }
                        };
                        c.DamageType = new DamageTypeDescription() {
                            Type = DamageType.Energy,
                            Energy = DamageEnergyType.Unholy
                        };
                    });
                    bp.AddComponent<AdditionalDiceOnAttack>(c => {
                        c.OnHit = true;
                        c.AllNaturalAndUnarmed = true;
                        c.InitiatorConditions = new ConditionsChecker();
                        c.TargetConditions = new ConditionsChecker() {
                            Conditions = new Condition[] {
                                new ContextConditionHasFact() {
                                    m_Fact = PlantType,
                                }
                            }
                        };
                        c.Value = new ContextDiceValue() {
                            DiceType = DiceType.D6,
                            DiceCountValue = 1,
                            BonusValue = new ContextValue() {
                                ValueType = ContextValueType.Rank
                            },
                        };
                        c.DamageType = new DamageTypeDescription() {
                            Type = DamageType.Energy,
                            Energy = DamageEnergyType.Unholy
                        };
                    });
                    bp.AddComponent<AdditionalDiceOnAttack>(c => {
                        c.OnHit = true;
                        c.CheckWeaponRangeType = true;
                        c.RangeType = WeaponRangeType.MeleeTouch;
                        c.InitiatorConditions = new ConditionsChecker();
                        c.TargetConditions = new ConditionsChecker() {
                            Conditions = new Condition[] {
                                new ContextConditionHasFact() {
                                    m_Fact = PlantType,
                                    Not = true
                                }
                            }
                        };
                        c.Value = new ContextDiceValue() {
                            DiceType = DiceType.D6,
                            DiceCountValue = 1,
                            BonusValue = new ContextValue() {
                                ValueType = ContextValueType.Rank
                            },
                        };
                        c.DamageType = new DamageTypeDescription() {
                            Type = DamageType.Energy,
                            Energy = DamageEnergyType.Unholy
                        };
                    });
                    bp.AddComponent<AdditionalDiceOnAttack>(c => {
                        c.OnHit = true;
                        c.CheckWeaponRangeType = true;
                        c.RangeType = WeaponRangeType.MeleeTouch;
                        c.InitiatorConditions = new ConditionsChecker();
                        c.TargetConditions = new ConditionsChecker() {
                            Conditions = new Condition[] {
                                new ContextConditionHasFact() {
                                    m_Fact = PlantType,
                                }
                            }
                        };
                        c.Value = new ContextDiceValue() {
                            DiceType = DiceType.D6,
                            DiceCountValue = 2,
                            BonusValue = new ContextValue() {
                                ValueType = ContextValueType.Rank
                            },
                        };
                        c.DamageType = new DamageTypeDescription() {
                            Type = DamageType.Energy,
                            Energy = DamageEnergyType.Unholy
                        };
                    });
                    bp.AddComponent<AddInitiatorAttackWithWeaponTrigger>(c => {
                        c.OnlyHit = true;
                        c.AllNaturalAndUnarmed = true;
                        c.Action = Helpers.CreateActionList(
                            Helpers.Create<ContextActionDealDamage>(a => {
                                a.m_Type = ContextActionDealDamage.Type.AbilityDamage;
                                a.AbilityType = StatType.Strength;
                                a.DamageType = new DamageTypeDescription();
                                a.Duration = new ContextDurationValue() {
                                    DiceCountValue = 0,
                                    BonusValue = 0
                                };
                                a.Value = new ContextDiceValue() {
                                    DiceCountValue = 0,
                                    BonusValue = 1
                                };
                            })
                        );
                    });
                    bp.AddComponent<AddInitiatorAttackWithWeaponTrigger>(c => {
                        c.OnlyHit = true;
                        c.CheckWeaponRangeType = true;
                        c.RangeType = WeaponRangeType.MeleeTouch;
                        c.Action = Helpers.CreateActionList(
                            Helpers.Create<ContextActionDealDamage>(a => {
                                a.m_Type = ContextActionDealDamage.Type.AbilityDamage;
                                a.AbilityType = StatType.Strength;
                                a.DamageType = new DamageTypeDescription();
                                a.Duration = new ContextDurationValue() {
                                    DiceCountValue = 0,
                                    BonusValue = 0
                                };
                                a.Value = new ContextDiceValue() {
                                    DiceCountValue = 0,
                                    BonusValue = 1
                                };
                            })
                        );
                    });
                    bp.AddContextRankConfig(c => {
                        c.m_BaseValueType = ContextRankBaseValueType.MythicLevel;
                    });
                });

                TTTContext.Logger.LogPatch(DecayingTouchFeature);
            }
            static void PatchEclipseChill() {
                if (TTTContext.Homebrew.MythicReworks.Lich.IsDisabled("EclipseChill")) { return; }

                var EclipseChillFeature = BlueprintTools.GetBlueprint<BlueprintFeature>("731bebb09171d5748b6f08cbe88f8af7");
                var EclipseChillToggleAbility = BlueprintTools.GetBlueprint<BlueprintActivatableAbility>("a34b61de2713f604c9971d640ec50b8a");
                var EclipseChillOnBuff = BlueprintTools.GetBlueprint<BlueprintBuff>("1d585582fbe72e14aadc5cd7985c06f4");
                var EclipseChillEffectBuff = BlueprintTools.GetBlueprint<BlueprintBuff>("1e82cabbfc9b30c44bcc1354b3daa6f4");
                var EclipseChillResource = BlueprintTools.GetBlueprint<BlueprintAbilityResource>("b134e2d400adc4a49bd217a7953d6d6a");
                var PlantType = BlueprintTools.GetBlueprintReference<BlueprintUnitFactReference>("706e61781d692a042b35941f14bc41c5");

                EclipseChillFeature.SetDescription(TTTContext, "For a number of rounds equal to 3 + half his mythic rank, a Lich can imbue all spells he casts with the " +
                    "powers of the Eclipse chill. Creatures affected by such spells must pass a Fortitude saving throw " +
                    "(DC = 10 + 1/2 character level + your mythic rank + your highest stat bonus) or become blinded and suffer (2d8 + mythic rank) " +
                    "cold damage, becoming vulnerable to cold and negative energy until the end of the combat.");
                EclipseChillToggleAbility.m_Description = EclipseChillFeature.m_Description;
                EclipseChillEffectBuff.m_Description = EclipseChillFeature.m_Description;
                EclipseChillResource.TemporaryContext(bp => {
                    bp.m_MaxAmount = new BlueprintAbilityResource.Amount() {
                        m_Class = new BlueprintCharacterClassReference[0],
                        m_Archetypes = new BlueprintArchetypeReference[0],
                        m_ClassDiv = Game.Instance.BlueprintRoot.Progression.m_CharacterMythics,
                        BaseValue = 3,
                        IncreasedByLevelStartPlusDivStep = true,
                        LevelStep = 2,
                        PerStepIncrease = 1
                    };
                });

                EclipseChillOnBuff.SetComponents();
                EclipseChillOnBuff.TemporaryContext(bp => {
                    bp.m_DisplayName = EclipseChillFeature.m_DisplayName;
                    bp.m_Flags = BlueprintBuff.Flags.StayOnDeath;
                    bp.AddComponent<AddAbilityUseTrigger>(c => {
                        c.ActionsOnAllTargets = true;
                        c.Action = Helpers.CreateActionList(
                            Helpers.Create<Conditional>(conditional => {
                                conditional.ConditionsChecker = new ConditionsChecker() {
                                    Conditions = new Condition[] {
                                        new ContextConditionIsAlly()
                                    }
                                };
                                conditional.IfTrue = Helpers.CreateActionList();
                                conditional.IfFalse = Helpers.CreateActionList(
                                    Helpers.Create<ContextActionSavingThrow>(save => {
                                        save.Type = SavingThrowType.Fortitude;
                                        save.HasCustomDC = true;
                                        save.CustomDC = new ContextValue() {
                                            ValueType = ContextValueType.CasterCustomProperty,
                                            m_CustomProperty = LichDCProperty
                                        };
                                        save.Actions = Helpers.CreateActionList(
                                            Helpers.Create<ContextActionConditionalSaved>(condition => {
                                                condition.Succeed = Helpers.CreateActionList();
                                                condition.Failed = Helpers.CreateActionList(
                                                    Helpers.Create<ContextActionApplyBuff>(applyBuff => {
                                                        applyBuff.m_Buff = EclipseChillEffectBuff.ToReference<BlueprintBuffReference>();
                                                        applyBuff.Permanent = true;
                                                        applyBuff.DurationValue = new ContextDurationValue() {
                                                            DiceCountValue = new ContextValue(),
                                                            BonusValue = new ContextValue()
                                                        };
                                                    })
                                                );
                                            }),
                                            Helpers.Create<ContextActionDealDamageTTT>(a => {
                                                a.DamageType = new DamageTypeDescription() {
                                                    Type = DamageType.Energy,
                                                    Energy = DamageEnergyType.Cold,
                                                    Common = new DamageTypeDescription.CommomData(),
                                                    Physical = new DamageTypeDescription.PhysicalData()
                                                };
                                                a.Duration = new ContextDurationValue() {
                                                    m_IsExtendable = true,
                                                    DiceCountValue = 0,
                                                    BonusValue = 0
                                                };
                                                a.Value = new ContextDiceValue() {
                                                    DiceType = DiceType.D8,
                                                    DiceCountValue = 2,
                                                    BonusValue = new ContextValue() {
                                                        ValueType = ContextValueType.Rank
                                                    }
                                                };
                                                a.IgnoreWeapon = true;
                                            })
                                        );
                                    })
                                );
                            })
                        );
                    });
                    bp.AddContextRankConfig(c => {
                        c.m_BaseValueType = ContextRankBaseValueType.MythicLevel;
                    });
                });
                TTTContext.Logger.LogPatch(EclipseChillFeature);
                TTTContext.Logger.LogPatch(EclipseChillToggleAbility);
                TTTContext.Logger.LogPatch(EclipseChillOnBuff);
                TTTContext.Logger.LogPatch(EclipseChillEffectBuff);
                TTTContext.Logger.LogPatch(EclipseChillResource);
            }
            static void PatchFearControl() {
                if (TTTContext.Homebrew.MythicReworks.Lich.IsDisabled("FearControl")) { return; }

                var FearControlAura = BlueprintTools.GetBlueprint<BlueprintAbilityAreaEffect>("bc54394234798444f827b26bb85171d7");

                FearControlAura.FlattenAllActions()
                    .OfType<ContextActionSavingThrow>()
                    .ForEach(savingThrow => {
                        savingThrow.HasCustomDC = true;
                        savingThrow.CustomDC = new ContextValue() {
                            ValueType = ContextValueType.CasterCustomProperty,
                            m_CustomProperty = LichDCProperty
                        };
                    });
            }
            static void PatchTainedSneakAttack() {
                if (TTTContext.Homebrew.MythicReworks.Lich.IsDisabled("TainedSneakAttack")) { return; }

                var TaintedSneakAttackFeature = BlueprintTools.GetBlueprint<BlueprintFeature>("e6ce101a94ac9034b8b55c546e74b9dd");
                var TaintedSneakAttackBuff = BlueprintTools.GetBlueprint<BlueprintBuff>("7860e92789511a24dba5906ac8d65f90");
                var SneakAttack = BlueprintTools.GetBlueprintReference<BlueprintUnitFactReference>("9b9eac6709e1c084cb18c3a366e0ec87");

                TaintedSneakAttackFeature.TemporaryContext(bp => {
                    bp.SetDescription(TTTContext, "Whenever Lich lands a successful sneak attack, the enemy must pass Fortitude saving throw " +
                        "(DC = 10 + 1/2 character level + your mythic rank + your highest stat bonus) or become tainted. The tainted creature is vulnerable to " +
                        "all weapon and elemental damage, as well as suffers a –2 penalty on all attack rolls and weapon damage " +
                        "rolls, until the end of the combat.\n" +
                        "Additionally, Lich's sneak attack damage is increased by 1d6.");
                    bp.SetComponents();
                    bp.AddComponent<AddSneakAttackDamageTrigger>(c => {
                        c.Action = Helpers.CreateActionList(
                            Helpers.Create<ContextActionSavingThrow>(save => {
                                save.Type = SavingThrowType.Fortitude;
                                save.HasCustomDC = true;
                                save.CustomDC = new ContextValue() {
                                    ValueType = ContextValueType.CasterCustomProperty,
                                    m_CustomProperty = LichDCProperty
                                };
                                save.Actions = Helpers.CreateActionList(
                                    Helpers.Create<ContextActionConditionalSaved>(condition => {
                                        condition.Succeed = Helpers.CreateActionList();
                                        condition.Failed = Helpers.CreateActionList(
                                            Helpers.Create<ContextActionApplyBuff>(applyBuff => {
                                                applyBuff.m_Buff = TaintedSneakAttackBuff.ToReference<BlueprintBuffReference>();
                                                applyBuff.Permanent = true;
                                                applyBuff.DurationValue = new ContextDurationValue() {
                                                    DiceCountValue = new ContextValue(),
                                                    BonusValue = new ContextValue()
                                                };
                                            })
                                        );
                                    })
                                );
                            })
                        );
                    });
                    bp.AddComponent<AddFacts>(c => {
                        c.m_Facts = new BlueprintUnitFactReference[] { SneakAttack };
                    });
                });
                TaintedSneakAttackBuff.TemporaryContext(bp => {
                    bp.m_Description = TaintedSneakAttackFeature.m_Description;
                    bp.m_Flags &= ~BlueprintBuff.Flags.StayOnDeath;
                    bp.RemoveComponents<AddDamageTypeVulnerability>();
                    bp.AddComponent<AddDamageTypeVulnerability>(c => {
                        c.PhyscicalForm = true;
                        c.FormType = PhysicalDamageForm.Bludgeoning;
                    });
                    bp.AddComponent<AddDamageTypeVulnerability>(c => {
                        c.PhyscicalForm = true;
                        c.FormType = PhysicalDamageForm.Slashing;
                    });
                    bp.AddComponent<AddDamageTypeVulnerability>(c => {
                        c.PhyscicalForm = true;
                        c.FormType = PhysicalDamageForm.Piercing;
                    });
                    bp.AddComponent<RemoveWhenCombatEnded>();
                });

                TTTContext.Logger.LogPatch(TaintedSneakAttackFeature);
                TTTContext.Logger.LogPatch(TaintedSneakAttackBuff);
            }
        }
    }
}
