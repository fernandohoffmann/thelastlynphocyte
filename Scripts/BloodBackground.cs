using Godot;

public partial class BloodBackground : Node2D
{
	private Texture2D _bgTexture;
	private const float AREA      = 6000f;
	private const float TILE_SIZE = 256f;

	public override void _Ready()
	{
		ZIndex     = -10;
		Position   = Vector2.Zero;
		_bgTexture = GD.Load<Texture2D>("res://Assets/fundo.png");
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;

		float t = GameManager.Instance.ElapsedTime;
		Color target;

		if (t < 600f)
		{
			target = Colors.White; // 0-10 min: primeiros sinais
		}
		else if (t < 1200f)
		{
			float p = (t - 600f) / 600f;
			target = Colors.White.Lerp(new Color(0.75f, 0.55f, 0.8f), p); // 10-20 min: arroxeado
		}
		else
		{
			float p = Mathf.Min((t - 1200f) / 600f, 1f);
			target = new Color(0.75f, 0.55f, 0.8f).Lerp(new Color(0.28f, 0.2f, 0.3f), p); // 20-30 min: critico
		}

		Modulate = Modulate.Lerp(target, 2f * (float)delta);
	}

	public override void _Draw()
	{
		if (_bgTexture == null)
		{
			DrawRect(new Rect2(-AREA, -AREA, AREA * 2, AREA * 2), new Color(0.42f, 0.06f, 0.10f));
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
