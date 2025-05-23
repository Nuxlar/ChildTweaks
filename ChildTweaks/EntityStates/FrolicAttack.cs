using RoR2;
using RoR2.CharacterAI;
using RoR2.Navigation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EntityStates;
using EntityStates.ChildMonster;

namespace ChildTweaks;

public class FrolicAttack : BasicMeleeAttack
{
    public static float baseDuration = 2f;
    public static float damageCoefficient = 4f;
    public static float force = 20f;
    public static string attackString;

    public override void OnEnter()
    {
        base.OnEnter();
        this.hitBoxGroupName = "EnergyBall";
        this.swingEffectPrefab = Main.meleeEffectPrefab;
        this.swingEffectMuzzleString = "HitBox";
        // hitboxgroup name EnergyBall
        // swingeffectmuzzlestring
        Util.PlaySound(Frolic.attackString, this.gameObject);
        this.duration = Frolic.baseDuration / this.attackSpeedStat;
        // this.PlayAnimation("Gesture, Override", "FrolicEnter", "FrolicEnter.playbackRate", 1f);
    }
    public override void PlayAnimation()
    {
        // FrolicFire
        this.PlayAnimation("Gesture, Override", "FrolicFire", "FrolicEnter.playbackRate", 0.5f);
    }

    public override InterruptPriority GetMinimumInterruptPriority()
    {
        return InterruptPriority.Frozen;
    }

    public void FireTPEffect()
    {
        Vector3 position = this.FindModelChild("Chest").transform.position;
        EffectManager.SpawnEffect(Frolic.tpEffectPrefab, new EffectData()
        {
            origin = position,
            scale = 1f
        }, true);
        Util.PlaySound("Play_child_attack2_reappear", this.gameObject);
    }
}
