using BepInEx;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Audio;
using RoR2.CharacterAI;
using RoR2.ContentManagement;
using RoR2.Projectile;
using RoR2.Skills;
using RoR2BepInExPack.GameAssetPathsBetter;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ChildTweaks;

[BepInPlugin(PluginGUID, PluginAuthor, PluginVersion)]
public class Main : BaseUnityPlugin
{
  public const string PluginGUID = "Nuxlar.ChildTweaks";
  public const string PluginAuthor = "Nuxlar";
  public const string PluginName = "ChildTweaks";
  public const string PluginVersion = "1.2.1";
  public LoopSoundDef lsdSparkProjectile = ScriptableObject.CreateInstance<LoopSoundDef>();
  public static GameObject teleportVFX;
  public static Material destealthMat;
  private SkillDef skillDef = ScriptableObject.CreateInstance<SkillDef>();
  public static GameObject meleeEffectPrefab;

  internal static Main Instance { get; private set; }

  public static string PluginDirectory { get; private set; }

  public void Awake()
  {
    Main.Instance = this;

    Log.Init(this.Logger);

    this.lsdSparkProjectile.startSoundName = "Play_spark_projectile_loop";
    this.lsdSparkProjectile.stopSoundName = "Stop_spark_projectile_loop";
    this.LoadAssets();
    this.TweakProjectile();
    this.TweakProjectileGhost();
    this.TweakSpawnCard();
    this.TweakSkillDrivers();
    this.TweakBody();
    this.TweakEntityState();

    ContentAddition.AddEntityState<ChildBlink>(out bool _);
  }

  private void CreateSkill(GameObject body)
  {
    this.skillDef.skillName = "ChildBlink";
    this.skillDef.skillNameToken = "ChildBlink";
    this.skillDef.activationState = new SerializableEntityStateType(typeof(ChildBlink));
    this.skillDef.activationStateMachineName = "Body";
    this.skillDef.interruptPriority = InterruptPriority.Skill;
    this.skillDef.baseMaxStock = 1;
    this.skillDef.baseRechargeInterval = 8f;
    this.skillDef.rechargeStock = 1;
    this.skillDef.requiredStock = 1;
    this.skillDef.stockToConsume = 1;
    this.skillDef.dontAllowPastMaxStocks = true;
    this.skillDef.beginSkillCooldownOnSkillEnd = true;
    this.skillDef.canceledFromSprinting = false;
    this.skillDef.forceSprintDuringState = false;
    this.skillDef.fullRestockOnAssign = true;
    this.skillDef.resetCooldownTimerOnUse = true;
    this.skillDef.isCombatSkill = false;
    this.skillDef.mustKeyPress = false;
    this.skillDef.cancelSprintingOnActivation = true;
    SkillFamily instance = ScriptableObject.CreateInstance<SkillFamily>();
    // instance.name = "ChildSecondaryAltFamily";
    instance.variants = new SkillFamily.Variant[1]
    {
      new SkillFamily.Variant() { skillDef = this.skillDef }
    };
    GenericSkill genericSkill = body.AddComponent<GenericSkill>();
    genericSkill._skillFamily = instance;
    body.GetComponent<SkillLocator>().secondary = genericSkill;
    ContentAddition.AddSkillFamily(instance);
    ContentAddition.AddSkillDef(this.skillDef);
  }

  private void TweakProjectile()
  {
    AssetAsyncReferenceManager<GameObject>.LoadAsset(new AssetReferenceT<GameObject>(RoR2_DLC2_Child.ChildTrackingSparkBall_prefab)).Completed += (Action<AsyncOperationHandle<GameObject>>)(x =>
    {
      GameObject result = x.Result;
      result.GetComponent<ProjectileController>().flightSoundLoop = this.lsdSparkProjectile;
      ProjectileSimple component = result.GetComponent<ProjectileSimple>();
      component.enableVelocityOverLifetime = false;
      component.desiredForwardSpeed = 18f;
    });
  }

