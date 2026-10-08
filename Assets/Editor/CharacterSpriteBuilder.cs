using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Turns a character sheet, e.g. Assets/Art/Wizard.png (+ Wizard.json), into everything
// the Animator needs:
//   1. Slices the sheet into individual Sprites (via SpriteSheetImporter)
//   2. Makes one AnimationClip per row: keyframes that swap SpriteRenderer.sprite
//   3. Builds the Animator Controller state machine (open Animations/<Sheet>/<Sheet>.controller
//      in Window > Animation > Animator to see the graph)
// Every character gets the same state machine; only the art and the name of the
// "action" state differ (Cast for the wizard, Attack for the skeleton).
// Called by DungeonBuilder.Build(). Regenerate the art with the scripts in Tools/.
public static class CharacterSpriteBuilder
{
    private const string ShadowPath = "Assets/Art/Shadow.png";

    public struct Result
    {
        public RuntimeAnimatorController Controller;
        public Sprite DefaultSprite;
        public Sprite Shadow;
    }

    public static Result Build(string sheet)
    {
        // 1. Slicing is shared with the spell effects, so it lives in SpriteSheetImporter.
        var sprites = SpriteSheetImporter.Import(sheet);

        string animFolder = $"Assets/Art/Animations/{sheet}";
        if (!AssetDatabase.IsValidFolder("Assets/Art/Animations"))
            AssetDatabase.CreateFolder("Assets/Art", "Animations");
        if (!AssetDatabase.IsValidFolder(animFolder))
            AssetDatabase.CreateFolder("Assets/Art/Animations", sheet);
        var clips = sprites.Layout.animations.ToDictionary(
            a => a.name, a => CreateClip(animFolder, a, sprites.Frames(a.name)));

        return new Result
        {
            Controller = CreateController($"{animFolder}/{sheet}.controller", sprites.Layout.action, clips),
            DefaultSprite = sprites.Frames("Idle_Front")[0],
            Shadow = SpriteSheetImporter.ImportSingle(ShadowPath, sprites.Layout.pixelsPerUnit),
        };
    }

    // ---------- 2. Clips ----------

    private static AnimationClip CreateClip(string folder, SpriteSheetImporter.AnimInfo anim, Sprite[] frames)
    {
        string path = $"{folder}/{anim.name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.frameRate = anim.fps;

        // A sprite animation is just a curve that swaps the SpriteRenderer's sprite.
        // "" = the SpriteRenderer is on the same GameObject as the Animator.
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[anim.frames + 1];
        for (int i = 0; i < anim.frames; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / (float)anim.fps, value = frames[i] };
        // Repeat the last frame at the end so it's shown for a full frame, not 0 seconds.
        keys[anim.frames] = new ObjectReferenceKeyframe
        {
            time = anim.frames / (float)anim.fps,
            value = keys[anim.frames - 1].value,
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = anim.loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    // ---------- 3. State machine ----------

    private static AnimatorController CreateController(string path, string action, Dictionary<string, AnimationClip> clips)
    {
        AssetDatabase.DeleteAsset(path); // rebuilt from scratch every time
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("FacingBack", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Action", AnimatorControllerParameterType.Trigger); // plays the Cast/Attack state
        ctrl.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Dead", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Sitting", AnimatorControllerParameterType.Bool); // only heroes have a Sit clip

        var sm = ctrl.layers[0].stateMachine;

        // Each action state holds a 1D blend tree that picks the Front or Back clip.
        AnimatorState Directional(string name)
        {
            var state = ctrl.CreateBlendTreeInController(name, out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "FacingBack";
            tree.useAutomaticThresholds = false;
            tree.AddChild(clips[name + "_Front"], 0f);
            tree.AddChild(clips[name + "_Back"], 1f);
            return state;
        }

        var idle = Directional("Idle");
        var walk = Directional("Walk");
        var act = Directional(action);
        var hurt = Directional("Hurt");
        var die = sm.AddState("Die");
        die.motion = clips["Die"];
        sm.defaultState = idle;

        // Sprites can't cross-fade, so every transition is instant (duration 0).
        Instant(idle.AddTransition(walk)).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        Instant(walk.AddTransition(idle)).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // One-shot states play to the end (exit time 1 = 100%), then return to Idle.
        AfterFinishing(act.AddTransition(idle));
        AfterFinishing(hurt.AddTransition(idle));

        // "Any State" transitions can interrupt whatever is playing. They're checked in
        // the order added, so Die wins, and Hurt/Action are blocked once Dead is set.
        var toDie = Instant(sm.AddAnyStateTransition(die));
        toDie.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
        toDie.canTransitionToSelf = false;

        var toHurt = Instant(sm.AddAnyStateTransition(hurt));
        toHurt.AddCondition(AnimatorConditionMode.If, 0f, "Hurt");
        toHurt.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");

        var toAction = Instant(sm.AddAnyStateTransition(act));
        toAction.AddCondition(AnimatorConditionMode.If, 0f, "Action");
        toAction.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");

        // Sitting (the heroes, on the toilet at home): held for as long as Sitting is set.
        // Hurt or Action can still interrupt it; they return to Idle, and from there this
        // Any State transition sits the hero straight back down.
        AnimatorState sit = null;
        if (clips.TryGetValue("Sit", out var sitClip))
        {
            sit = sm.AddState("Sit");
            sit.motion = sitClip;
            var toSit = Instant(sm.AddAnyStateTransition(sit));
            toSit.AddCondition(AnimatorConditionMode.If, 0f, "Sitting");
            toSit.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
            toSit.canTransitionToSelf = false;
            Instant(sit.AddTransition(idle)).AddCondition(AnimatorConditionMode.IfNot, 0f, "Sitting");
        }

        // Lay the graph out readably for the Animator window.
        Place(sm, idle, 300, 0); Place(sm, walk, 560, 0);
        Place(sm, act, 300, 140); Place(sm, hurt, 560, 140); Place(sm, die, 300, -140);
        if (sit != null) Place(sm, sit, 560, -140);
        sm.anyStatePosition = new Vector3(40, 140);
        sm.entryPosition = new Vector3(40, 0);

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    private static T Instant<T>(T t) where T : AnimatorStateTransition
    {
        t.hasExitTime = false;
        t.duration = 0f;
        return t;
    }

    private static void AfterFinishing(AnimatorStateTransition t)
    {
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.duration = 0f;
    }

    private static void Place(AnimatorStateMachine sm, AnimatorState state, float x, float y)
    {
        var states = sm.states;
        for (int i = 0; i < states.Length; i++)
            if (states[i].state == state) states[i].position = new Vector3(x, y);
        sm.states = states;
    }
}
