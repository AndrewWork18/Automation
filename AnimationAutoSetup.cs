using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Animations;
using System.Collections.Generic;

public class AnimationAutoSetup : MonoBehaviour
{

    [MenuItem("MyTools/Setup Troop Animations and Overrides")]
    static void SetupTroopAnimationsAndOverrides()
    {
        string relativeTroopsFolderPath = "Sprites/Troops/"; // Path relative to Assets
        string fullTroopsFolderPath = Path.Combine(Application.dataPath, relativeTroopsFolderPath); // Full system path
        DirectoryInfo troopsDirectory = new DirectoryInfo(fullTroopsFolderPath);
        string baseControllerPath = "Assets/Sprites/Animations/TroopAnimatorController.controller";

        foreach (var troopFolder in troopsDirectory.GetDirectories())
        {
            // Setup animations and controllers for walking
            SetupTroopWalkAnimations(troopFolder, relativeTroopsFolderPath);

            //Set up animations and controllers for attacking
            SetupAttackAnimations(troopFolder, relativeTroopsFolderPath);

            // Create and assign overrides for each troop
            CreateAndAssignOverrideController(troopFolder, baseControllerPath);




        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All animations, controllers, and overrides setup complete.");
    }
    static void SetupTroopWalkAnimations(DirectoryInfo troopFolder, string relativeTroopsFolderPath)
    {
        string troopName = troopFolder.Name; // Get the troop's name
        string relativeWalkAnimationsPath = Path.Combine(relativeTroopsFolderPath, troopName, "WalkAnimations");
        string fullWalkAnimationsPath = Path.Combine(Application.dataPath, relativeWalkAnimationsPath);
        DirectoryInfo walkAnimationsDir = new DirectoryInfo(fullWalkAnimationsPath);

        if (!walkAnimationsDir.Exists) return;

        foreach (var directionFolder in walkAnimationsDir.GetDirectories())
        {
            string directionName = directionFolder.Name; //  North, south , etc...
            string animationName = troopName + directionName; // TroopOneNorth etc...
            string controllerRelativePath = "Assets/" + Path.Combine(relativeWalkAnimationsPath, directionName, animationName + "Controller.controller");

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerRelativePath);

            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            // If .anim files exist, process them, otherwise create new animations from images
            FileInfo[] animFiles = directionFolder.GetFiles("*.anim");
            if (animFiles.Length > 0)
            {
                foreach (var animFile in animFiles)
                {
                    string animRelativePath = "Assets/" + Path.Combine(relativeWalkAnimationsPath, directionName, animFile.Name);
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animRelativePath);
                    var state = controller.layers[0].stateMachine.AddState(clip.name);
                    state.motion = clip;
                }
            }
            else
            {
                CreateAnimationFromImages(directionFolder, controller, animationName);
            }
        }
    }
    static void CreateAnimationFromImages(DirectoryInfo imageFolder, AnimatorController controller, string animationName)
    {
        FileInfo[] files = imageFolder.GetFiles("*.png");
        if (files.Length == 0) return;

        AnimationClip animClip = new AnimationClip();
        // Set the animation to loop
        animClip.wrapMode = WrapMode.Loop;

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] spriteKeyFrames = new ObjectReferenceKeyframe[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            string filePath = files[i].FullName.Replace(@"\", "/").Replace(Application.dataPath, "Assets");
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);

            spriteKeyFrames[i] = new ObjectReferenceKeyframe { time = i / 12.0f, value = sprite }; // 12 fps
        }

        AnimationUtility.SetObjectReferenceCurve(animClip, spriteBinding, spriteKeyFrames);

        // Set loop time for the animation clip
        SerializedObject serializedClip = new SerializedObject(animClip);
        SerializedProperty loopTimeProp = serializedClip.FindProperty("m_LoopTime");
        if (loopTimeProp != null)
        {
            loopTimeProp.boolValue = true;
            serializedClip.ApplyModifiedProperties();
        }
        // Set loop time for the animation clip
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(animClip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(animClip, settings);

        string relativeFolderPath = imageFolder.FullName.Replace(@"\", "/").Replace(Application.dataPath, "Assets");
        string animationPath = Path.Combine(relativeFolderPath, animationName + ".anim");
        AssetDatabase.CreateAsset(animClip, animationPath);

        var state = controller.layers[0].stateMachine.AddState(animationName);
        state.motion = animClip;
    }

    static void CreateAndAssignOverrideController(DirectoryInfo troopFolder, string baseControllerPath)
    {
        // Get the troop's name, used for naming and path construction.
        string troopName = troopFolder.Name;

        // Load the base Animator Controller.
        AnimatorController baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(baseControllerPath);
        if (baseController == null)
        {
            Debug.LogError("Base controller not found at: " + baseControllerPath);
            return;
        }

        // Create a new Animator Override Controller based on the base controller.
        AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);
        string overrideControllerPath = "Assets/Sprites/Animations/" + troopName + "OverrideAnimatorController.overrideController";
        AssetDatabase.CreateAsset(overrideController, overrideControllerPath);

        // Get the current list of overrides from the override controller.
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        // Create a temporary list to hold new overrides.
        var newOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();

        // Iterate through each existing override pair.
        foreach (var pair in overrides)
        {
            // Construct the path to the expected animation clip based on the slot name and troop name.
            string slotName = pair.Key.name;  // e.g., MoveNorth, AttackNorth
            string directionPart = slotName.StartsWith("Attack") ? slotName.Substring(6) : slotName.Substring(4);  // e.g., North
            string animType = slotName.StartsWith("Attack") ? "Attack" : "Walk";  // Determine if it's an attack or walk animation
            string expectedAnimName = troopName + animType + directionPart;  // e.g., TroopFourWalkNorth or TroopFourAttackNorth
            string animPath = "Assets/Sprites/Troops/" + troopName + "/" + animType + "Animations/" + animType + directionPart + "/" + expectedAnimName + ".anim";

            // Try to load the corresponding Animation Clip.
            AnimationClip newClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);

            // If the animation clip exists, add it to the new overrides list.
            if (newClip != null)
            {
                newOverrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, newClip));
            }
            else
            {
                // If no new animation is found, keep the original pair.
                newOverrides.Add(pair);
            }
        }

        // Apply all the collected overrides to the controller at once.
        overrideController.ApplyOverrides(newOverrides);
        Debug.Log($"Animator Override Controller created and animations assigned for {troopName}.");
    }
    static void SetupAttackAnimations(DirectoryInfo troopFolder, string relativeTroopsFolderPath)
    {
        string troopName = troopFolder.Name;
        string relativeAttackAnimationsPath = Path.Combine(relativeTroopsFolderPath, troopName, "AttackAnimations");
        string fullAttackAnimationsPath = Path.Combine(Application.dataPath, relativeAttackAnimationsPath);
        DirectoryInfo attackAnimationsDir = new DirectoryInfo(fullAttackAnimationsPath);

        if (!attackAnimationsDir.Exists)
        {
            Debug.LogWarning("No AttackAnimations directory found for " + troopName);
            return; // No attack animations for this troop
        }

        foreach (var directionFolder in attackAnimationsDir.GetDirectories())
        {
            string directionName = directionFolder.Name; // e.g., AttackNorth
            string animationName = troopName + directionName; // e.g., TroopFourAttackNorth

            // Create an Animator Controller for each direction of attack animations
            string controllerRelativePath = "Assets/" + Path.Combine(relativeAttackAnimationsPath, directionName, animationName + "Controller.controller");
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerRelativePath);

            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            // Use the existing CreateAnimationFromImages method or a similar one for attacks
            CreateAnimationFromImages(directionFolder, controller, animationName);
        }
    }

}