  private void TweakProjectileGhost()
  {
    AssetAsyncReferenceManager<GameObject>.LoadAsset(new AssetReferenceT<GameObject>(RoR2_DLC2_Child.ChildTrackingSparkBallGhost_prefab)).Completed += (Action<AsyncOperationHandle<GameObject>>)(x =>
    {
      Transform child = x.Result.transform.GetChild(0);
      foreach (UnityEngine.Object @object in ((IEnumerable<ObjectScaleCurve>)child.GetComponents<ObjectScaleCurve>()).ToList<ObjectScaleCurve>())
        UnityEngine.Object.Destroy(@object);
      UnityEngine.Object.Destroy(child.GetComponent<ObjectTransformCurve>());
      child.localScale = new Vector3(1.5f, 1.5f, 1.5f);
    });
  }

  private void TweakSpawnCard()
  {
    AssetAsyncReferenceManager<SpawnCard>.LoadAsset(new AssetReferenceT<SpawnCard>(RoR2_DLC2_Child.cscChild_asset)).Completed += (Action<AsyncOperationHandle<SpawnCard>>)(x => x.Result.directorCreditCost = 25);
  }

  private void TweakBody()
  {
    AssetAsyncReferenceManager<GameObject>.LoadAsset(new AssetReferenceT<GameObject>(RoR2_DLC2_Child.ChildBody_prefab)).Completed += (Action<AsyncOperationHandle<GameObject>>)(x =>
    {
      GameObject result = x.Result;
      result.transform.GetChild(0).GetChild(0).GetChild(1).GetChild(2).position = new Vector3(0.0f, 2f, 0.0f);
      UnityEngine.Object.Destroy(result.GetComponent<ChildMonsterController>());
      result.GetComponent<SetStateOnHurt>().hurtState = new SerializableEntityStateType(typeof(HurtState));
      this.CreateSkill(result);
    });
  }

  private void TweakSkillDrivers()
  {
    AssetAsyncReferenceManager<GameObject>.LoadAsset(new AssetReferenceT<GameObject>(RoR2_DLC2_Child.ChildMaster_prefab)).Completed += (Action<AsyncOperationHandle<GameObject>>)(x =>
    {
      foreach (AISkillDriver component in x.Result.GetComponents<AISkillDriver>())
      {
        if (component.customName == "Frolic")
          component.requiredSkill = null;
        if (component.customName == "RunAway")
          component.maxDistance = 25f;
        if (component.customName == "FireSparkBall")
        {
          component.maxDistance = 40f;
          component.minDistance = 20f;
        }
        if (component.customName == "PathFromAfar")
        {
          component.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
          component.minDistance = 30f;
          component.shouldSprint = true;
        }
      }
    });
  }

  private void TweakEntityState()
  {
    AssetAsyncReferenceManager<EntityStateConfiguration>.LoadAsset(new AssetReferenceT<EntityStateConfiguration>(RoR2_DLC2_Child.EntityStates_ChildMonster_FireTrackingSparkBall_asset)).Completed += (Action<AsyncOperationHandle<EntityStateConfiguration>>)(x =>
    {
      EntityStateConfiguration result = x.Result;
      for (int index = 0; index < result.serializedFieldsCollection.serializedFields.Length; ++index)
      {
        if (result.serializedFieldsCollection.serializedFields[index].fieldName == "bombDamageCoefficient")
          result.serializedFieldsCollection.serializedFields[index].fieldValue.stringValue = "3";
      }
    });
  }

  private void LoadAssets()
  {
    AsyncOperationHandle<GameObject> asyncOperationHandle = AssetAsyncReferenceManager<GameObject>.LoadAsset(new AssetReferenceT<GameObject>(RoR2_DLC2_Child.FrolicTeleportVFX_prefab));
    asyncOperationHandle.Completed += x => Main.teleportVFX = x.Result;
    AssetAsyncReferenceManager<Material>.LoadAsset(new AssetReferenceT<Material>(RoR2_Base_Parent.matParentDissolve_mat)).Completed += x => Main.destealthMat = x.Result;
    asyncOperationHandle = AssetAsyncReferenceManager<GameObject>.LoadAsset(new AssetReferenceT<GameObject>(RoR2_DLC2_Child.MuzzleflashFrolic_prefab));
    asyncOperationHandle.Completed += x => Main.meleeEffectPrefab = x.Result;
  }
}
