using UnityEngine;

internal sealed class BeaconLevel
{
    internal readonly string Name;
    internal readonly string Hint;
    internal readonly float Width;
    internal readonly Color Accent;
    internal readonly Rect[] Surfaces;
    internal readonly Vector3[] Ladders;
    internal readonly Vector2[] Cores;
    internal readonly Rect[] Spikes;
    internal readonly Vector3[] Patrols;
    internal readonly float[] Checkpoints;
    internal readonly float GateX;

    internal BeaconLevel(
        string name, string hint, float width, Color accent,
        Rect[] surfaces, Vector3[] ladders, Vector2[] cores, Rect[] spikes,
        Vector3[] patrols, float[] checkpoints, float gateX)
    {
        Name = name;
        Hint = hint;
        Width = width;
        Accent = accent;
        Surfaces = surfaces;
        Ladders = ladders;
        Cores = cores;
        Spikes = spikes;
        Patrols = patrols;
        Checkpoints = checkpoints;
        GateX = gateX;
    }

    // A plataforma é definida pelas bordas horizontais e pela altura superior.
    private static Rect Platform(float left, float right, float top, float depth = .34f) =>
        new Rect(left, top - depth, right - left, depth);

    private static Rect SpikeStrip(float left, float right) => new Rect(left, 0, right - left, .36f);

    internal static readonly BeaconLevel[] Campaign =
    {
        new BeaconLevel(
            "Docas de Serviço", "Suba pela escada para recolher a primeira célula.",
            36, new Color32(60, 224, 198, 255),
            new[] { Platform(0, 14, 0, 1.25f), Platform(16.7f, 36, 0, 1.25f), Platform(5, 11, 2.72f) },
            new[] { new Vector3(7.4f, 0, 2.72f) },
            new[] { new Vector2(7.4f, 3.18f), new Vector2(24.8f, .48f) },
            new[] { SpikeStrip(20.8f, 22) },
            new[] { new Vector3(26, 29.7f, 0) },
            new[] { 18.5f },
            34.4f
        ),
        new BeaconLevel(
            "Estação de Bombeamento", "Combine corrida e salto para cruzar as tubulações.",
            43, new Color32(111, 187, 255, 255),
            new[]
            {
                Platform(0, 10.5f, 0, 1.25f), Platform(13.2f, 23.8f, 0, 1.25f),
                Platform(27.1f, 43, 0, 1.25f), Platform(5.9f, 9.8f, 2.7f), Platform(16.5f, 22.2f, 3.3f)
            },
            new[] { new Vector3(7.5f, 0, 2.7f), new Vector3(19, 0, 3.3f) },
            new[] { new Vector2(7.5f, 3.15f), new Vector2(19, 3.77f), new Vector2(35.3f, .48f) },
            new[] { SpikeStrip(14.6f, 15.8f), SpikeStrip(31.1f, 32.9f), SpikeStrip(39.8f, 41f) },
            new[] { new Vector3(16.3f, 18.15f, 0), new Vector3(36, 38.8f, 0) },
            new[] { 28.6f },
            41.8f
        ),
        new BeaconLevel(
            "Núcleo do Farol", "Salte perto da borda e pouse além dos espinhos.",
            52, new Color32(245, 181, 93, 255),
            new[]
            {
                Platform(0, 9.5f, 0, 1.25f), Platform(12.5f, 21.5f, 0, 1.25f),
                Platform(25.6f, 34.6f, 0, 1.25f), Platform(38.9f, 52, 0, 1.25f),
                Platform(3.6f, 8.8f, 2.9f), Platform(16.5f, 20.7f, 3.5f), Platform(29.3f, 34.4f, 4.2f)
            },
            new[] { new Vector3(5.6f, 0, 2.9f), new Vector3(17.5f, 0, 3.5f), new Vector3(32.7f, 0, 4.2f) },
            new[]
            {
                new Vector2(5.6f, 3.36f), new Vector2(17.5f, 3.96f),
                new Vector2(32.7f, 4.66f), new Vector2(47.5f, .48f)
            },
            new[] { SpikeStrip(6.85f, 8.1f), SpikeStrip(15f, 16.25f), SpikeStrip(27.15f, 28.65f), SpikeStrip(44.1f, 45.4f), SpikeStrip(49f, 50f) },
            new[]
            {
                new Vector3(13f, 14.45f, 0), new Vector3(29.85f, 31.25f, 0),
                new Vector3(30.15f, 31.65f, 4.2f), new Vector3(41f, 43.25f, 0)
            },
            new[] { 26.4f, 40.2f },
            51
        )
    };
}
