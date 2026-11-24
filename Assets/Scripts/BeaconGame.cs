using System.Collections.Generic;
using UnityEngine;

public sealed partial class BeaconGame : MonoBehaviour
{
    private enum Status { Menu, Playing, Transition, GameOver, Victory }

    private sealed class Patrol
    {
        internal Vector3 Route;
        internal Vector2 Position;
        internal float Direction = 1;
        internal SpriteRenderer Visual;
        internal bool Active = true;
    }

    private sealed class Pickup
    {
        internal Vector2 Position;
        internal SpriteRenderer Visual;
        internal SpriteRenderer Halo;
        internal bool Active = true;
    }

    private const int StartingLives = 4;
    private const float BodyHalfWidth = .29f;
    private const float BodyHeight = .86f;
    private const float JumpSpeed = 9.8f;
    private const float Gravity = 23.5f;
    private const float ClimbSpeed = 3.55f;
    private const float WalkSpeed = 4.4f;
    private const float RunSpeed = 8.05f;
    private const float SpawnX = 1.4f;

    private readonly List<Patrol> enemies = new List<Patrol>();
    private readonly List<Pickup> pickups = new List<Pickup>();
    private readonly List<SpriteRenderer> checkpointVisuals = new List<SpriteRenderer>();

    private Status status;
    private BeaconLevel level;
    private int stage;
    private int lives;
    private int score;
    private int collected;
    private int totalCells;
    private int checkpoint;
    private Vector2 position;
    private Vector2 velocity;
    private bool grounded;
    private int ladder = -1;
    private float ladderLock;
    private float jumpBuffer;
    private float coyoteTime;
    private float protection;
    private float hurtTime;
    private float bannerTime;
    private float feedbackTime;
    private string feedback;
    private bool feedbackWarning;
    private float worldTime;
    private float transitionTime;
    private float horizontalInput;
    private float verticalInput;
    private bool runInput;
    private bool jumpHeld;
    private bool jumpPressed;
    private bool interactPressed;
    private bool smokeMode;
    private bool smokeJumpHeld;

    public int StageNumber => stage + 1;
    public int Lives => lives;
    public int Score => score;
    public int LevelCells => collected;
    public int RequiredCells => level.Cores.Length;
    public Vector2 PlayerPosition => position;
    public Vector2 PlayerVelocity => velocity;
    public bool Grounded => grounded;
    public string RunStatus => status.ToString();
    public Vector2 GoalPosition => new Vector2(level.GateX, 0);
    public Vector2[] RemainingCells
    {
        get
        {
            var cells = new List<Vector2>();
            foreach (Pickup pickup in pickups)
                if (pickup.Active)
                    cells.Add(pickup.Position);
            return cells.ToArray();
        }
    }

    private void Awake()
    {
        InitializePresentation();
        lives = StartingLives;
        LoadLevel(0);
        status = Status.Menu;
    }

