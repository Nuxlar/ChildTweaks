using BepInEx;
using EntityStates;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using R2API;
using RoR2.Skills;
using RoR2.Projectile;
using RoR2.Audio;
using RoR2.ContentManagement;

namespace ChildTweaks
{
  [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
  public class Main : BaseUnityPlugin
  {
    public const string PluginGUID = PluginAuthor + "." + PluginName;
    public const string PluginAuthor = "Nuxlar";
    public const string PluginName = "ChildTweaks";
    public const string PluginVersion = "1.1.3";

    internal static Main Instance { get; private set; }
    public static string PluginDirectory { get; private set; }
    public LoopSoundDef lsdSparkProjectile = ScriptableObject.CreateInstance<LoopSoundDef>();
    public static GameObject teleportVFX;
    public static Material destealthMat;
    private SkillDef skillDef = ScriptableObject.CreateInstance<SkillDef>();
    public static GameObject meleeEffectPrefab;

    public void Awake()
    {
      Instance = this;

      Log.Init(Logger);

      lsdSparkProjectile.startSoundName = "Play_spark_projectile_loop";
      lsdSparkProjectile.stopSoundName = "Stop_spark_projectile_loop";

      LoadAssets();
      TweakProjectile();
      TweakProjectileGhost();
      TweakSpawnCard();
      TweakSkillDrivers();
      TweakBody();
      TweakEntityState();

      ContentAddition.AddEntityState<ChildBlink>(out _);
    }

    private void CreateSkill(GameObject body)
    {
      skillDef.skillName = "ChildBlink";
      (skillDef as ScriptableObject).name = "ChildBlink";

      skillDef.activationState = new SerializableEntityStateType(typeof(ChildBlink));
      skillDef.activationStateMachineName = "Body";
      skillDef.interruptPriority = InterruptPriority.Frozen;

      skillDef.baseMaxStock = 1;
      skillDef.baseRechargeInterval = 8f;

      skillDef.rechargeStock = 1;
      skillDef.requiredStock = 1;
      skillDef.stockToConsume = 1;

      skillDef.dontAllowPastMaxStocks = true;
      skillDef.beginSkillCooldownOnSkillEnd = false;
      skillDef.canceledFromSprinting = false;
      skillDef.forceSprintDuringState = true;
      skillDef.fullRestockOnAssign = true;
      skillDef.resetCooldownTimerOnUse = true;
      skillDef.isCombatSkill = false;
      skillDef.mustKeyPress = false;
      skillDef.cancelSprintingOnActivation = false;

      SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
      (newFamily as ScriptableObject).name = "ChildSecondaryAltFamily";
      ;
      newFamily.variants = new SkillFamily.Variant[1] { new SkillFamily.Variant { skillDef = skillDef } };

      GenericSkill skill = body.AddComponent<GenericSkill>();
      skill._skillFamily = newFamily;
      body.GetComponent<SkillLocator>().secondary = skill;

      ContentAddition.AddSkillFamily(newFamily);
      ContentAddition.AddSkillDef(skillDef);
    }

    private void TweakProjectile()
    {
      AssetReferenceT<GameObject> projectileRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC2_Child.ChildTrackingSparkBall_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(projectileRef).Completed += (x) =>
      {
        GameObject sparkProjectile = x.Result;
        sparkProjectile.GetComponent<ProjectileController>().flightSoundLoop = lsdSparkProjectile;
        ProjectileSimple projectileSimple = sparkProjectile.GetComponent<ProjectileSimple>();
        projectileSimple.enableVelocityOverLifetime = false;
      };
    }

    private void TweakProjectileGhost()
    {
      AssetReferenceT<GameObject> ghostRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC2_Child.ChildTrackingSparkBallGhost_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(ghostRef).Completed += (x) =>
      {
        GameObject sparkProjectileGhost = x.Result;
        Transform scaler = sparkProjectileGhost.transform.GetChild(0);
        Destroy(scaler.GetComponent<ObjectScaleCurve>());
        Destroy(scaler.GetComponent<ObjectScaleCurve>());
        Destroy(scaler.GetComponent<ObjectTransformCurve>());
      };
    }

    private void TweakSpawnCard()
    {
      AssetReferenceT<SpawnCard> cardRef = new AssetReferenceT<SpawnCard>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC2_Child.cscChild_asset);
      AssetAsyncReferenceManager<SpawnCard>.LoadAsset(cardRef).Completed += (x) =>
      {
        SpawnCard spawnCard = x.Result;
        spawnCard.directorCreditCost = 25; // 35
      };
    }

    private void TweakBody()
    {
      AssetReferenceT<GameObject> bodyRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC2_Child.ChildBody_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(bodyRef).Completed += (x) =>
      {
        GameObject childBody = x.Result;
        // Reduces projectile spawn point distance
        childBody.transform.GetChild(0).GetChild(0).GetChild(1).GetChild(2).position = new Vector3(0, 2.5f, 0);

        GameObject.Destroy(childBody.GetComponent<ChildMonsterController>());
        childBody.GetComponent<SetStateOnHurt>().hurtState = new SerializableEntityStateType(typeof(HurtState));

        CreateSkill(childBody);
      };
    }

    private void TweakSkillDrivers()
    {
      AssetReferenceT<GameObject> masterRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC2_Child.ChildMaster_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(masterRef).Completed += (x) =>
      {
        GameObject childMaster = x.Result;
        AISkillDriver[] skillDrivers = childMaster.GetComponents<AISkillDriver>();
        foreach (AISkillDriver skillDriver in skillDrivers)
        {
          if (skillDriver.customName == "Frolic")
          {
            skillDriver.movementType = AISkillDriver.MovementType.FleeMoveTarget;
            skillDriver.aimType = AISkillDriver.AimType.MoveDirection;
            skillDriver.shouldSprint = true;
            skillDriver.requiredSkill = null;
          }
          if (skillDriver.customName == "RunAway")
          {
            skillDriver.maxDistance = 20f; // 30 orig
          }
          if (skillDriver.customName == "FireSparkBall")
          {
            skillDriver.maxDistance = 45; // 37 orig
            skillDriver.minDistance = 20f; // 25 orig
          }
          if (skillDriver.customName == "PathFromAfar")
          {
            skillDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            skillDriver.minDistance = 25f; // 35 orig
            skillDriver.shouldSprint = true;
          }
        }
      };
    }

    private void TweakEntityState()
    {
      AssetReferenceT<EntityStateConfiguration> escRef = new AssetReferenceT<EntityStateConfiguration>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC2_Child_EntityStates_ChildMonster.FireTrackingSparkBall_asset);
      AssetAsyncReferenceManager<EntityStateConfiguration>.LoadAsset(escRef).Completed += (x) =>
      {
        EntityStateConfiguration esc = x.Result;
        for (int i = 0; i < esc.serializedFieldsCollection.serializedFields.Length; i++)
        {
          if (esc.serializedFieldsCollection.serializedFields[i].fieldName == "bombDamageCoefficient")
          {
            esc.serializedFieldsCollection.serializedFields[i].fieldValue.stringValue = "2";  // orig 6
          }
        }
      };
    }

    private void LoadAssets()
    {
      AssetReferenceT<GameObject> vfxRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC2_Child.FrolicTeleportVFX_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(vfxRef).Completed += (x) => teleportVFX = x.Result;
      AssetReferenceT<Material> matRef = new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Parent.matParentDissolve_mat);
      AssetAsyncReferenceManager<Material>.LoadAsset(matRef).Completed += (x) => destealthMat = x.Result;
      AssetReferenceT<GameObject> effectRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC2_Child.MuzzleflashFrolic_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(effectRef).Completed += (x) => meleeEffectPrefab = x.Result;
    }

  }
}
