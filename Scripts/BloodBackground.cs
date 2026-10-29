// BloodBackground.cs — tecido mais escuro e mais saturado (vermelho profundo)
using Godot;

public partial class BloodBackground : Node2D
{
	private Texture2D _bgTexture;
	private const float AREA      = 6000f;
	private const float TILE_SIZE = 256f;   // aumente para 320/384 se quiser celulas maiores

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	// Fase 1 (0-10 min): tecido saudavel, porem escuro
	private static readonly Color TINT_SAUDAVEL = new Color(0.78f, 0.62f, 0.50f);
	// Fase 2 (10-20 min): arroxeado
	private static readonly Color TINT_INFECTADO = new Color(0.55f, 0.42f, 0.62f);
	// Fase 3 (20-30 min): critico
	private static readonly Color TINT_CRITICO = new Color(0.20f, 0.15f, 0.24f);
	// ──────────────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		ZIndex     = -10;
		Position   = Vector2.Zero;
		// a fase decide qual tecido aparece e o quanto a cor e lavada
		Modulate   = GamePhase.Data.GameTint;
		_bgTexture = GD.Load<Texture2D>(GamePhase.Data.GameBackground);

		if (_bgTexture == null)
			GD.PrintErr($"[BloodBackground] textura nao encontrada: {GamePhase.Data.GameBackground}");
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;

		float t = GameManager.Instance.ElapsedTime;
		Color target;

		Color baseTint = GamePhase.Data.GameTint;

		if (t < 600f)
		{
			target = baseTint; // 0-10 min: primeiros sinais
		}
		else if (t < 1200f)
		{
			float p = (t - 600f) / 600f;
			target = baseTint.Lerp(TINT_INFECTADO, p); // 10-20 min: arroxeado
		}
		else
		{
			float p = Mathf.Min((t - 1200f) / 600f, 1f);
			target = TINT_INFECTADO.Lerp(TINT_CRITICO, p); // 20-30 min: critico
		}

		Modulate = Modulate.Lerp(target, 2f * (float)delta);
	}

	public override void _Draw()
	{
		if (_bgTexture == null)
		{
			DrawRect(new Rect2(-AREA, -AREA, AREA * 2, AREA * 2), new Color(0.30f, 0.04f, 0.06f));
			return;
		}

		int   count = (int)(AREA * 2 / TILE_SIZE) + 2;
		float start = -AREA - TILE_SIZE;

		for (int x = 0; x <= count; x++)
			for (int y = 0; y <= count; y++)
				DrawTextureRect(
					_bgTexture,
					new Rect2(new Vector2(start + x * TILE_SIZE, start + y * TILE_SIZE),
							  new Vector2(TILE_SIZE, TILE_SIZE)),
					false
				);
	}
}
