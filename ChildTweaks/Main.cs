using BepInEx;
using EntityStates;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Navigation;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using R2API;
using RoR2.Skills;
using RoR2.Projectile;
using EntityStates.ChildMonster;
using RoR2.Audio;

namespace ChildTweaks
{
  [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
  public class Main : BaseUnityPlugin
  {
    public const string PluginGUID = PluginAuthor + "." + PluginName;
    public const string PluginAuthor = "Nuxlar";
    public const string PluginName = "ChildTweaks";
    public const string PluginVersion = "1.1.0";

    internal static Main Instance { get; private set; }
    public static string PluginDirectory { get; private set; }
    private const BindingFlags allFlags = (BindingFlags)(-1);
    private GameObject sparkProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Child/ChildTrackingSparkBall.prefab").WaitForCompletion();
    private GameObject sparkProjectileGhost = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Child/ChildTrackingSparkBallGhost.prefab").WaitForCompletion();
    private SpawnCard spawnCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC2/Child/cscChild.asset").WaitForCompletion();
    private GameObject childMaster = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Child/ChildMaster.prefab").WaitForCompletion();
    public LoopSoundDef lsdSparkProjectile = ScriptableObject.CreateInstance<LoopSoundDef>();
    // RoR2/DLC2/Child/MuzzleflashFrolic.prefab
    // RoR2/DLC2/Child/FrolicTeleportVFX.prefab
    // RoR2/DLC2/Child/FrolicProjectileImpactVFX.prefab
    private SkillDef frolicSkill = Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC2/Child/ChildBodyFrolic.asset").WaitForCompletion();
    public static GameObject meleeEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Child/MuzzleflashFrolic.prefab").WaitForCompletion();
    public void Awake()
    {
      Instance = this;

      Stopwatch stopwatch = Stopwatch.StartNew();

      Log.Init(Logger);

      ContentAddition.AddEntityState<FrolicAttack>(out _);

      frolicSkill.activationState = new SerializableEntityStateType(typeof(FrolicAttack));

      lsdSparkProjectile.startSoundName = "Play_spark_projectile_loop";
      lsdSparkProjectile.stopSoundName = "Stop_spark_projectile_loop";

      sparkProjectile.GetComponent<ProjectileController>().flightSoundLoop = lsdSparkProjectile;
      Transform scaler = sparkProjectileGhost.transform.GetChild(0);
      Destroy(scaler.GetComponent<ObjectScaleCurve>());
      Destroy(scaler.GetComponent<ObjectScaleCurve>());
      Destroy(scaler.GetComponent<ObjectTransformCurve>());

      // Child Credit Cost 35
      // Lemurian Credit Cost 11
      // Stop_spark_projectile_loop
      // Play_spark_projectile_loop

      /*
      4 skill drivers total
      1 "Frolic" secondary requiredSkill

      2 "RunAway"
      3 "FireSparkBall" primary
      4 "PathFromAfar"

      Projectile Info
      ChildTrackingSparkBall
      ProjectileSimple enableVelocityOverLifetime = false updateAfterFiring = false (maybe) desiredForwardSpeed = 17 (current)
      ProjectileSteerTowardsTarget rotationSpeed = 90 (current)

      ProjectileGhost
      GetChild(0) or MoonMesh
      ObjectScaleCurve x2
      ObjectTransformCurve

      ChildMonsterController

      Play_child_attack1_chargeUp


      */
      AISkillDriver[] skillDrivers = childMaster.GetComponents<AISkillDriver>();
      foreach (AISkillDriver skillDriver in skillDrivers)
      {
        if (skillDriver.customName == "RunAway")
        {
          skillDriver.maxDistance = 20f; // 30 orig
          skillDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
        }
        if (skillDriver.customName == "FireSparkBall")
        {
          skillDriver.maxDistance = 50f; // 37 orig
          skillDriver.minDistance = 15f; // 25 orig
        }
        if (skillDriver.customName == "PathFromAfar")
        {
          skillDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
          skillDriver.minDistance = 0f; // 35 orig
          skillDriver.shouldSprint = true;
        }
      }
      SetAddressableEntityStateField("RoR2/DLC2/Child/EntityStates.ChildMonster.FireTrackingSparkBall.asset", "bombDamageCoefficient", "3");
      // bombDamageCoefficient 6

      stopwatch.Stop();
      Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    }

    public static bool SetAddressableEntityStateField(string fullEntityStatePath, string fieldName, string value)
    {
      EntityStateConfiguration esc = Addressables.LoadAssetAsync<EntityStateConfiguration>(fullEntityStatePath).WaitForCompletion();
      for (int i = 0; i < esc.serializedFieldsCollection.serializedFields.Length; i++)
      {
        if (esc.serializedFieldsCollection.serializedFields[i].fieldName == fieldName)
        {
          esc.serializedFieldsCollection.serializedFields[i].fieldValue.stringValue = value;
          return true;
        }
      }
      return false;
    }

    // self is just for being able to call self.OnEnter() inside hooks.
    private static void BaseStateOnEnterCaller(BaseState self)
    {

    }

    // self is just for being able to call self.OnEnter() inside hooks.
    private static void BaseStateOnEnterCallerMethodModifier(ILContext il)
    {
      var cursor = new ILCursor(il);
      cursor.Emit(OpCodes.Ldarg_0);
      cursor.Emit(OpCodes.Call, typeof(BaseState).GetMethod(nameof(BaseState.OnEnter), allFlags));
    }
  }
}