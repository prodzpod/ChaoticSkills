using System;

namespace ChaoticSkills.Content {
    public abstract class SkillBase<T> : SkillBase where T : SkillBase<T>
    {
        public static T Instance { get; private set; }

        public SkillBase()
        {
            if (Instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting SkillBase was instantiated twice");
            Instance = this as T;
        }
    }
    public abstract class SkillBase {
        public abstract SerializableEntityStateType ActivationState { get; }
        public abstract float Cooldown { get; }
        public virtual int StockToConsume { get; } = 1;
        public abstract string Machine { get; }
        public abstract int MaxStock { get; }
        public abstract string LangToken { get; }
        public abstract string Name { get; }
        public virtual string Survivor { get; } = null;
        public virtual SkillSlot Slot { get; } = SkillSlot.None;
        public abstract string Description { get; }
        public virtual bool IsCombat { get; } = true;
        public virtual bool Agile { get; } = false;
        public virtual bool DelayCooldown { get; } = false;
        public virtual List<string> Keywords { get; } = new();
        public abstract Sprite SkillIcon { get; }
        public virtual UnlockableDef Unlock { get; } = null;
        public virtual bool AutoApply { get; } = true;
        public virtual bool MustKeyPress { get; } = false;
        public virtual bool Passive { get; } = false;
        public SkillDef SkillDef;

        public void Init() {
            SkillDef = ScriptableObject.CreateInstance<SkillDef>();
            SkillDef.skillNameToken = "SKILL_" + LangToken + "_NAME";
            SkillDef.skillDescriptionToken = "SKILL_" + LangToken + "_DESC";
            SkillDef.skillName = LangToken;
            SkillDef.baseRechargeInterval = Cooldown;
            SkillDef.baseMaxStock = MaxStock;
            SkillDef.activationState = ActivationState;
            SkillDef.canceledFromSprinting = Passive ? false : !Agile;
            SkillDef.icon = SkillIcon;
            SkillDef.cancelSprintingOnActivation = !Agile;
            SkillDef.isCombatSkill = IsCombat;
            SkillDef.activationStateMachineName = Machine;
            SkillDef.beginSkillCooldownOnSkillEnd = DelayCooldown;
            SkillDef.stockToConsume = StockToConsume;
            SkillDef.requiredStock = Passive ? 321 : 1;
            SkillDef.mustKeyPress = MustKeyPress;
            List<string> newKeywords = Keywords;

            if (Agile) {
                newKeywords.Add(Utils.Keywords.Agile);
            }
            
            if (newKeywords.Count >= 1) {
                SkillDef.keywordTokens = newKeywords.ToArray();
            }

            if (AutoApply && Passive) {
                GameObject survivor = Survivor.Load<GameObject>();
                bool wasPassiveReal = false;

                foreach (GenericSkill skill in survivor.GetComponents<GenericSkill>()) {
                    if (skill.skillName.ToLower().Contains("passive") || (skill.skillFamily as ScriptableObject).name.ToLower().Contains("passive")) {
                        SkillFamily family = skill.skillFamily;

                        Array.Resize(ref family.variants, family.variants.Length + 1);
                
                        family.variants[family.variants.Length - 1] = new SkillFamily.Variant {
                            skillDef = SkillDef,
                            unlockableDef = Unlock,
                            viewableNode = new ViewablesCatalog.Node(SkillDef.skillNameToken, false, null)
                        };
                        wasPassiveReal = true;
                        break;
                    }
                }

                if (!wasPassiveReal) {
                    GenericSkill skill = survivor.AddComponent<GenericSkill>();
                    SkillLocator locator = survivor.GetComponent<SkillLocator>();
                    SkillFamily family = ScriptableObject.CreateInstance<SkillFamily>();
                    skill.skillName = survivor.name + "Passive";
                    (family as ScriptableObject).name = survivor.name + "Passive";
                    family.variants = new SkillFamily.Variant[2];
                    
                    family.variants[1] = new SkillFamily.Variant {
                        skillDef = SkillDef,
                        unlockableDef = Unlock,
                        viewableNode = new ViewablesCatalog.Node(SkillDef.skillNameToken, false, null)
                    };

                    SkillDef oldPassive = ScriptableObject.CreateInstance<SkillDef>();
                    oldPassive.skillNameToken = locator.passiveSkill.skillNameToken;
                    oldPassive.skillDescriptionToken = locator.passiveSkill.skillDescriptionToken;
                    oldPassive.activationStateMachineName = "TheAmongUs";
                    oldPassive.activationState = new SerializableEntityStateType(typeof(Idle));
                    oldPassive.icon = locator.passiveSkill.icon;
                    
                    ContentAddition.AddSkillDef(oldPassive);

                    locator.passiveSkill.enabled = false;
                    skill.hideInCharacterSelect = true;

                    family.variants[0] = new SkillFamily.Variant {
                        skillDef = oldPassive,
                        viewableNode = new ViewablesCatalog.Node(oldPassive.skillNameToken, false, null)
                    };

                    skill._skillFamily = family;
                }
            }
            else if (AutoApply) {
                GameObject survivor = Survivor.Load<GameObject>();
                SkillLocator skillLocator = survivor.GetComponent<SkillLocator>();

                SkillFamily family = null;

                switch (Slot) {
                    case SkillSlot.Primary:
                        family = skillLocator.primary.skillFamily;
                        break;
                    case SkillSlot.Secondary:
                        family = skillLocator.secondary.skillFamily;
                        break;
                    case SkillSlot.Utility:
                        family = skillLocator.utility.skillFamily;
                        break;
                    case SkillSlot.Special:
                        family = skillLocator.special.skillFamily;
                        break;
                    default:
                        break;
                }

                if (family != null) {
                    Array.Resize(ref family.variants, family.variants.Length + 1);
                    
                    family.variants[family.variants.Length - 1] = new SkillFamily.Variant {
                        skillDef = SkillDef,
                        unlockableDef = Unlock,
                        viewableNode = new ViewablesCatalog.Node(SkillDef.skillNameToken, false, null)
                    };
                }
            }

            SkillDef.skillNameToken.Add(Name);
            string tempDesc = Description;
            if (Agile) {
                tempDesc = "<style=cIsUtility>Agile.</style> " + tempDesc;
            }
            SkillDef.skillDescriptionToken.Add(tempDesc);

            ContentAddition.AddSkillDef(SkillDef);

            PostCreation();
        }

        public virtual void PostCreation() {

        }
    }
}