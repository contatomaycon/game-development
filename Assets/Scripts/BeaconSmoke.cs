#if DEBUG
using System;
using System.Collections;
using System.IO;
using UnityEngine;

internal sealed class BeaconSmoke : MonoBehaviour
{
    private BeaconGame game;
    private string captureFolder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartIfRequested()
    {
        if (!Debug.isDebugBuild)
            return;

        string[] arguments = Environment.GetCommandLineArgs();
        if (Array.IndexOf(arguments, "-farol-smoke") < 0)
            return;

        Application.runInBackground = true;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        BeaconGame game = FindAnyObjectByType<BeaconGame>();
        if (game == null)
        {
            Debug.LogError("FAROL_SMOKE_FAIL: componente BeaconGame ausente.");
            Application.Quit(1);
            return;
        }

        int captureFlag = Array.IndexOf(arguments, "-farol-captures");
        var runner = game.gameObject.AddComponent<BeaconSmoke>();
        runner.game = game;
        if (captureFlag >= 0 && captureFlag + 1 < arguments.Length)
            runner.captureFolder = arguments[captureFlag + 1];

        runner.StartCoroutine(runner.Execute());
    }

    private IEnumerator Execute()
    {
        IEnumerator verification = Verify();
        while (true)
        {
            bool next;
            object instruction;
            try
            {
                next = verification.MoveNext();
                instruction = next ? verification.Current : null;
            }
            catch (Exception error)
            {
                Debug.LogError("FAROL_SMOKE_FAIL: " + error);
                Application.Quit(1);
                yield break;
            }

            if (!next)
                yield break;
            yield return instruction;
        }
    }

    private IEnumerator Verify()
    {
        Assert(game.RunStatus == "Menu", "A cena de entrada não mostrou o menu.");
        yield return new WaitForEndOfFrame();
        Capture("01-menu");

        game.BeginSmokeTest();
        Assert(game.StageNumber == 1 && game.Lives == 4, "A missão não iniciou com quatro vidas no primeiro setor.");
        Climb(7.4f, 2.72f);
        Debug.Log("FAROL_SMOKE_STAGE: Docas de Serviço.");
        yield return null;
        yield return new WaitForEndOfFrame();
        Capture("02-docas-de-servico");
        Jump(12.5f, 18.55f);
        Jump(19.62f, 24.8f);
        Jump(24.83f, 31.6f);
        EndStage(2);

        Climb(7.5f, 2.7f);
        Jump(8.7f, 18.9f);
        Climb(19, 3.3f);
        Debug.Log("FAROL_SMOKE_STAGE: Estação de Bombeamento.");
        yield return null;
        yield return new WaitForEndOfFrame();
        Capture("03-estacao-de-bombeamento");
        Jump(21.24f, 29.45f);
        Jump(29.48f, 35.25f);
        Jump(35.25f, 39.28f, 3);
        Jump(39.22f, 41.8f, 0);
        EndStage(3);

        Climb(5.6f, 2.9f);
        Jump(8.25f, 17.5f, 10);
        Climb(17.5f, 3.5f);
        Jump(20.14f, 29.4f, 11);
        Jump(29.14f, 32.7f, 2);
        Climb(32.7f, 4.2f);
        Debug.Log("FAROL_SMOKE_STAGE: Núcleo do Farol.");
        yield return null;
        yield return new WaitForEndOfFrame();
        Capture("04-nucleo-do-farol");
        Jump(33.6f, 40.2f);
        Jump(40.18f, 46.55f, 11);
        Walk(47.6f);
        Jump(48.1f, 51.1f, 0);
        EndStage(3);
        Assert(game.RunStatus == "Victory" && game.Score == 3850 && game.Lives == 4,
            "A rota não concluiu a campanha com 9 células, 3.850 pontos e 4 vidas.");
        Debug.Log("FAROL_CAMPAIGN_PASS: 3 setores, 9 células, 4 vidas, 3850 pontos.");
        yield return null;
        yield return new WaitForEndOfFrame();
        Capture("05-vitoria");

        game.BeginSmokeTest();
        FallIntoGap();
        Assert(game.Lives == 3 && Mathf.Abs(game.PlayerPosition.x - 1.4f) < .025f,
            "A primeira queda não consumiu uma vida e não retornou ao início.");
        yield return null;
        yield return new WaitForEndOfFrame();
        Capture("06-retorno-apos-queda");
        FallIntoGap();
        FallIntoGap();
        FallIntoGap();
        Assert(game.RunStatus == "GameOver" && game.Lives == 0, "O fim de jogo não apareceu após a quarta queda.");
        yield return null;
        yield return new WaitForEndOfFrame();
        Capture("07-fim-de-jogo");

        game.BeginSmokeTest();
        Assert(game.RunStatus == "Playing" && game.StageNumber == 1 && game.Lives == 4 && game.Score == 0,
            "O reinício não limpou a pontuação e não restaurou as quatro vidas.");
        Debug.Log("FAROL_SMOKE_PASS: campanha, quedas, fim de jogo e reinício confirmados.");
        yield return null;
        Application.Quit(0);
    }

