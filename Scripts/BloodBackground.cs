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

	public override void _Draw()
	{
		if (_bgTexture == null)
		{
			DrawRect(new Rect2(-AREA, -AREA, AREA * 2, AREA * 2),
				new Color(0.42f, 0.06f, 0.10f));
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
