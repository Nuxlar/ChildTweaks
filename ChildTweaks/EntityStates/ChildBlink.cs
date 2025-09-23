using EntityStates;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Navigation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChildTweaks;

public class ChildBlink : BaseState
{
    public static GameObject tpEffect = Main.teleportVFX;
    public float destealthDuration = 1f;
    public float tpDuration = 1f;
    public float fireFrolicDuration = 0.3f;
    public Material destealtMat = Main.destealthMat;
    public string beginSoundString = "Play_child_attack2_teleport";
    public string endSoundString = "Play_child_attack2_reappear";
    private Transform modelTransform;
    private CharacterModel characterModel;
    private HurtBoxGroup hurtboxGroup;
    private bool frolicFireFired;
    private bool tpFired;

    public override void OnEnter()
    {
        base.OnEnter();
        this.PlayAnimation("Gesture, Override", "FrolicEnter", "FrolicEnter.playbackRate", 1f);
        this.fireFrolicDuration += this.tpDuration;
        Util.PlaySound(this.beginSoundString, this.gameObject);
        this.FireTPEffect();
        this.modelTransform = this.GetModelTransform();
        if ((bool)this.modelTransform)
        {
            this.characterModel = this.modelTransform.GetComponent<CharacterModel>();
            this.hurtboxGroup = this.modelTransform.GetComponent<HurtBoxGroup>();
        }
        if ((bool)this.characterModel)
            ++this.characterModel.invisibilityCount;
        if (!(bool)this.hurtboxGroup)
            return;
        ++this.hurtboxGroup.hurtBoxesDeactivatorCounter;
    }

    private void FireTPEffect()
    {
        Vector3 position = this.FindModelChild("Chest").transform.position;
        EffectManager.SpawnEffect(ChildBlink.tpEffect, new EffectData()
        {
            origin = position,
            scale = 1f
        }, true);
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if ((double)this.fixedAge > this.tpDuration && !this.tpFired)
        {
            this.tpFired = true;
            this.TeleportAway();
        }
        if ((double)this.fixedAge <= (double)this.fireFrolicDuration || this.frolicFireFired)
            return;
        Util.PlaySound(this.endSoundString, this.gameObject);
        this.FireTPEffect();
        this.frolicFireFired = true;
        this.outer.SetNextStateToMain();
    }

    public override void OnExit()
    {
        if (!this.outer.destroying)
        {
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
        this.characterBody.transform.LookAt(this.characterBody.master.GetComponent<BaseAI>().currentEnemy.characterBody.transform.position);
        base.OnExit();
    }

    public void TeleportAway()
    {
        this.GetComponent<ModelLocator>().modelTransform.GetComponent<CharacterModel>();
        NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Ground);
        Vector3 corePosition = this.characterBody.corePosition;
        List<NodeGraph.NodeIndex> nodesInRange = nodeGraph.FindNodesInRange(corePosition, 10f, 25f, HullMask.Human);
        Vector3 position = new Vector3();
        bool flag = false;
        int num1 = 35;
        while (!flag)
        {
            NodeGraph.NodeIndex nodeIndex = nodesInRange.ElementAt<NodeGraph.NodeIndex>(Random.Range(1, nodesInRange.Count));
            nodeGraph.GetNodePosition(nodeIndex, out position);
            double num2 = (double)Vector3.Distance(this.characterBody.coreTransform.position, position);
            --num1;
            if (num2 > 35.0 || num1 < 0)
                flag = true;
        }
        if (num1 < 0)
            Debug.LogWarning("Child.ChildBlink state entered a loop where it ran more than 35 times without getting out - check what it's doing");
        TeleportHelper.TeleportBody(this.characterBody, position + Vector3.up * 1.5f, false);
    }
}
