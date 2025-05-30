using RoR2;
using UnityEngine;
using EntityStates;

namespace ChildTweaks;

public class ChildBlink : BaseState
{
    public static GameObject blinkPrefab = Main.teleportVFX;
    public float duration = 0.3f;
    public float speedCoefficient = 5f; //25
    public float destealthDuration = 0.5f;
    public Material destealtMat = Main.destealthMat;
    public string beginSoundString = "Play_child_attack2_teleport";
    public string endSoundString = "Play_child_attack2_reappear";

    private Transform modelTransform;
    private float stopwatch;
    private Vector3 blinkVector = Vector3.zero;
    private CharacterModel characterModel;
    private HurtBoxGroup hurtboxGroup;

    public override void OnEnter()
    {
        base.OnEnter();
        this.PlayAnimation("Gesture, Override", "FrolicEnter", "FrolicEnter.playbackRate", 1f);
        Util.PlaySound(this.beginSoundString, this.gameObject);
        this.modelTransform = this.GetModelTransform();
        if ((bool)this.modelTransform)
        {
            this.characterModel = this.modelTransform.GetComponent<CharacterModel>();
            this.hurtboxGroup = this.modelTransform.GetComponent<HurtBoxGroup>();
        }
        if ((bool)this.characterModel)
            ++this.characterModel.invisibilityCount;
        if ((bool)this.hurtboxGroup)
            ++this.hurtboxGroup.hurtBoxesDeactivatorCounter;
        this.blinkVector = this.GetBlinkVector();
        this.CreateBlinkEffect(Util.GetCorePosition(this.gameObject));
    }

    protected virtual Vector3 GetBlinkVector() => this.inputBank.aimDirection;

    private void CreateBlinkEffect(Vector3 origin)
    {
        EffectManager.SpawnEffect(ChildBlink.blinkPrefab, new EffectData()
        {
            rotation = Util.QuaternionSafeLookRotation(this.blinkVector),
            origin = origin
        }, false);
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        this.stopwatch += this.GetDeltaTime();
        if ((bool)this.characterMotor && (bool)this.characterDirection)
        {
            this.characterMotor.velocity = Vector3.zero;
            this.characterMotor.rootMotion += this.blinkVector * (this.moveSpeedStat * this.speedCoefficient * this.GetDeltaTime());
        }
        if ((double)this.stopwatch < this.duration || !this.isAuthority)
            return;
        this.outer.SetNextStateToMain();
    }

    public override void OnExit()
    {
        if (!this.outer.destroying)
        {
            Util.PlaySound(this.endSoundString, this.gameObject);
            this.CreateBlinkEffect(Util.GetCorePosition(this.gameObject));
            this.modelTransform = this.GetModelTransform();
            if ((bool)this.modelTransform)
            {
                TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(this.modelTransform.gameObject);
                temporaryOverlayInstance.duration = this.destealthDuration;
                temporaryOverlayInstance.destroyComponentOnEnd = true;
                temporaryOverlayInstance.originalMaterial = this.destealtMat;
                temporaryOverlayInstance.inspectorCharacterModel = this.modelTransform.gameObject.GetComponent<CharacterModel>();
                temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0.0f, 1f, 1f, 0.0f);
                temporaryOverlayInstance.animateShaderAlpha = true;
            }
        }
        if ((bool)this.characterModel)
            --this.characterModel.invisibilityCount;
        if ((bool)this.hurtboxGroup)
            --this.hurtboxGroup.hurtBoxesDeactivatorCounter;
        if ((bool)this.characterMotor)
            this.characterMotor.disableAirControlUntilCollision = false;
        base.OnExit();
    }
}
