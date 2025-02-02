﻿using UnityEngine;
using RoR2;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;
using RoR2.Projectile;
using RocketSurvivor.Components.Projectile;
using R2API;
using RocketSurvivor.Modules.Survivors;

namespace RocketSurvivor.Components {
    public class RocketTrackerComponent : NetworkBehaviour
    {
        private List<RocketInfo> rocketList;
        private List<RocketInfo> c4List;
        private SkillLocator skillLocator;

        public static NetworkSoundEventDef detonateSuccess;
        public static NetworkSoundEventDef detonateFail;

        [SyncVar]
        private bool _rocketAvailable = false;

        public void Awake()
        {
            c4List = new List<RocketInfo>();
            rocketList = new List<RocketInfo>();
            skillLocator = base.GetComponent<SkillLocator>();
        }

        public void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                UpdateRocketAvailable();
            }
        }

        public void OnDestroy()
        {
            if (NetworkServer.active && c4List != null)
            {
                foreach (RocketInfo ri in c4List)
                {
                    if (ri.gameObject) Destroy(ri.gameObject);
                }
            }
        }

        [Server]
        private void UpdateRocketAvailable()
        {
            if (!NetworkServer.active) return;
            bool newRocketAvailable = false;

            c4List.RemoveAll(item => item.gameObject == null);
            rocketList.RemoveAll(item => item.gameObject == null);
            if ((rocketList.Count + c4List.Count) > 0)
            {
                newRocketAvailable = true;
            }

            if (newRocketAvailable != _rocketAvailable) _rocketAvailable = newRocketAvailable;
        }

        public bool IsRocketAvailable()
        {
            return _rocketAvailable;
        }

        public void AddRocket(GameObject rocket, bool applyAirDetBuff, bool isC4 = false)
        {
            RocketInfo info = new RocketInfo(rocket, applyAirDetBuff, isC4);
            if (info.isC4)
            {
                int c4InList = c4List.Count;
                int maxC4 = 1;
                if (this.skillLocator && this.skillLocator.utility)
                {
                    maxC4 = Mathf.Max(maxC4, this.skillLocator.utility.maxStock);
                }
                if (c4InList >= maxC4)
                {
                    RocketInfo oldestC4 = c4List.FirstOrDefault<RocketInfo>();
                    c4List.Remove(oldestC4);
                    DetonateRocketInfo(oldestC4);
                }
                c4List.Add(info);
            }
            else
            {
                rocketList.Add(info);
            }
        }

        private bool DetonateRocketInfo(RocketInfo info)
        {
            bool detonatedSuccessfully = false;
            GameObject toDetonate = info.gameObject;
            ProjectileDamage pd = toDetonate.GetComponent<ProjectileDamage>();
            ProjectileController pc = toDetonate.GetComponent<ProjectileController>();
            ProjectileImpactExplosion pie = toDetonate.GetComponent<ProjectileImpactExplosion>();
            BlastJumpComponent bjc = toDetonate.GetComponent<BlastJumpComponent>();
            TeamFilter tf = toDetonate.GetComponent<TeamFilter>();

            if (pc && pie)
            {
                //Handle self-knockback first
                if (bjc)
                {
                    if (info.applyAirDetBonus)
                    {
                        bjc.aoe *= EntityStates.RocketSurvivorSkills.Secondary.AirDet.radiusMult;
                        bjc.force *= EntityStates.RocketSurvivorSkills.Secondary.AirDet.forceMult;
                    }
                    bjc.BlastJump();
                }

                //Handle blastattack second
                if (tf && pd && pc.owner)
                {
                    BlastAttack ba = new BlastAttack
                    {
                        attacker = pc.owner,
                        attackerFiltering = AttackerFiltering.NeverHitSelf,
                        baseDamage = pd.damage * pie.blastDamageCoefficient,
                        baseForce = pd.force,
                        bonusForce = pie.bonusBlastForce,
                        canRejectForce = pie.canRejectForce,
                        crit = pd.crit,
                        damageColorIndex = (!info.isC4 && pd.damageColorIndex == DamageColorIndex.Default) ? DamageColorIndex.WeakPoint : pd.damageColorIndex,
                        damageType = pd.damageType,
                        falloffModel = BlastAttack.FalloffModel.None,
                        inflictor = pc.owner,
                        position = toDetonate.transform.position,
                        procChainMask = default,
                        procCoefficient = pie.blastProcCoefficient,
                        radius = pie.blastRadius,
                        teamIndex = tf.teamIndex
                    };

                    if (info.applyAirDetBonus)
                    {
                        ba.baseForce *= EntityStates.RocketSurvivorSkills.Secondary.AirDet.forceMult;
                        ba.bonusForce *= EntityStates.RocketSurvivorSkills.Secondary.AirDet.forceMult;
                        ba.baseDamage *= EntityStates.RocketSurvivorSkills.Secondary.AirDet.damageMult;
                        ba.radius = Mathf.Max(ba.radius * EntityStates.RocketSurvivorSkills.Secondary.AirDet.radiusMult, EntityStates.RocketSurvivorSkills.Secondary.AirDet.minRadius);
                    }

                    DamageAPI.ModdedDamageTypeHolderComponent mdc = toDetonate.GetComponent<DamageAPI.ModdedDamageTypeHolderComponent>();
                    if (mdc)
                    {
                        if (mdc.Has(DamageTypes.ScaleForceToMass)) ba.AddModdedDamageType(DamageTypes.ScaleForceToMass);
                        if (mdc.Has(DamageTypes.AirborneBonus)) ba.AddModdedDamageType(DamageTypes.AirborneBonus);
                    }

                    GameObject effectPrefab = EntityStates.RocketSurvivorSkills.Secondary.AirDet.explosionEffectPrefab;
                    if (pd.damageType.HasFlag(DamageType.Silent) && pd.damageType.HasFlag(DamageType.Stun1s))
                    {
                        effectPrefab = EntityStates.RocketSurvivorSkills.Secondary.AirDet.concExplosionEffectPrefab;
                    }

                    EffectManager.SpawnEffect(effectPrefab, new EffectData { origin = toDetonate.transform.position, scale = ba.radius }, true);

                    ba.Fire();
                    detonatedSuccessfully = true;
                }
            }
            Destroy(toDetonate);
            return detonatedSuccessfully;
        }

        [Server]
        public bool DetonateRocket()
        {
            bool detonatedSuccessfully = false;
            if (NetworkServer.active)
            {
                if (this.IsRocketAvailable())
                {
                    foreach (RocketInfo info in rocketList)
                    {
                        bool detonate = DetonateRocketInfo(info);
                        detonatedSuccessfully = detonatedSuccessfully || detonate;
                    }

                    foreach (RocketInfo info in c4List)
                    {
                        bool detonate = DetonateRocketInfo(info);
                        detonatedSuccessfully = detonatedSuccessfully || detonate;
                    }
                }
                UpdateRocketAvailable();
            }
            return detonatedSuccessfully;
        }

        [ClientRpc]
        public void RpcAddSecondaryStock()
        {
            if (!this.hasAuthority) return;
            if (skillLocator & skillLocator.secondary.stock < skillLocator.secondary.maxStock)
            {
                skillLocator.secondary.AddOneStock();
            }
        }

        //Is this redundant?
        public void ServerDetonateRocket()
        {
            if (NetworkServer.active)
            {
                bool success = DetonateRocket();
                EffectManager.SimpleSoundEffect(success ? detonateSuccess.index : detonateFail.index, base.transform.position, true); //Moved from AirDet.cs to here
                if (!success)
                {
                    RpcAddSecondaryStock();
                }
            }
        }

        public class RocketInfo
        {
            public GameObject gameObject;
            public bool applyAirDetBonus;
            public bool isC4;

            public RocketInfo(GameObject gameObject, bool applyAirDetBonus, bool isC4 = false)
            {
                this.gameObject = gameObject;
                this.applyAirDetBonus = applyAirDetBonus;
                this.isC4 = isC4;
            }
        }
    }
}
