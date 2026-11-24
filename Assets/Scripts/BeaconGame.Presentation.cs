using System.Collections.Generic;
using UnityEngine;

public sealed partial class BeaconGame
{
    private sealed class Spark
    {
        internal SpriteRenderer Renderer;
        internal Vector2 Velocity;
        internal float EndTime;
        internal float Duration;
        internal Color Color;
    }

    private static readonly Color BackgroundColor = new Color32(10, 15, 28, 255);
    private static readonly Color Panel = new Color32(12, 20, 34, 246);
    private static readonly Color Border = new Color32(47, 62, 82, 255);
    private static readonly Color Ink = new Color32(241, 247, 249, 255);
    private static readonly Color Muted = new Color32(166, 187, 208, 255);
    private static readonly Color Warm = new Color32(255, 222, 140, 255);
    private static readonly Color Coral = new Color32(255, 138, 122, 255);

    private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, AudioClip> sounds = new Dictionary<string, AudioClip>();
    private readonly List<Spark> sparks = new List<Spark>();
    private readonly Dictionary<int, GUIStyle> labelStyles = new Dictionary<int, GUIStyle>();

    private Camera worldCamera;
    private AudioSource music;
    private AudioSource effects;
    private Transform worldRoot;
    private Transform backLayer;
    private SpriteRenderer player;
    private SpriteRenderer gate;
    private Animator playerAnimator;
    private RuntimeAnimatorController playerController;
    private RuntimeAnimatorController enemyController;
    private string playerPose;
    private Sprite square;
    private Sprite circle;
    private Texture2D squareTexture;
    private Texture2D circleTexture;
    private GUIStyle buttonStyle;
    private System.Random decorationRandom = new System.Random(6249);
    private System.Random effectRandom = new System.Random(4812);
    private readonly Vector3 cameraDepth = new Vector3(0, 0, -10);

