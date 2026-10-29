// BloodCellField.cs — globulos vermelhos boiando pela corrente sanguinea.
// Reaproveita as mesmas instancias: quando um globulo sai do alcance da camera
// ele reaparece do lado oposto. Nunca instancia/libera nada durante o jogo.
using Godot;
using System.Collections.Generic;

public partial class BloodCellField : Node2D
{
	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	private const string TEX_PATH     = "res://Assets/Sprites/globulo.png";
	private const int    COUNT_BACK   = 55;    // globulos atras dos personagens
	private const int    COUNT_FRONT  = 6;     // alguns na frente = sensacao de profundidade
	private const float  FIELD_RADIUS = 1500f; // raio em volta da camera
	private const float  BASE_SCALE   = 0.10f; // AJUSTE conforme o tamanho do seu png
	private const float  SCALE_VAR    = 0.45f; // variacao de tamanho (+/-)
	private const float  ALPHA_MIN    = 0.16f;
	private const float  ALPHA_MAX    = 0.38f;
	private const float  ALPHA_FRONT  = 0.12f; // os da frente sao bem apagados
	private const float  DRIFT_SPEED  = 20f;   // velocidade da corrente
	private const float  DRIFT_VAR    = 12f;   // variacao por globulo
	private const float  CURRENT_TURN = 0.08f; // o quanto a corrente muda de direcao
	private static readonly Color TINT = new Color(0.85f, 0.62f, 0.60f);
	// ──────────────────────────────────────────────────────────────────────

	private class Cell
	{
		public Sprite2D Node;
		public Vector2  Vel;
		public float    Spin;
		public bool     Front;
	}

	private readonly List<Cell> _cells = new List<Cell>();
	private Node2D  _back;
	private Node2D  _front;
	private float   _currentAngle;
	private Vector2 _lastCenter = Vector2.Zero;

	public override void _Ready()
	{
		var tex = GD.Load<Texture2D>(TEX_PATH);
		if (tex == null)
		{
			GD.PrintErr($"[BloodCellField] textura nao encontrada: {TEX_PATH}");
			return;
		}

		_back  = new Node2D { Name = "Back",  ZIndex = -9 };
		_front = new Node2D { Name = "Front", ZIndex = 5  };
		AddChild(_back);
		AddChild(_front);

		_currentAngle = (float)GD.RandRange(0.0, Mathf.Tau);
		_lastCenter   = GetCameraCenter();

		for (int i = 0; i < COUNT_BACK;  i++) _cells.Add(MakeCell(tex, false));
		for (int i = 0; i < COUNT_FRONT; i++) _cells.Add(MakeCell(tex, true));

		GD.Print($"[BloodCellField] {_cells.Count} globulos criados");
	}

	private Cell MakeCell(Texture2D tex, bool front)
	{
		float scale = BASE_SCALE * (1f + (float)GD.RandRange(-SCALE_VAR, SCALE_VAR));
		if (front) scale *= 1.6f;

		float alpha = front
			? ALPHA_FRONT
			: (float)GD.RandRange(ALPHA_MIN, ALPHA_MAX);

		var sprite = new Sprite2D
		{
			Texture  = tex,
			Scale    = new Vector2(scale, scale),
			Rotation = (float)GD.RandRange(0.0, Mathf.Tau),
			Modulate = new Color(TINT.R, TINT.G, TINT.B, alpha),
			Position = RandomPointInField(GetCameraCenter())
		};

		(front ? _front : _back).AddChild(sprite);

		return new Cell
		{
			Node  = sprite,
			Vel   = new Vector2(
						(float)GD.RandRange(-DRIFT_VAR, DRIFT_VAR),
						(float)GD.RandRange(-DRIFT_VAR, DRIFT_VAR)),
			Spin  = (float)GD.RandRange(-0.35, 0.35),
			Front = front
		};
	}

	private Vector2 GetCameraCenter()
	{
		var cam = GetViewport()?.GetCamera2D();
		return cam != null ? cam.GetScreenCenterPosition() : _lastCenter;
	}

	private Vector2 RandomPointInField(Vector2 center)
	{
		float a = (float)GD.RandRange(0.0, Mathf.Tau);
		float r = FIELD_RADIUS * Mathf.Sqrt((float)GD.Randf());
		return center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
	}

	public override void _Process(double delta)
	{
		if (_cells.Count == 0) return;

		float d = (float)delta;

		// a corrente muda de direcao bem devagar
		_currentAngle += Mathf.Sin(Time.GetTicksMsec() / 4000f) * CURRENT_TURN * d;
		Vector2 current = new Vector2(Mathf.Cos(_currentAngle), Mathf.Sin(_currentAngle)) * DRIFT_SPEED;

		Vector2 center = GetCameraCenter();
		_lastCenter = center;

		foreach (var cell in _cells)
		{
			if (!IsInstanceValid(cell.Node)) continue;

			// os da frente andam mais rapido — parallax barato
			float speedMult = cell.Front ? 1.8f : 1f;

			cell.Node.Position += (current + cell.Vel) * speedMult * d;
			cell.Node.Rotation += cell.Spin * d;

			// saiu do alcance? reaparece do lado oposto
			Vector2 off = cell.Node.Position - center;
			if (off.Length() > FIELD_RADIUS)
			{
				Vector2 dir  = off.Normalized();
				Vector2 perp = new Vector2(-dir.Y, dir.X);
				cell.Node.Position = center - dir * FIELD_RADIUS * 0.98f
								   + perp * (float)GD.RandRange(-FIELD_RADIUS * 0.7, FIELD_RADIUS * 0.7);
			}
		}
	}
}
