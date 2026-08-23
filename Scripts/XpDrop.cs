using Godot;

public enum XpDropType { Pequeno, Medio, Grande, Raro }

public partial class XpDrop : Area2D
{
	public int XpValue { get; private set; } = 5;
	private XpDropType _type;
	private float _lifetime  = 60f;
	private float _age       = 0f;
	private float _bobTimer  = 0f;
	private bool  _attracted = false; // sendo atraído pelo player

	private static readonly Color[] DropColors = {
		new Color(0.4f, 0.9f, 0.4f),  // Pequeno — verde
		new Color(0.4f, 0.7f, 1.0f),  // Médio   — azul
		new Color(0.9f, 0.8f, 0.1f),  // Grande  — amarelo
		new Color(0.9f, 0.3f, 1.0f),  // Raro    — roxo
	};

	private static readonly float[] Radii = { 5f, 7f, 9f, 12f };

	public void Init(XpDropType type)
	{
		_type = type;
		XpValue = type switch
		{
			XpDropType.Pequeno => 5,
			XpDropType.Medio   => 25,
			XpDropType.Grande  => 50,
			XpDropType.Raro    => 100,
			_                  => 5
		};
	}

	public override void _Ready()
{
	CollisionLayer = 8;
	CollisionMask  = 1;
	Monitoring     = true;
	Monitorable    = false;

	var col   = new CollisionShape2D();
	col.Shape = new CircleShape2D { Radius = Radii[(int)_type] + 8f };
	AddChild(col);

	BodyEntered += OnBodyEntered;

	// Luz com cor do tipo de drop
	CallDeferred(MethodName.AddLight);
}

private void AddLight()
{
	Color lightColor = _type switch
	{
		XpDropType.Pequeno => new Color(0.4f, 1f, 0.4f),   // verde claro
		XpDropType.Medio   => new Color(0.4f, 0.6f, 1f),   // azul
		XpDropType.Grande  => new Color(1f, 0.9f, 0.2f),   // amarelo
		XpDropType.Raro    => new Color(0.9f, 0.3f, 1f),   // roxo
		_                  => Colors.White
	};

	var light = new PointLight2D();
	light.Color         = lightColor;
	light.Energy        = 1.2f;
	light.TextureScale  = 1.5f;
	light.BlendMode     = Light2D.BlendModeEnum.Add;
	light.Texture       = GD.Load<Texture2D>("res://Assets/light_texture.tres");
	light.ShadowEnabled = false;
	AddChild(light);
}
	public override void _Process(double delta)
	{
		_age      += (float)delta;
		_bobTimer += (float)delta;

		var player = Player.Instance;

		// Lógica de atração (Quimiotaxia)
		if (player != null)
		{
			float magnetRadius = player.MagnetRadius;
			float dist = GlobalPosition.DistanceTo(player.GlobalPosition);

			if (dist <= magnetRadius)
			{
				_attracted = true;
			}

			if (_attracted)
			{
				float speed = 300f;
				Vector2 dir = (player.GlobalPosition - GlobalPosition).Normalized();
				GlobalPosition += dir * speed * (float)delta;
			}
		}

		if (!_attracted)
		{
			// Flutuação normal
			Position = new Vector2(Position.X,
				Position.Y + Mathf.Sin(_bobTimer * 3f) * 0.3f);

			// Pisca antes de expirar
			if (_age > 50f)
				Modulate = new Color(1, 1, 1, Mathf.Sin(_age * 10f) * 0.5f + 0.5f);
		}

		if (_age >= _lifetime) QueueFree();

		QueueRedraw();
	}

	private void OnBodyEntered(Node body)
	{
		if (body is Player p)
		{
			int xp = XpValue;
			// Aplica bônus de Fator de Crescimento
			if (p.XpBonus > 0f)
				xp = Mathf.RoundToInt(xp * (1f + p.XpBonus));
			p.GainXp(xp);
			AudioManager.Instance?.PlayPickup();
			QueueFree();
		}
	}

	public override void _Draw()
	{
		Color c = DropColors[(int)_type];
		float r = Radii[(int)_type];
		DrawCircle(Vector2.Zero, r, c);
		DrawArc(Vector2.Zero, r, 0f, Mathf.Tau, 16, new Color(1, 1, 1, 0.5f), 1.5f);
	}
}
