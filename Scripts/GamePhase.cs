// GamePhase.cs — a fase selecionada manda no tema do menu, no mapa e no fundo.
// Tudo que muda entre fases mora aqui, em vez de espalhar "if fase == 2" por ai.
using Godot;

public class PhaseData
{
	public int    Number;
	public string Name;
	public string MenuBackground;   // arte do menu
	public string GameBackground;   // textura do tecido no jogo
	public Color  GameTint;         // "lavagem" aplicada na textura do jogo
	public bool   HasVeinWalls;     // paredes desenhadas com Line2D (fase 1)
	public bool   HasLungWalls;     // silhueta de pulmao gerada por codigo
	public bool   HasMissions;
	public bool   HasBloodCells;    // globulos vermelhos boiando
	public bool   HasVeinOverlay;   // capilares vermelhos por cima da tela
	public bool   ShowTileMap;      // o TileMapLayer que existe na cena

	// cores da parede no jogo
	public Color  WallDeep, WallEdge, WallRim;

	// minimapa
	public Color  MapBg, MapRevealed;
	public bool   ShowMinimapVeins;   // desenhar os tracos das veias no mapa

	// paleta do menu
	public Color Line, LineOn, LineOff, Fill, FillOn, Panel, Accent;
}

public static class GamePhase
{
	/// Disparado quando o jogador troca de fase no loadout.
	public static event System.Action Changed;

	public const int MAX_PHASE = 2;

	public static int Current { get; private set; } = 1;

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	private static readonly PhaseData[] _phases =
	{
		new PhaseData
		{
			Number         = 1,
			Name           = "SISTEMA CIRCULATORIO",
			MenuBackground = "res://Assets/fundo_menu.png",
			GameBackground = "res://Assets/fundo.png",
			GameTint       = new Color(0.78f, 0.62f, 0.50f),
			HasVeinWalls   = true,
			HasLungWalls   = false,
			HasMissions    = true,
			HasBloodCells  = true,
			HasVeinOverlay = true,
			ShowTileMap    = true,

			WallDeep = new Color(0.30f, 0.020f, 0.030f),
			WallEdge = new Color(0.85f, 0.120f, 0.110f),
			WallRim  = new Color(1.00f, 0.320f, 0.180f),

			MapBg            = new Color(0.18f, 0.04f, 0.06f),
			MapRevealed      = new Color(0.75f, 0.25f, 0.30f),
			ShowMinimapVeins = true,

			Line    = new Color(0.90f, 0.16f, 0.22f),
			LineOn  = new Color(1.00f, 0.42f, 0.42f),
			LineOff = new Color(0.45f, 0.20f, 0.22f),
			Fill    = new Color(0.16f, 0.02f, 0.05f, 0.72f),
			FillOn  = new Color(0.42f, 0.05f, 0.09f, 0.85f),
			Panel   = new Color(0.10f, 0.02f, 0.04f, 0.96f),
			Accent  = new Color(0.30f, 0.95f, 0.80f),
		},
		new PhaseData
		{
			Number         = 2,
			Name           = "PULMOES",
			MenuBackground = "res://Assets/fundo_2.png",
			GameBackground = "res://Assets/pulmao.png",
			// o asset e roxo forte demais: isto lava a cor e escurece bastante
			GameTint       = new Color(0.42f, 0.30f, 0.52f),
			HasVeinWalls   = false,
			HasLungWalls   = true,    // a silhueta do pulmao vira a parede
			HasMissions    = false,   // fase 2 ainda sem missoes
			HasBloodCells  = false,   // nada de globulo vermelho no pulmao
			HasVeinOverlay = false,   // nem os capilares vermelhos
			ShowTileMap    = false,   // o tilemap da cena e da fase 1

			WallDeep = new Color(0.14f, 0.04f, 0.26f),
			WallEdge = new Color(0.52f, 0.20f, 0.85f),
			WallRim  = new Color(0.78f, 0.45f, 1.00f),

			MapBg            = new Color(0.11f, 0.04f, 0.20f),
			MapRevealed      = new Color(0.58f, 0.34f, 0.85f),
			// os "tracos" no pulmao seriam as espinhas dos lobos: enormes e
			// sem sentido no mapa, entao ficam de fora
			ShowMinimapVeins = false,

			Line    = new Color(0.62f, 0.26f, 0.92f),
			LineOn  = new Color(0.80f, 0.55f, 1.00f),
			LineOff = new Color(0.30f, 0.18f, 0.42f),
			Fill    = new Color(0.09f, 0.03f, 0.16f, 0.72f),
			FillOn  = new Color(0.24f, 0.08f, 0.40f, 0.85f),
			Panel   = new Color(0.07f, 0.02f, 0.13f, 0.96f),
			Accent  = new Color(0.45f, 0.90f, 1.00f),
		},
	};
	// ──────────────────────────────────────────────────────────────────────

	public static PhaseData Data => Get(Current);

	public static PhaseData Get(int number)
	{
		int i = Mathf.Clamp(number - 1, 0, _phases.Length - 1);
		return _phases[i];
	}

	/// A fase 1 esta sempre liberada; as outras dependem do save.
	public static bool IsUnlocked(int number)
	{
		if (number <= 1) return true;
		return SaveData.UnlockedPhases.Contains(number);
	}

	public static void Unlock(int number)
	{
		if (IsUnlocked(number)) return;
		SaveData.UnlockPhase(number);
		GD.Print($"[GamePhase] fase {number} liberada");
	}

	public static void Select(int number)
	{
		number = Mathf.Clamp(number, 1, MAX_PHASE);
		if (!IsUnlocked(number) || number == Current) return;

		Current = number;
		GD.Print($"[GamePhase] fase {Current} — {Data.Name}");
		Changed?.Invoke();
	}
}