    private void InitializePresentation()
    {
        Application.targetFrameRate = 60;
        worldCamera = Camera.main;
        if (worldCamera == null)
        {
            var cameraObject = new GameObject("Câmera orbital");
            cameraObject.tag = "MainCamera";
            worldCamera = cameraObject.AddComponent<Camera>();
        }

        worldCamera.orthographic = true;
        worldCamera.orthographicSize = 4.65f;
        worldCamera.clearFlags = CameraClearFlags.SolidColor;
        worldCamera.backgroundColor = BackgroundColor;
        worldCamera.transform.rotation = Quaternion.identity;
        if (FindAnyObjectByType<AudioListener>() == null)
            worldCamera.gameObject.AddComponent<AudioListener>();

        squareTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        squareTexture.name = "Farol-bloco";
        squareTexture.SetPixel(0, 0, Color.white);
        squareTexture.Apply();
        square = Sprite.Create(squareTexture, new Rect(0, 0, 1, 1), Vector2.one / 2, 1);

        const int size = 96;
        circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        circleTexture.name = "Farol-disco";
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), Vector2.one * (size / 2f));
                byte alpha = (byte)(Mathf.Clamp01(size / 2f - distance) * 255);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }
        circleTexture.SetPixels32(pixels);
        circleTexture.Apply();
        circle = Sprite.Create(circleTexture, new Rect(0, 0, size, size), Vector2.one / 2, size);

        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.loop = true;
        music.volume = .36f;
        music.clip = Resources.Load<AudioClip>("Audio/Music");

        effects = gameObject.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        effects.volume = .78f;

        foreach (string cue in new[] { "Jump", "Collect", "Hurt", "Enemy", "Checkpoint", "Clear" })
            sounds.Add(cue, Resources.Load<AudioClip>("Audio/" + cue));

        playerController = Resources.Load<RuntimeAnimatorController>("Animation/Player");
        enemyController = Resources.Load<RuntimeAnimatorController>("Animation/Enemy");
    }

    private void BuildWorld()
    {
        if (worldRoot != null)
        {
            worldRoot.gameObject.SetActive(false);
            Destroy(worldRoot.gameObject);
        }

        sparks.Clear();
        worldRoot = new GameObject(level.Name).transform;
        worldRoot.SetParent(transform, false);
        decorationRandom = new System.Random(6249 + stage * 103);
        effectRandom = new System.Random(4812 + stage * 19);
        BuildBackground();

        foreach (Vector3 rail in level.Ladders)
            DrawLadder(rail);

        foreach (Rect surface in level.Surfaces)
            DrawPlatform(surface);

        foreach (Rect strip in level.Spikes)
            DrawSpikes(strip);

        foreach (float x in level.Checkpoints)
        {
            MakeVisual(worldRoot, "Luz de retorno", circle, new Vector2(x, .61f), new Vector2(1.20f, 1.20f),
                WithAlpha(level.Accent, .10f), 1);
            SpriteRenderer marker = DrawAsset("Checkpoint", new Vector2(x, .58f), new Vector2(.70f, 1.14f), 12,
                new Color(.68f, .82f, .88f, 1));
            checkpointVisuals.Add(marker);
        }

        Vector2 gatePosition = new Vector2(level.GateX, .89f);
        Rectangle(worldRoot, "Estrutura da comporta", gatePosition + Vector2.up * .11f,
            new Vector2(1.72f, 2.0f), new Color32(38, 62, 81, 255), 2);
        Rectangle(worldRoot, "Fundo da comporta", gatePosition + Vector2.up * .11f,
            new Vector2(1.58f, 1.86f), new Color32(18, 29, 45, 255), 3);
        Rectangle(worldRoot, "Luz da comporta", gatePosition + Vector2.up * 1.20f,
            new Vector2(1.42f, .09f), level.Accent, 8);
        gate = DrawAsset("Exit", gatePosition + Vector2.up * .10f, new Vector2(1.42f, 1.85f), 13, Warm);

        foreach (Vector2 origin in level.Cores)
        {
            SpriteRenderer halo = MakeVisual(worldRoot, "Halo da célula", circle, origin,
                new Vector2(.94f, .94f), WithAlpha(Warm, .15f), 8);
            pickups.Add(new Pickup
            {
                Position = origin,
                Halo = halo,
                Visual = DrawAsset("Core", origin, new Vector2(.59f, .59f), 14, Color.white)
            });
        }

        foreach (Vector3 route in level.Patrols)
        {
            var patrol = new Patrol { Route = route, Position = new Vector2((route.x + route.y) * .5f, route.z) };
            patrol.Visual = DrawAsset("Enemy/Patrol_00", patrol.Position + Vector2.up * .33f, new Vector2(.92f, .78f), 16, Color.white);
            if (enemyController != null)
            {
                var animator = patrol.Visual.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = enemyController;
                animator.Play("Patrol");
            }
            enemies.Add(patrol);
        }

        player = DrawAsset("Player/Idle_00", position + Vector2.up * .50f, new Vector2(1.16f, 1.16f), 20, Color.white);
        if (playerController != null)
        {
            playerAnimator = player.gameObject.AddComponent<Animator>();
            playerAnimator.runtimeAnimatorController = playerController;
        }
        else
        {
            playerAnimator = null;
        }
        playerPose = null;
    }

    private void BuildBackground()
    {
        backLayer = new GameObject("Campo orbital").transform;
        backLayer.SetParent(worldRoot, false);
        Color baseWall = Color.Lerp(BackgroundColor, level.Accent, .06f);
        Rectangle(backLayer, "Nébula", new Vector2(level.Width * .25f, 3.1f), new Vector2(level.Width + 25, 19), baseWall, -90);

        Sprite orbit = GetSprite("Backdrop/Orbit");
        if (orbit != null)
        {
            const float panelWidth = 24;
            float panelHeight = panelWidth * orbit.bounds.size.y / orbit.bounds.size.x;
            for (float x = 4; x < level.Width * .58f + panelWidth; x += panelWidth)
            {
                MakeVisual(backLayer, "Órbita distante", orbit, new Vector2(x, 3.8f), new Vector2(panelWidth, panelHeight),
                    new Color(.65f, .74f, .87f, .77f), -85);
            }
        }

        for (int i = 0; i < 95; i++)
        {
            float x = Range(-10, level.Width * .70f + 10);
            float y = Range(1.4f, 8.6f);
            float width = i % 11 == 0 ? .045f : Range(.012f, .029f);
            Rectangle(backLayer, "Estrela", new Vector2(x, y), new Vector2(width, width),
                WithAlpha(i % 7 == 0 ? level.Accent : Ink, Range(.18f, .52f)), -80);
        }

        if (orbit == null)
        {
            Vector2 planet = new Vector2(stage == 0 ? 10 : 12, 5.4f);
            MakeVisual(backLayer, "Planeta em órbita", circle, planet, new Vector2(4.5f, 4.5f), new Color32(32, 45, 65, 255), -83);
            MakeVisual(backLayer, "Sombra do planeta", circle, planet + new Vector2(-.23f, -.12f),
                new Vector2(4.25f, 4.25f), new Color32(16, 26, 45, 255), -82);
        }

        Color bulkhead = Color.Lerp(baseWall, level.Accent, .115f);
        for (float x = -2; x < level.Width + 3; x += 5.1f)
        {
            float height = Range(3f, 6f);
            Rectangle(worldRoot, "Coluna da estação", new Vector2(x, -.9f + height / 2), new Vector2(.55f, height), bulkhead, -60);
            Rectangle(worldRoot, "Conduíte", new Vector2(x + .4f, -.4f + height / 2), new Vector2(.08f, height),
                Color.Lerp(bulkhead, level.Accent, .11f), -59);
            Rectangle(worldRoot, "Luz da estação", new Vector2(x + .39f, height - 1.1f), new Vector2(.15f, .29f),
                WithAlpha(level.Accent, .32f), -57);
        }

        for (float x = -2; x < level.Width; x += 1.22f)
        {
            SpriteRenderer bar = Rectangle(worldRoot, "Treliça orbital", new Vector2(x, 1.46f),
                new Vector2(.065f, 1.45f), bulkhead, -56);
            bar.transform.rotation = Quaternion.Euler(0, 0, Mathf.RoundToInt(x) % 2 == 0 ? 28 : -28);
        }
        Rectangle(worldRoot, "Passarela distante", new Vector2(level.Width / 2, 2.05f),
            new Vector2(level.Width + 5, .09f), bulkhead, -58);
        Rectangle(worldRoot, "Passarela distante", new Vector2(level.Width / 2, .87f),
            new Vector2(level.Width + 5, .11f), bulkhead, -58);
    }

    private void DrawPlatform(Rect surface)
    {
        bool floor = surface.yMax <= .01f;
        float tileHeight = floor ? .84f : .57f;
        float centerY = surface.yMax - tileHeight / 2;
        Rectangle(worldRoot, "Viga", new Vector2(surface.center.x, surface.yMax - surface.height / 2),
            surface.size, new Color32(29, 39, 56, 255), 3);

        for (float x = surface.xMin; x < surface.xMax - .001f; x += 1.0f)
        {
            float width = Mathf.Min(1, surface.xMax - x);
            DrawAsset("Platform", new Vector2(x + width / 2, centerY), new Vector2(width + .001f, tileHeight), 5, Color.white);
        }

        Rectangle(worldRoot, "Borda transitável", new Vector2(surface.center.x, surface.yMax - .026f),
            new Vector2(surface.width, .052f), WithAlpha(level.Accent, .77f), 6);
        Rectangle(worldRoot, "Chanfro da plataforma", new Vector2(surface.center.x, surface.yMax - .13f),
            new Vector2(surface.width, .035f), WithAlpha(level.Accent, .24f), 6);
    }

    private void DrawLadder(Vector3 rail)
    {
        float height = rail.z - rail.y;
        float center = (rail.y + rail.z) / 2;
        Rectangle(worldRoot, "Halo da escada", new Vector2(rail.x, center), new Vector2(.80f, height),
            WithAlpha(level.Accent, .10f), 1);
        for (float y = rail.y; y < rail.z - .001f; y += .55f)
        {
            float cellHeight = Mathf.Min(.55f, rail.z - y);
            DrawAsset("Climb", new Vector2(rail.x, y + cellHeight / 2), new Vector2(.66f, cellHeight), 8, Color.white);
        }
    }

    private void DrawSpikes(Rect strip)
    {
        Rectangle(worldRoot, "Faixa de risco", new Vector2(strip.center.x, .035f), new Vector2(strip.width, .07f), Coral, 9);
        for (float x = strip.xMin; x < strip.xMax - .001f; x += .48f)
        {
            float width = Mathf.Min(.48f, strip.xMax - x);
            DrawAsset("Spikes", new Vector2(x + width / 2, strip.height / 2), new Vector2(width, strip.height), 10, Color.white);
        }
    }

    private SpriteRenderer DrawAsset(string path, Vector2 center, Vector2 size, int order, Color color)
    {
        Sprite sprite = GetSprite(path);
        if (sprite == null)
        {
            Color fallback = path.Contains("Player") ? level.Accent :
                path == "Core" ? Warm : path == "Spikes" || path.Contains("Enemy") ? Coral :
                path == "Platform" ? new Color32(56, 73, 94, 255) : level.Accent;
            return Rectangle(worldRoot, path, center, size, fallback, order);
        }
        return MakeVisual(worldRoot, path, sprite, center, size, color, order);
    }

    private Sprite GetSprite(string path)
    {
        if (!sprites.TryGetValue(path, out Sprite sprite))
        {
            sprite = Resources.Load<Sprite>("Sprites/" + path);
            sprites.Add(path, sprite);
        }
        return sprite;
    }

    private SpriteRenderer Rectangle(Transform parent, string name, Vector2 center, Vector2 size, Color color, int order) =>
        MakeVisual(parent, name, square, center, size, color, order);

    private static SpriteRenderer MakeVisual(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, Color color, int order)
    {
        var instance = new GameObject(name);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = center;
        Vector3 bounds = sprite.bounds.size;
        instance.transform.localScale = new Vector3(size.x / bounds.x, size.y / bounds.y, 1);
        var renderer = instance.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    private void UpdatePresentation(float delta, bool snap)
    {
        if (player == null)
            return;

        player.transform.localPosition = position + Vector2.up * .51f;
        if (Mathf.Abs(horizontalInput) > .03f)
            player.flipX = horizontalInput < 0;

        float motion = Mathf.Abs(velocity.x);
        string pose = hurtTime > 0 ? "Hurt" : ladder >= 0 ? "Climb" : !grounded ? "Jump" :
            motion > WalkSpeed + .4f ? "Run" : motion > .13f ? "Walk" : "Idle";
        if (pose != playerPose)
        {
            playerPose = pose;
            if (playerAnimator != null)
            {
                playerAnimator.Play("Base Layer." + pose, 0, 0);
            }
            else
            {
                Sprite sprite = GetSprite("Player/" + pose + "_00");
                if (sprite != null)
                    player.sprite = sprite;
            }
        }

        player.color = protection > 0 && worldTime % .16f < .07f ? new Color(1, 1, 1, .47f) : Color.white;
        if (gate != null)
            gate.color = collected == level.Cores.Length ? Color.white : Warm;

        foreach (Patrol patrol in enemies)
        {
            if (!patrol.Active)
                continue;
            patrol.Visual.transform.localPosition = patrol.Position + Vector2.up * .35f;
            patrol.Visual.flipX = patrol.Direction < 0;
        }
        foreach (Pickup pickup in pickups)
        {
            if (pickup.Active)
                pickup.Visual.transform.localPosition = pickup.Position + Vector2.up * (Mathf.Sin(worldTime * 3.7f + pickup.Position.x) * .072f);
        }
        for (int i = sparks.Count - 1; i >= 0; i--)
        {
            Spark spark = sparks[i];
            if (worldTime >= spark.EndTime)
            {
                spark.Renderer.gameObject.SetActive(false);
                Destroy(spark.Renderer.gameObject);
                sparks.RemoveAt(i);
                continue;
            }
            spark.Velocity += Vector2.down * (delta * 4.6f);
            spark.Renderer.transform.localPosition += (Vector3)(spark.Velocity * delta);
            spark.Renderer.color = WithAlpha(spark.Color, (spark.EndTime - worldTime) / spark.Duration);
        }

        if (snap)
        {
            SnapCamera();
        }
        else
        {
            Vector3 target = CameraTarget();
            worldCamera.transform.position = Vector3.Lerp(worldCamera.transform.position, target, 1 - Mathf.Exp(-8.5f * delta));
            MoveBackground();
        }
    }

    private Vector3 CameraTarget()
    {
        float half = worldCamera.orthographicSize * worldCamera.aspect;
        float ahead = Mathf.Clamp(velocity.x / 6, -.95f, .95f);
        float x = level.Width > half * 2 ? Mathf.Clamp(position.x + ahead, half, level.Width - half) : level.Width / 2;
        float y = 2.8f + .55f * Mathf.Max(0, position.y - 1.75f);
        return new Vector3(x, y, 0) + cameraDepth;
    }

    private void SnapCamera()
    {
        worldCamera.transform.position = CameraTarget();
        MoveBackground();
    }

    private void MoveBackground()
    {
        if (backLayer != null)
            backLayer.localPosition = new Vector3(worldCamera.transform.position.x * .52f, worldCamera.transform.position.y * .09f, 0);
    }

    private void Burst(Vector2 at, Color color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (float)effectRandom.NextDouble() * Mathf.PI * 2;
            float speed = 1 + (float)effectRandom.NextDouble() * 3.0f;
            float duration = .25f + (float)effectRandom.NextDouble() * .35f;
            float size = .038f + (float)effectRandom.NextDouble() * .072f;
            Spark spark = new Spark
            {
                Renderer = Rectangle(worldRoot, "Centelha", at, Vector2.one * size, color, 24),
                Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) + .45f) * speed,
                Duration = duration,
                EndTime = worldTime + duration,
                Color = color
            };
            sparks.Add(spark);
        }
    }

    private float Range(float low, float high) => Mathf.Lerp(low, high, (float)decorationRandom.NextDouble());
    private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

    private void PlayCue(string cue)
    {
        if (sounds.TryGetValue(cue, out AudioClip clip) && clip != null)
            effects.PlayOneShot(clip);
    }

    private void PlayMusic()
    {
        if (music.clip != null && !music.isPlaying)
            music.Play();
    }

    private void StopMusic() => music.Stop();

    private void OnDestroy()
    {
        Destroy(square);
        Destroy(circle);
        Destroy(squareTexture);
        Destroy(circleTexture);
    }
}
