﻿using UnityEngine;
using RoR2;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;
using RoR2.Projectile;
using RocketSurvivor.Components.Projectile;
using R2API;
using RocketSurvivor.Modules.Survivors;

namespace RocketSurvivor.Components
{
    public class RocketTrackerComponent : NetworkBehaviour
    {
        private List<GameObject> rocketList;
        private SkillLocator skillLocator;

        public static NetworkSoundEventDef detonateSuccess;
        public static NetworkSoundEventDef detonateFail;

        [SyncVar]
         private bool _rocketAvailable = false;

        public void Awake()
        {
            rocketList = new List<GameObject>();
            skillLocator = base.GetComponent<SkillLocator>();
        }

        public void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                UpdateRocketAvailable();
            }
        }

        [Server]
        private void UpdateRocketAvailable()
        {
            if (!NetworkServer.active) return;
            bool newRocketAvailable = false;

            rocketList.RemoveAll(item => item == null);
            if (rocketList.Count > 0)
            {
                newRocketAvailable = true;
            }

            if (newRocketAvailable != _rocketAvailable) _rocketAvailable = newRocketAvailable;
        }

        public bool IsRocketAvailable()
        {
            return _rocketAvailable;
        }

        public void AddRocket(GameObject rocket)
        {
            rocketList.Add(rocket);
        }

        [Server]
        public bool DetonateRocket()
        {
            bool detonatedSuccessfully = false;
            if (NetworkServer.active)
            {
                if (this.IsRocketAvailable())
                {
                    foreach (GameObject toDetonate in rocketList)
                    {
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
                                bjc.aoe *= EntityStates.RocketSurvivorSkills.Secondary.AirDet.radiusMult;
                                bjc.force *= EntityStates.RocketSurvivorSkills.Secondary.AirDet.forceMult;
                                bjc.BlastJump();
                            }

                            //Handle blastattack second
                            if (tf && pd && pc.owner)
                            {
                                BlastAttack ba = new BlastAttack
                                {
                                    attacker = pc.owner,
                                    attackerFiltering = AttackerFiltering.NeverHitSelf,
                                    baseDamage = pd.damage * pie.blastDamageCoefficient * EntityStates.RocketSurvivorSkills.Secondary.AirDet.damageMult,
                                    baseForce = pd.force * EntityStates.RocketSurvivorSkills.Secondary.AirDet.forceMult,
                                    bonusForce = pie.bonusBlastForce * EntityStates.RocketSurvivorSkills.Secondary.AirDet.forceMult,
                                    canRejectForce = pie.canRejectForce,
                                    crit = pd.crit,
                                    damageColorIndex = pd.damageColorIndex,
                                    damageType = pd.damageType,
                                    falloffModel = BlastAttack.FalloffModel.None,
                                    inflictor = pc.owner,
                                    position = toDetonate.transform.position,
                                    procChainMask = default,
                                    procCoefficient = pie.blastProcCoefficient,
                                    radius = Mathf.Max(pie.blastRadius * EntityStates.RocketSurvivorSkills.Secondary.AirDet.radiusMult, EntityStates.RocketSurvivorSkills.Secondary.AirDet.minRadius),
                                    teamIndex = tf.teamIndex
                                };

                                DamageAPI.ModdedDamageTypeHolderComponent mdc = toDetonate.GetComponent<DamageAPI.ModdedDamageTypeHolderComponent>();
                                if (mdc)
                                {
                                    if (mdc.Has(DamageTypes.ScaleForceToMass)) ba.AddModdedDamageType(DamageTypes.ScaleForceToMass);
                                    if (mdc.Has(DamageTypes.AirborneBonus)) ba.AddModdedDamageType(DamageTypes.AirborneBonus);
                                    if (mdc.Has(DamageTypes.MarkForAirshot)) ba.AddModdedDamageType(DamageTypes.MarkForAirshot);
                                }

                                GameObject effectPrefab = EntityStates.RocketSurvivorSkills.Secondary.AirDet.explosionEffectPrefab;
                                if (pd.damageType.HasFlag(DamageType.Silent) && pd.damageType.HasFlag(DamageType.Stun1s))
                                {
                                    effectPrefab = EntityStates.RocketSurvivorSkills.Secondary.AirDet.concExplosionEffectPrefab;
                                }

                                EffectManager.SpawnEffect(effectPrefab, new EffectData { origin = toDetonate.transform.position, scale = ba.radius }, true);

                                FlakShotgunComponent fsc = toDetonate.GetComponent<FlakShotgunComponent>();
                                if (fsc)
                                {
                                    fsc.FireFlakProjectiles(EntityStates.RocketSurvivorSkills.Secondary.AirDet.damageMult);
                                    effectPrefab = EntityStates.RocketSurvivorSkills.Special.FireFlak.explosionEffectPrefab;
                                }

                                ba.Fire();
                            }
                        }
                        Destroy(toDetonate);
                        detonatedSuccessfully = true;
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

        [ClientRpc]
        public void RpcAddSpecialStock()
        {
            if (!this.hasAuthority) return;
            if (skillLocator & skillLocator.special.stock < skillLocator.special.maxStock)
            {
                skillLocator.special.AddOneStock();
            }
        }

        [Command]
        public void CmdDetonateRocket()
        {
            ServerDetonateRocket();
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
    }
}
