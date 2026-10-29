// DisplaySettings.cs — resolucao e tela cheia num lugar so.
// Chame DisplaySettings.Apply() no boot pra restaurar o que estava salvo.
using Godot;

public static class DisplaySettings
{
	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	// A ordem importa: o indice salvo aponta pra esta lista.
	public static readonly Vector2I[] RESOLUTIONS =
	{
		new Vector2I(1280,  720),
		new Vector2I(1366,  768),
		new Vector2I(1600,  900),
		new Vector2I(1920, 1080),
		new Vector2I(2560, 1440),
	};
	public const int DEFAULT_INDEX = 3; // 1920x1080
	// ──────────────────────────────────────────────────────────────────────

	public static string Label(int index)
	{
		var r = Get(index);
		return $"{r.X} x {r.Y}";
	}

	public static Vector2I Get(int index)
	{
		if (index < 0 || index >= RESOLUTIONS.Length) index = DEFAULT_INDEX;
		return RESOLUTIONS[index];
	}

	// Restaura o que estava salvo. Chamar no _Ready do GameManager (autoload).
	public static void Apply()
	{
		SetFullscreen(SaveData.Fullscreen, save: false);

		if (!SaveData.Fullscreen)
			SetResolution(SaveData.ResolutionIndex, save: false);

		GD.Print($"[DisplaySettings] {Label(SaveData.ResolutionIndex)} " +
				 $"fullscreen={SaveData.Fullscreen}");
	}

	public static void SetResolution(int index, bool save = true)
	{
		var res = Get(index);

		// em tela cheia o tamanho da janela e o do monitor — so guarda a escolha
		if (!SaveData.Fullscreen)
		{
			DisplayServer.WindowSetSize(res);
			CenterWindow();
		}

		if (save) SaveData.SetResolutionIndex(index);
	}

	public static void SetFullscreen(bool on, bool save = true)
	{
		DisplayServer.WindowSetMode(on
			? DisplayServer.WindowMode.Fullscreen
			: DisplayServer.WindowMode.Windowed);

		if (save) SaveData.SetFullscreen(on);

		// ao voltar pra janela, reaplica a resolucao escolhida
		if (!on)
		{
			DisplayServer.WindowSetSize(Get(SaveData.ResolutionIndex));
			CenterWindow();
		}
	}

	private static void CenterWindow()
	{
		int screen = DisplayServer.WindowGetCurrentScreen();
		Vector2I screenSize = DisplayServer.ScreenGetSize(screen);
		Vector2I screenPos  = DisplayServer.ScreenGetPosition(screen);
		Vector2I winSize    = DisplayServer.WindowGetSize();

		DisplayServer.WindowSetPosition(screenPos + (screenSize - winSize) / 2);
	}

	// Indice da resolucao mais proxima do tamanho atual da janela,
	// usado pra iniciar o OptionButton no valor certo.
	public static int CurrentIndex()
	{
		int idx = SaveData.ResolutionIndex;
		if (idx >= 0 && idx < RESOLUTIONS.Length) return idx;
		return DEFAULT_INDEX;
	}
}
