using System;

namespace ChaoticSkills.Misc {
    public class AllyCaps {
        public struct AllyCap {
            public GameObject prefab;
            public int cap;
            public int lysateCap;
        }

        private static List<AllyCap> caps;

        public static void RegisterAllyCap(GameObject prefab, int max = 1, int maxWithLysate = 2) {
            if (caps == null) {
                caps = new();
            }

            caps.Add(new AllyCap {
                prefab = prefab,
                cap = max,
                lysateCap = maxWithLysate
            });
        }

        public static void Hooks() {
            On.RoR2.CharacterBody.Start += HopooWhy;
        }

        private static void HopooWhy(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self) {
            orig(self);
            if (NetworkServer.active) {
                foreach (AllyCap cap in caps) {
                    if (self.master && self.master.minionOwnership && self.master.minionOwnership.ownerMaster) {
                        BodyIndex index = BodyCatalog.FindBodyIndex(cap.prefab);
                        if (self.bodyIndex == index) {
                            MinionOwnership[] minions = GameObject.FindObjectsOfType<MinionOwnership>().Where(
                                x => x.ownerMaster && x.ownerMaster == self.master.minionOwnership.ownerMaster && x.GetComponent<CharacterMaster>()
                                && x.GetComponent<CharacterMaster>().GetBody() && x.GetComponent<CharacterMaster>().GetBody().bodyIndex == index
                            ).ToArray();

                            int total = 0;
                            int max = 0;
                            bool hasLysate = self.master.minionOwnership.ownerMaster.inventory.GetItemCount(DLC1Content.Items.EquipmentMagazineVoid) > 1;
                            if (Main.config.Bind<bool>("Global", "Uncap Lysate Cell", false, "Uncap the number of minions you can have with Lysate Cell").Value) {
                                max = cap.cap + self.master.minionOwnership.ownerMaster.inventory.GetItemCount(DLC1Content.Items.EquipmentMagazineVoid);
                            }
                            else {
                                max = hasLysate ? cap.lysateCap : cap.cap;
                            }

                            foreach (MinionOwnership minion in minions) {
                                total += 1;
                                if (total > max) minion.GetComponent<CharacterMaster>().TrueKill();
                            }
                        }
                    }
                }
            }
        }
    }
}