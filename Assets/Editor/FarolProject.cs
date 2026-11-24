using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FarolProject
{
    public const string EntryScene = "Assets/Scenes/Farol.unity";
    private const string SpriteRoot = "Assets/Resources/Sprites";
    private const string AnimationRoot = "Assets/Resources/Animation";

    private readonly struct Motion
    {
        internal readonly string Name;
        internal readonly int Frames;
        internal readonly int FramesPerSecond;
        internal readonly bool Loop;

        internal Motion(string name, int frames, int framesPerSecond, bool loop = true)
        {
            Name = name;
            Frames = frames;
            FramesPerSecond = framesPerSecond;
            Loop = loop;
        }
    }

    [MenuItem("Farol de Íon/Preparar projeto")]
    public static void Prepare()
    {
        ImportSprites();
        CreateController("Player", new[]
        {
            new Motion("Idle", 4, 5), new Motion("Walk", 8, 10),
            new Motion("Run", 8, 15), new Motion("Jump", 4, 9, false),
            new Motion("Climb", 6, 10), new Motion("Hurt", 3, 12, false)
        });
        CreateController("Enemy", new[] { new Motion("Patrol", 6, 9) });
        CreateScene();

        PlayerSettings.companyName = "Farol de Íon";
        PlayerSettings.productName = "Farol de Íon";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        PlayerSettings.SplashScreen.show = false;
        AssetDatabase.SaveAssets();
    }

    private static void ImportSprites()
    {
        foreach (string file in Directory.GetFiles(SpriteRoot, "*.png", SearchOption.AllDirectories))
        {
            string path = file.Replace('\\', '/');
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spritePixelsPerUnit == 32 && importer.filterMode == FilterMode.Point &&
                importer.textureCompression == TextureImporterCompression.Uncompressed &&
                !importer.mipmapEnabled && importer.spriteImportMode == SpriteImportMode.Single)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePivot = new Vector2(.5f, .5f);
            importer.SaveAndReimport();
        }
    }

    private static void CreateController(string name, Motion[] motions)
    {
        string folder = $"{AnimationRoot}/{name}Clips";
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        string path = $"{AnimationRoot}/{name}.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (Motion motion in motions)
        {
            string clipPath = $"{folder}/{motion.Name}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = motion.Name };
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            clip.frameRate = motion.FramesPerSecond;
            var frames = new ObjectReferenceKeyframe[motion.Frames + 1];
            for (int index = 0; index <= motion.Frames; index++)
            {
                int spriteIndex = index == motion.Frames ? (motion.Loop ? 0 : index - 1) : index;
                string spritePath = $"{SpriteRoot}/{name}/{motion.Name}_{spriteIndex:00}.png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null)
                    throw new FileNotFoundException("Sprite de animação ausente", spritePath);

                frames[index] = new ObjectReferenceKeyframe
                {
                    time = index / (float)motion.FramesPerSecond,
                    value = sprite
                };
            }

            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = motion.Loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);

            AnimatorState state = stateMachine.states.FirstOrDefault(child => child.state.name == motion.Name).state;
            if (state == null)
                state = stateMachine.AddState(motion.Name);
            state.motion = clip;
            if (motion.Name == motions[0].Name)
                stateMachine.defaultState = state;
        }

        EditorUtility.SetDirty(controller);
    }

    private static void CreateScene()
    {
        if (!File.Exists(EntryScene))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(EntryScene));
            Scene previous = SceneManager.GetActiveScene();
            bool keepPrevious = previous.IsValid() && !string.IsNullOrEmpty(previous.path);
            if (!keepPrevious && !Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("A criação da cena foi cancelada.");

            NewSceneMode mode = keepPrevious ? NewSceneMode.Additive : NewSceneMode.Single;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            SceneManager.SetActiveScene(scene);
            new GameObject("Farol de Íon").AddComponent<BeaconGame>();
            if (!EditorSceneManager.SaveScene(scene, EntryScene))
                throw new IOException($"Não foi possível salvar {EntryScene}.");

            if (keepPrevious)
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(EntryScene, true) };
    }

    [MenuItem("Farol de Íon/Exportar para Linux")]
    public static void BuildLinux() => Build(BuildTarget.StandaloneLinux64, "Builds/Linux/FarolDeIon.x86_64");

    [MenuItem("Farol de Íon/Exportar para Windows")]
    public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/FarolDeIon.exe");

    public static void BuildLinuxSmoke() => Build(BuildTarget.StandaloneLinux64,
        "Builds/Smoke/FarolDeIon.x86_64", BuildOptions.Development);

    private static void Build(BuildTarget target, string defaultPath, BuildOptions options = BuildOptions.None)
    {
        Prepare();
        string overridePath = Environment.GetEnvironmentVariable("FAROL_BUILD_PATH");
        string path = string.IsNullOrEmpty(overridePath) ? defaultPath : overridePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { EntryScene },
            locationPathName = path,
            target = target,
            options = options
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Build {target}: {report.summary.result} ({report.summary.totalErrors} erros).");

        Debug.Log($"Farol de Íon: build {target} concluído em {path}.");
    }
}