    private void Update()
    {
        if (smokeMode)
            return;

        if (status == Status.Menu || status == Status.Victory || status == Status.GameOver)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.R))
                StartRun();
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            StartRun();
            return;
        }

        horizontalInput = Axis(KeyCode.A, KeyCode.LeftArrow, KeyCode.D, KeyCode.RightArrow);
        verticalInput = Axis(KeyCode.S, KeyCode.DownArrow, KeyCode.W, KeyCode.UpArrow);
        runInput = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        jumpHeld = Input.GetKey(KeyCode.Space);
        jumpPressed |= Input.GetKeyDown(KeyCode.Space);
        interactPressed |= Input.GetKeyDown(KeyCode.E);
    }

    private void FixedUpdate()
    {
        if (!smokeMode)
            Step(Time.fixedDeltaTime);
    }

    private void LateUpdate()
    {
        if (!smokeMode)
            UpdatePresentation(Time.deltaTime, false);
    }

    // Aciona o mesmo ciclo fixo da partida para testes determinísticos no Play Mode.
    public void BeginSmokeTest()
    {
        smokeMode = true;
        smokeJumpHeld = false;
        StartRun();
        UpdatePresentation(0, true);
    }

    public void AdvanceSmokeFrame(float horizontal, float vertical, bool run = false, bool jump = false, bool interact = false)
    {
        if (!smokeMode)
            return;

        horizontalInput = Mathf.Clamp(horizontal, -1, 1);
        verticalInput = Mathf.Clamp(vertical, -1, 1);
        runInput = run;
        jumpHeld = jump;
        jumpPressed = jump && !smokeJumpHeld;
        interactPressed = interact;
        smokeJumpHeld = jump;
        Step(.02f);
        UpdatePresentation(.02f, true);
    }

    private static float Axis(KeyCode negative, KeyCode negativeAlternate, KeyCode positive, KeyCode positiveAlternate)
    {
        int left = Input.GetKey(negative) || Input.GetKey(negativeAlternate) ? 1 : 0;
        int right = Input.GetKey(positive) || Input.GetKey(positiveAlternate) ? 1 : 0;
        return right - left;
    }

    private void StartRun()
    {
        lives = StartingLives;
        score = 0;
        totalCells = 0;
        horizontalInput = 0;
        verticalInput = 0;
        runInput = false;
        jumpHeld = false;
        jumpPressed = false;
        interactPressed = false;
        LoadLevel(0);
        status = Status.Playing;
        PlayMusic();
    }

    private void LoadLevel(int index)
    {
        stage = index;
        level = BeaconLevel.Campaign[index];
        collected = 0;
        checkpoint = -1;
        worldTime = 0;
        transitionTime = 0;
        bannerTime = 2.25f;
        feedbackTime = 0;
        protection = 0;
        hurtTime = 0;
        jumpBuffer = 0;
        coyoteTime = 0;
        pickups.Clear();
        enemies.Clear();
        checkpointVisuals.Clear();
        PlacePlayer(SpawnX);
        BuildWorld();
        UpdatePresentation(0, true);
    }

    private void PlacePlayer(float x)
    {
        position = new Vector2(x, 0);
        velocity = Vector2.zero;
        grounded = true;
        ladder = -1;
        ladderLock = 0;
        jumpBuffer = 0;
        jumpPressed = false;
    }

    private void Step(float delta)
    {
        worldTime += delta;
        if (status == Status.Transition)
        {
            transitionTime -= delta;
            if (transitionTime <= 0)
            {
                if (stage + 1 < BeaconLevel.Campaign.Length)
                {
                    LoadLevel(stage + 1);
                    status = Status.Playing;
                }
                else
                {
                    status = Status.Victory;
                    StopMusic();
                }
            }
            return;
        }
        if (status != Status.Playing)
            return;

        bannerTime = Mathf.Max(0, bannerTime - delta);
        feedbackTime = Mathf.Max(0, feedbackTime - delta);
        protection = Mathf.Max(0, protection - delta);
        hurtTime = Mathf.Max(0, hurtTime - delta);
        ladderLock = Mathf.Max(0, ladderLock - delta);
        jumpBuffer = jumpPressed ? .135f : Mathf.Max(0, jumpBuffer - delta);
        coyoteTime = grounded ? .115f : Mathf.Max(0, coyoteTime - delta);
        jumpPressed = false;

        Vector2 previous = position;
        if (TryEnterLadder())
            Climb(delta);
        else
            MoveOnPlatforms(delta);

        MovePatrols(delta);
        if (position.y < -3.3f)
        {
            Damage(true);
            interactPressed = false;
            return;
        }
        if (HitHazard(previous))
        {
            interactPressed = false;
            return;
        }

        CollectCells();
        SaveCheckpoint();

        if (interactPressed && NearGate())
            ActivateGate();
        interactPressed = false;
    }

    private bool TryEnterLadder()
    {
        if (ladder >= 0)
            return true;
        if (Mathf.Abs(verticalInput) < .35f || ladderLock > 0 || hurtTime > 0)
            return false;

        for (int i = 0; i < level.Ladders.Length; i++)
        {
            Vector3 rail = level.Ladders[i];
            if (Mathf.Abs(position.x - rail.x) > .44f ||
                position.y < rail.y - .12f || position.y > rail.z + .18f)
                continue;
            if ((verticalInput > 0 && position.y >= rail.z - .04f) ||
                (verticalInput < 0 && position.y <= rail.y + .04f))
                continue;

            ladder = i;
            velocity = Vector2.zero;
            grounded = false;
            return true;
        }
        return false;
    }

    private void Climb(float delta)
    {
        if (jumpBuffer > 0)
        {
            ladder = -1;
            ladderLock = .24f;
            LaunchJump();
            MoveOnPlatforms(delta);
            return;
        }
        if (Mathf.Abs(horizontalInput) > .7f && Mathf.Abs(verticalInput) < .15f)
        {
            ladder = -1;
            ladderLock = .22f;
            MoveOnPlatforms(delta);
            return;
        }

        Vector3 rail = level.Ladders[ladder];
        position.x = Mathf.MoveTowards(position.x, rail.x, delta * 7);
        position.y = Mathf.Clamp(position.y + verticalInput * ClimbSpeed * delta, rail.y, rail.z);
        if ((verticalInput > 0 && position.y >= rail.z) || (verticalInput < 0 && position.y <= rail.y))
        {
            ladder = -1;
            grounded = true;
            ladderLock = .12f;
        }
    }

    private void MoveOnPlatforms(float delta)
    {
        float topSpeed = runInput ? RunSpeed : WalkSpeed;
        float desired = horizontalInput * topSpeed;
        float acceleration = Mathf.Abs(horizontalInput) > .01f ? (grounded ? 28 : 24) : (grounded ? 34 : 13);
        velocity.x = Mathf.MoveTowards(velocity.x, desired, acceleration * delta);

        if (jumpBuffer > 0 && coyoteTime > 0)
            LaunchJump();

        Vector2 next = position + new Vector2(velocity.x * delta, 0);
        next.x = Mathf.Clamp(next.x, BodyHalfWidth, level.Width - BodyHalfWidth);
        float gravityScale = !jumpHeld && velocity.y > 0 ? 1.75f : 1;
        velocity.y = Mathf.Max(-16, velocity.y - Gravity * gravityScale * delta);
        next.y = position.y + velocity.y * delta;
        grounded = false;

        if (velocity.y <= 0)
        {
            float support = float.NegativeInfinity;
            foreach (Rect surface in level.Surfaces)
            {
                if (next.x + BodyHalfWidth * .77f <= surface.xMin ||
                    next.x - BodyHalfWidth * .77f >= surface.xMax)
                    continue;
                float top = surface.yMax;
                if (position.y >= top - .025f && next.y <= top)
                    support = Mathf.Max(support, top);
            }
            if (!float.IsNegativeInfinity(support))
            {
                next.y = support;
                velocity.y = 0;
                grounded = true;
            }
        }
        position = next;
    }

    private void LaunchJump()
    {
        velocity.y = JumpSpeed;
        jumpBuffer = 0;
        coyoteTime = 0;
        grounded = false;
        PlayCue("Jump");
        Burst(position + Vector2.up * .06f, level.Accent, 7);
    }

    private void MovePatrols(float delta)
    {
        foreach (Patrol patrol in enemies)
        {
            if (!patrol.Active)
                continue;
            float speed = 1.24f + stage * .31f + (patrol.Route.z > 0 ? .36f : 0);
            patrol.Position.x += patrol.Direction * speed * delta;
            if (patrol.Position.x <= patrol.Route.x || patrol.Position.x >= patrol.Route.y)
            {
                patrol.Position.x = Mathf.Clamp(patrol.Position.x, patrol.Route.x, patrol.Route.y);
                patrol.Direction = -patrol.Direction;
            }
        }
    }

    private bool HitHazard(Vector2 previous)
    {
        if (protection > 0)
            return false;

        Rect body = new Rect(position.x - BodyHalfWidth, position.y + .02f, BodyHalfWidth * 2, BodyHeight);
        foreach (Rect strip in level.Spikes)
        {
            // O centro da faixa define a área de contato, deixando as pontas visuais tolerantes.
            Rect contact = Rect.MinMaxRect(strip.xMin + .08f, strip.yMin, strip.xMax - .08f, strip.yMax * .82f);
            if (body.Overlaps(contact))
            {
                Damage(false);
                return true;
            }
        }

        foreach (Patrol patrol in enemies)
        {
            if (!patrol.Active)
                continue;
            Rect contact = new Rect(patrol.Position.x - .31f, patrol.Position.y, .62f, .5f);
            if (!body.Overlaps(contact))
                continue;

            if (velocity.y < -1 && previous.y >= contact.yMax - .08f)
            {
                patrol.Active = false;
                patrol.Visual.enabled = false;
                velocity.y = 6.6f;
                position.y = contact.yMax + .01f;
                score += 100;
                PlayCue("Enemy");
                Burst(patrol.Position + Vector2.up * .4f, new Color32(255, 130, 107, 255), 11);
            }
            else
            {
                Damage(false);
                return true;
            }
        }

        return false;
    }

    private void Damage(bool falling)
    {
        if (protection <= 0)
        {
            lives--;
            PlayCue("Hurt");
            Burst(position + Vector2.up * .43f, new Color32(255, 125, 111, 255), 13);
            if (lives == 0)
            {
                status = Status.GameOver;
                hurtTime = .35f;
                StopMusic();
                return;
            }

            string reason = falling ? "Queda detectada." : "Colisão detectada.";
            Announce(reason + " Retornando ao ponto seguro.", true);
            protection = 1.2f;
            hurtTime = .35f;
        }

        PlacePlayer(checkpoint >= 0 ? level.Checkpoints[checkpoint] : SpawnX);
        SnapCamera();
    }

    private void CollectCells()
    {
        Vector2 center = position + Vector2.up * .47f;
        foreach (Pickup pickup in pickups)
        {
            if (!pickup.Active || (pickup.Position - center).sqrMagnitude > .59f * .59f)
                continue;

            pickup.Active = false;
            pickup.Visual.enabled = false;
            pickup.Halo.enabled = false;
            collected++;
            totalCells++;
            score += 250;
            PlayCue("Collect");
            Burst(pickup.Position, new Color32(255, 222, 140, 255), 13);
            Announce(collected == level.Cores.Length ? "Circuito completo. Saída liberada." : "Célula de íon recuperada.");
        }
    }

    private void SaveCheckpoint()
    {
        for (int i = checkpoint + 1; i < level.Checkpoints.Length; i++)
        {
            if (Mathf.Abs(position.x - level.Checkpoints[i]) >= .52f || position.y < -.1f || position.y > .75f)
                continue;
            checkpoint = i;
            for (int j = 0; j < checkpointVisuals.Count; j++)
                checkpointVisuals[j].color = j == checkpoint ? Color.white : new Color(.68f, .82f, .88f, 1);
            PlayCue("Checkpoint");
            Burst(new Vector2(level.Checkpoints[i], .65f), level.Accent, 12);
            Announce("Ponto de retorno ativado.");
            return;
        }
    }

    private bool NearGate() => Mathf.Abs(position.x - level.GateX) < .92f && position.y < 1.15f && position.y > -.2f;

    private void ActivateGate()
    {
        if (collected != level.Cores.Length)
        {
            int remaining = level.Cores.Length - collected;
            Announce(remaining == 1 ? "Falta 1 célula para liberar a saída." : $"Faltam {remaining} células para liberar a saída.", true);
            return;
        }

        score += 500;
        status = Status.Transition;
        transitionTime = 1.18f;
        velocity = Vector2.zero;
        PlayCue("Clear");
        Burst(new Vector2(level.GateX, .86f), level.Accent, 22);
    }

    private void Announce(string message, bool warning = false)
    {
        feedback = message;
        feedbackWarning = warning;
        feedbackTime = 2.6f;
    }
}
