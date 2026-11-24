using UnityEngine;

public sealed partial class BeaconGame
{
    private void OnGUI()
    {
        if (level == null)
            return;

        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        float width = Screen.width / scale;
        float height = Screen.height / scale;
        Matrix4x4 previousMatrix = GUI.matrix;
        int previousDepth = GUI.depth;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
        GUI.depth = -30;

        if (status == Status.Menu)
        {
            DrawMenu(width, height);
        }
        else
        {
            DrawHud(width, height);
            if (status == Status.Transition)
                DrawTransition(width, height);
            if (status == Status.GameOver || status == Status.Victory)
                DrawSummary(width, height);
        }

        GUI.depth = previousDepth;
        GUI.matrix = previousMatrix;
    }

    private void DrawMenu(float width, float height)
    {
        Box(0, 0, width, height, new Color32(10, 18, 34, 100));
        const float cardWidth = 588;
        float margin = width * .057f;
        float y = (height - 490) / 2;
        Box(margin, y, cardWidth, 490, Panel);
        Box(margin, y, cardWidth, 1, Border);
        Box(margin, y, 4, 490, level.Accent);
        Text("MANUTENÇÃO ORBITAL", margin + 36, y + 32, cardWidth - 72, 22, 17, level.Accent, true);
        Text("Farol de Íon", margin + 33, y + 65, cardWidth - 68, 87, 69, Ink, true);
        Text("A luz da estação se apagou.", margin + 36, y + 171, cardWidth - 72, 24, 23, Ink, true);
        Text("Guie Nilo por três setores, recupere as células de íon e religue o farol que orienta as naves da região.",
            margin + 36, y + 206, cardWidth - 75, 57, 20, Muted, wrap: true);
        if (Button("INICIAR MISSÃO", margin + 36, y + 290, 228, 56))
        {
            StartRun();
            return;
        }
        Text("Enter", margin + 283, y + 302, 120, 32, 17, Muted);
        Box(margin + 36, y + 375, cardWidth - 72, 1, Border);
        Text("A/D mover    Shift correr    Espaço pular", margin + 36, y + 390, cardWidth - 72, 25, 17, Muted);
        Text("W/S escalar    E acessar a saída    R reiniciar", margin + 36, y + 423, cardWidth - 72, 25, 17, Muted);
    }

    private void DrawHud(float width, float height)
    {
        Box(0, 0, width, 78, Panel);
        Box(0, 77, width, 1, Border);
        Box(20, 17, 3, 46, level.Accent);
        Text("FAROL DE ÍON", 34, 13, 312, 21, 15, level.Accent, true);
        Text(level.Name, 34, 35, 374, 26, 23, Ink, true);
        Text($"SETOR {StageNumber} / {BeaconLevel.Campaign.Length}", 408, 18, 170, 20, 15, Muted, true);
        for (int i = 0; i < BeaconLevel.Campaign.Length; i++)
            Box(408 + i * 36, 49, 29, 4, i <= stage ? level.Accent : Border);

        Stat(width - 405, "PONTOS", score.ToString("0000"), Ink);
        Stat(width - 268, "VIDAS", lives.ToString(), lives <= 1 ? Coral : Ink);
        Stat(width - 131, "CÉLULAS", $"{collected}/{RequiredCells}", collected == RequiredCells ? level.Accent : Warm);

        Box(0, height - 54, width, 54, Panel);
        Box(0, height - 54, width, 1, Border);
        Text("A/D mover   •   Shift correr   •   Espaço pular   •   W/S escalar   •   R reiniciar",
            22, height - 45, width - 44, 34, 16, Muted);
        string goal = collected == RequiredCells ? "E acessar a saída" : "Recupere as células de íon";
        Text(goal, width - 370, height - 45, 348, 34, 17,
            collected == RequiredCells ? level.Accent : Warm, true, TextAnchor.MiddleRight);

        if (status != Status.Playing)
            return;

        if (bannerTime > 0)
        {
            float hintWidth = 525;
            float x = (width - hintWidth) / 2;
            Box(x, 96, hintWidth, 43, Panel);
            Box(x, 138, hintWidth, 1, Border);
            Text(level.Hint, x, 96, hintWidth, 43, 17, Ink, false, TextAnchor.MiddleCenter);
        }

        if (feedbackTime > 0)
        {
            float feedbackWidth = 542;
            float x = (width - feedbackWidth) / 2;
            Box(x, height - 115, feedbackWidth, 40, Panel);
            Box(x, height - 115, feedbackWidth, 1, feedbackWarning ? Coral : Border);
            Text(feedback, x + 10, height - 114, feedbackWidth - 20, 38, 17,
                feedbackWarning ? Coral : Ink, true, TextAnchor.MiddleCenter);
        }

        if (NearGate())
        {
            const float hintWidth = 460;
            float x = (width - hintWidth) / 2;
            Box(x, height - 183, hintWidth, 45, Panel);
            Text(collected == RequiredCells ? "E   Abrir comporta" : "Saída bloqueada • faltam células",
                x, height - 183, hintWidth, 45, 18, collected == RequiredCells ? level.Accent : Warm,
                true, TextAnchor.MiddleCenter);
        }
    }

