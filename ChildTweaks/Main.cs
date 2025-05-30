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
    private GameObject sparkProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Child/ChildTrackingSparkBall.prefab").WaitForCompletion();
    private GameObject sparkProjectileGhost = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Child/ChildTrackingSparkBallGhost.prefab").WaitForCompletion();
    private SpawnCard spawnCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC2/Child/cscChild.asset").WaitForCompletion();
    private GameObject childMaster = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Child/ChildMaster.prefab").WaitForCompletion();
    public LoopSoundDef lsdSparkProjectile = ScriptableObject.CreateInstance<LoopSoundDef>();
    public static GameObject teleportVFX = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Child/FrolicTeleportVFX.prefab").WaitForCompletion();
    private GameObject childBody = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Child/ChildBody.prefab").WaitForCompletion();
    public static Material destealthMat = Addressables.LoadAssetAsync<Material>("RoR2/Base/Parent/matParentDissolve.mat").WaitForCompletion();
    private SkillDef skillDef = ScriptableObject.CreateInstance<SkillDef>();
    public static GameObject meleeEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Child/MuzzleflashFrolic.prefab").WaitForCompletion();

    public void Awake()
    {
      Instance = this;

      Log.Init(Logger);

      CreateSkill();

      ContentAddition.AddEntityState<ChildBlink>(out _);

      // Reduces projectile spawn point distance
      childBody.transform.GetChild(0).GetChild(0).GetChild(1).GetChild(2).position = new Vector3(0, 2.5f, 0);

      lsdSparkProjectile.startSoundName = "Play_spark_projectile_loop";
      lsdSparkProjectile.stopSoundName = "Stop_spark_projectile_loop";

      sparkProjectile.GetComponent<ProjectileController>().flightSoundLoop = lsdSparkProjectile;
      Transform scaler = sparkProjectileGhost.transform.GetChild(0);
      Destroy(scaler.GetComponent<ObjectScaleCurve>());
      Destroy(scaler.GetComponent<ObjectScaleCurve>());
      Destroy(scaler.GetComponent<ObjectTransformCurve>());
      ProjectileSimple projectileSimple = sparkProjectile.GetComponent<ProjectileSimple>();
      projectileSimple.enableVelocityOverLifetime = false;

      GameObject.Destroy(childBody.GetComponent<ChildMonsterController>());
      GameObject.Destroy(childBody.GetComponent<SetStateOnHurt>());

      spawnCard.directorCreditCost = 20; // 35

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
          skillDriver.minDistance = 15f; // 25 orig
        }
        if (skillDriver.customName == "PathFromAfar")
        {
          skillDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
          skillDriver.minDistance = 25f; // 35 orig
          skillDriver.shouldSprint = true;
        }
      }
      SetAddressableEntityStateField("RoR2/DLC2/Child/EntityStates.ChildMonster.FireTrackingSparkBall.asset", "bombDamageCoefficient", "2");
      // bombDamageCoefficient 6
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

    private void CreateSkill()
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

      GenericSkill skill = childBody.AddComponent<GenericSkill>();
      skill._skillFamily = newFamily;
      childBody.GetComponent<SkillLocator>().secondary = skill;

      ContentAddition.AddSkillFamily(newFamily);
      ContentAddition.AddSkillDef(skillDef);
    }
  }
}