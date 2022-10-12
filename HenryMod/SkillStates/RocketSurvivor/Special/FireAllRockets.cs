﻿using EntityStates.RocketSurvivorSkills.Primary;
using RocketSurvivor.Modules.Survivors;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EntityStates.RocketSurvivorSkills.Special
{
    public class FireAllRockets : BaseState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            fireStopwatch = 0f;
            delayBetweenShots = FireAllRockets.baseDelayBetweenShots / base.attackSpeedStat;
            shotsRemaining = FireAllRockets.baseShotCount;    //Skill felt underwhelming when it was tied to primary stocks: only useful when primary is fully loaded, which is the opposite of the skill's intended purpose.
            isCrit = base.RollCrit();

            selectedPrimarySkill = RocketSurvivorSetup.FireRocketSkillDef;
            if (base.skillLocator)
            {
                if (base.skillLocator.primary.skillDef == RocketSurvivorSetup.FireRocketAltSkillDef)
                {
                    selectedPrimarySkill = RocketSurvivorSetup.FireRocketAltSkillDef;
                }
            }

            ModifyStats();
        }

        public virtual void ModifyStats() { }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (fireStopwatch <= 0f)
            {
                fireStopwatch += delayBetweenShots;
                if(shotsRemaining > 0) FireProjectile();
            }
            fireStopwatch -= Time.fixedDeltaTime;

            if (base.isAuthority)
            {
                if (base.skillLocator)
                {
                    if (shotsRemaining <= 0)//(base.skillLocator.primary.stock <= 0)
                    {
                        this.outer.SetNextState(new Rearm());
                        return;
                    }
                }
            }
        }

        public void FireProjectile()
        {
            if (base.isAuthority)
            {
                /*if (base.skillLocator && base.skillLocator.primary.stock > 0)
                {
                    base.skillLocator.primary.DeductStock(1);
                }*/

                Ray aimRay = base.GetAimRay();

                Vector3 aimDirection = aimRay.direction;


                float damageMult = RocketSurvivor.RocketSurvivorPlugin.GetICBMDamageMult(base.characterBody);

                //Copied from Bandit2
                if (RocketSurvivor.RocketSurvivorPlugin.pocketICBM && base.characterBody && base.characterBody.inventory && base.characterBody.inventory.GetItemCount(DLC1Content.Items.MoreMissile) > 0)
                {
                    Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                    Vector3 axis = Vector3.Cross(aimRay.direction, rhs);

                    float currentSpread = 0f;
                    float angle = 0f;
                    float num2 = 0f;
                    num2 = UnityEngine.Random.Range(1f + currentSpread, 1f + currentSpread) * 3f;   //Bandit is x2
                    angle = num2 / 2f;  //3 - 1 rockets

                    Vector3 direction = Quaternion.AngleAxis(-num2 * 0.5f, axis) * aimRay.direction;
                    Quaternion rotation = Quaternion.AngleAxis(angle, axis);
                    Ray aimRay2 = new Ray(aimRay.origin, direction);
                    for (int i = 0; i < 3; i++)
                    {
                        aimDirection = aimRay2.direction;
                        if (shotsRemaining != baseShotCount)
                        {
                            aimDirection = Util.ApplySpread(aimRay2.direction, 0f, 3f, 1f, 1f);
                        }

                        ProjectileManager.instance.FireProjectile(GetProjectilePrefab(), aimRay2.origin, Util.QuaternionSafeLookRotation(aimDirection), base.gameObject, this.damageStat * GetDamageCoefficient(), GetForce() * 0.25f, isCrit, DamageColorIndex.Default, null, -1f);
                        aimRay2.direction = rotation * aimRay2.direction;
                    }
                }
                else
                {
                    if (shotsRemaining != baseShotCount)
                    {
                        aimDirection = Util.ApplySpread(aimRay.direction, 0f, 3f, 1f, 1f);
                    }
                    ProjectileManager.instance.FireProjectile(GetProjectilePrefab(), aimRay.origin, Util.QuaternionSafeLookRotation(aimDirection), base.gameObject, this.damageStat * GetDamageCoefficient(), GetForce() * 0.25f, isCrit, DamageColorIndex.Default, null, -1f);
                }


                base.StartAimMode(aimRay, 3f, false);
            }

            if (FireRocket.effectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(GetEffectPrefab(), base.gameObject, GetMuzzleString(), false);
            }
            base.PlayAnimation("Gesture, Additive", "Shoot", "Shoot.playbackRate", 0.169f);
            Util.PlaySound(FireRocket.attackSoundString, base.gameObject);
            shotsRemaining--;
        }

        private GameObject GetProjectilePrefab()
        {
            if (selectedPrimarySkill == RocketSurvivorSetup.FireRocketAltSkillDef)
            {
                return FireRocketAlt.projectilePrefab;
            }
            else
            {
                return FireRocket.projectilePrefab;
            }
        }

        private GameObject GetEffectPrefab()
        {
            if (selectedPrimarySkill == RocketSurvivorSetup.FireRocketAltSkillDef)
            {
                return FireRocketAlt.effectPrefab;
            }
            else
            {
                return FireRocket.effectPrefab;
            }
        }

        private string GetMuzzleString()
        {
            if (selectedPrimarySkill == RocketSurvivorSetup.FireRocketAltSkillDef)
            {
                return FireRocketAlt.muzzleString;
            }
            else
            {
                return FireRocket.muzzleString;
            }
        }


        private float GetDamageCoefficient()
        {
            if (selectedPrimarySkill == RocketSurvivorSetup.FireRocketAltSkillDef)
            {
                return FireRocketAlt.damageCoefficient;
            }
            else
            {
                return FireRocket.damageCoefficient;
            }
        }

        private float GetForce()
        {
            if (selectedPrimarySkill == RocketSurvivorSetup.FireRocketAltSkillDef)
            {
                return FireRocketAlt.force;
            }
            else
            {
                return FireRocket.force;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Pain;
        }

        private SkillDef selectedPrimarySkill;
        private bool isCrit;
        private float fireStopwatch;

        public float delayBetweenShots;
        public int shotsRemaining;

        public static int baseShotCount = 4;
        public static float baseDelayBetweenShots = 0.2f;
    }
}