    private void DrawTransition(float width, float height)
    {
        Box(0, 0, width, height, new Color32(9, 15, 29, 173));
        const float cardWidth = 548;
        float x = (width - cardWidth) / 2;
        float y = (height - 147) / 2;
        Box(x, y, cardWidth, 147, Panel);
        Box(x, y, cardWidth, 3, level.Accent);
        Text("SETOR RESTAURADO", x, y + 23, cardWidth, 32, 19, level.Accent, true, TextAnchor.MiddleCenter);
        Text(level.Name, x, y + 67, cardWidth, 54, 36, Ink, true, TextAnchor.MiddleCenter);
    }

    private void DrawSummary(float width, float height)
    {
        bool victory = status == Status.Victory;
        Color accent = victory ? level.Accent : Coral;
        Box(0, 0, width, height, new Color32(9, 15, 29, 191));
        const float cardWidth = 605;
        float x = (width - cardWidth) / 2;
        float y = (height - 398) / 2;
        Box(x, y, cardWidth, 398, Panel);
        Box(x, y, cardWidth, 4, accent);
        Text(victory ? "SINAL EM OPERAÇÃO" : "ENERGIA ESGOTADA", x + 37, y + 39, cardWidth - 74, 25, 17, accent, true);
        Text(victory ? "Farol restaurado" : "Missão interrompida", x + 36, y + 79, cardWidth - 72, 63, 48, Ink, true);
        Text(victory ? "As naves voltaram a receber o sinal da estação." :
            "Nilo ficou sem energia. Reinicie para retomar a missão.", x + 37, y + 156, cardWidth - 74, 54, 20, Muted, wrap: true);
        Box(x + 37, y + 227, cardWidth - 74, 1, Border);
        Text($"Pontos {score:0000}    Células {totalCells}", x + 37, y + 245, cardWidth - 74, 36, 22, Ink, true);
        if (Button(victory ? "JOGAR DE NOVO" : "TENTAR DE NOVO", x + 37, y + 307, 226, 54))
            StartRun();
        Text("Enter ou R", x + 281, y + 316, 211, 37, 17, Muted);
    }

    private void Stat(float x, string title, string value, Color color)
    {
        Box(x - 12, 19, 1, 42, Border);
        Text(title, x, 15, 116, 21, 13, Muted, true);
        Text(value, x, 35, 116, 29, 24, color, true);
    }

    private static void Box(float x, float y, float width, float height, Color color)
    {
        Color before = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        GUI.color = before;
    }

    private void Text(string content, float x, float y, float width, float height, int size,
        Color color, bool bold = false, TextAnchor alignment = TextAnchor.MiddleLeft, bool wrap = false)
    {
        int key = size * 64 + (int)alignment * 4 + (bold ? 2 : 0) + (wrap ? 1 : 0);
        if (!labelStyles.TryGetValue(key, out GUIStyle style))
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                alignment = alignment,
                wordWrap = wrap,
                padding = new RectOffset(0, 0, 0, 0)
            };
            labelStyles.Add(key, style);
        }
        style.normal.textColor = color;
        GUI.Label(new Rect(x, y, width, height), content, style);
    }

    private bool Button(string content, float x, float y, float width, float height)
    {
        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            buttonStyle.normal.textColor = BackgroundColor;
            buttonStyle.hover.textColor = BackgroundColor;
            buttonStyle.active.textColor = BackgroundColor;
        }

        Rect rect = new Rect(x, y, width, height);
        Color color = rect.Contains(Event.current.mousePosition) ? Color.Lerp(level.Accent, Ink, .35f) : level.Accent;
        Box(x, y, width, height, color);
        return GUI.Button(rect, content, buttonStyle);
    }
}