    private void Tick(float x = 0, float y = 0, bool run = false, bool jump = false, bool use = false) =>
        game.AdvanceSmokeFrame(x, y, run, jump, use);

    private void Walk(float target)
    {
        for (int i = 0; i < 170; i++)
        {
            float error = target - game.PlayerPosition.x;
            if (Mathf.Abs(error) < .075f && Mathf.Abs(game.PlayerVelocity.x) < .14f && game.Grounded)
                return;
            Tick(Mathf.Clamp(error * 3 - game.PlayerVelocity.x * .2f, -1, 1));
        }
        Assert(false, $"Não atingiu a coordenada x={target}.");
    }

    private void Climb(float x, float y)
    {
        Walk(x);
        for (int i = 0; i < 90; i++)
        {
            if (game.PlayerPosition.y >= y - .01f && game.Grounded)
                return;
            Tick(y: 1);
        }
        Assert(false, $"Não subiu a escada em x={x}.");
    }

    private void Jump(float start, float target, int accelerate = 8)
    {
        int lives = game.Lives;
        Walk(start);
        for (int i = 0; i < accelerate; i++)
            Tick(1, run: true);
        for (int i = 0; i < 85 && (i <= 8 || !game.Grounded); i++)
        {
            float error = target - game.PlayerPosition.x;
            Tick(Mathf.Clamp(error * 2.8f - game.PlayerVelocity.x * .45f, -1, 1), run: true, jump: true);
            Assert(game.Lives == lives, $"Dano no salto de x={start} a x={target}.");
        }
        Walk(target);
    }

    private void EndStage(int expectedStage)
    {
        int oldStage = game.StageNumber;
        Assert(game.LevelCells == game.RequiredCells, "Células faltando antes da comporta.");
        Walk(game.GoalPosition.x);
        Tick(use: true);
        Assert(game.RunStatus == "Transition", "A comporta não abriu.");
        for (int i = 0; i < 62; i++)
            Tick();
        Assert(game.StageNumber == expectedStage && (game.StageNumber != oldStage || game.RunStatus == "Victory"),
            "A transição de setor não terminou.");
    }

    private void FallIntoGap()
    {
        int lives = game.Lives;
        for (int i = 0; i < 450 && game.Lives == lives; i++)
            Tick(game.PlayerPosition.x > 15.15f ? 0 : .9f);
        Assert(game.Lives == lives - 1, "A queda não retirou uma vida.");
    }

    private void Capture(string name)
    {
        if (string.IsNullOrEmpty(captureFolder))
            return;

        Directory.CreateDirectory(captureFolder);
        Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
        try
        {
            Assert(image != null && image.width >= 640 && image.height >= 360,
                "A captura do quadro não contém a resolução mínima.");
            File.WriteAllBytes(Path.Combine(captureFolder, name + ".png"), image.EncodeToPNG());
        }
        finally
        {
            Destroy(image);
        }
    }

    private void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"{message} Setor {game.StageNumber}, posição {game.PlayerPosition}, vidas {game.Lives}.");
    }
}
#endif
