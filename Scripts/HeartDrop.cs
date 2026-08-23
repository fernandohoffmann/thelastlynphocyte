using Godot;

public partial class HeartDrop : Area2D
{
	private float _lifetime  = 60f;
	private float _age       = 0f;
	private float _bobTimer  = 0f;
	private bool  _collected = false;
	private bool  _attracted = false;

	private float _healElapsed  = 0f;
	private float _healDuration = 5f;
	private float _healTimer    = 0f;
	private const float HEAL_INTERVAL = 1f;
	private Player _healTarget = null;

	public override void _Ready()
	{
		CollisionLayer = 8;
		CollisionMask  = 1;
		Monitoring     = true;
		Monitorable    = false;

		var col   = new CollisionShape2D();
		col.Shape = new CircleShape2D { Radius = 16f };
		AddChild(col);

		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		if (_collected)
		{
			_healElapsed += (float)delta;
			_healTimer   += (float)delta;

			if (_healTimer >= HEAL_INTERVAL)
			{
				_healTimer -= HEAL_INTERVAL;
				if (IsInstanceValid(_healTarget) && _healTarget.CurrentHp < _healTarget.MaxHp)
					_healTarget.Heal(1);
			}

			if (_healElapsed >= _healDuration) QueueFree();
			return;
		}

		_age      += (float)delta;
		_bobTimer += (float)delta;

		var player = Player.Instance;

		// Atração por Quimiotaxia
		if (player != null)
		{
			float dist = GlobalPosition.DistanceTo(player.GlobalPosition);
			if (dist <= player.MagnetRadius) _attracted = true;

			if (_attracted)
			{
				Vector2 dir = (player.GlobalPosition - GlobalPosition).Normalized();
				GlobalPosition += dir * 300f * (float)delta;
			}
		}

		if (!_attracted)
		{
			Position = new Vector2(Position.X,
				Position.Y + Mathf.Sin(_bobTimer * 2.5f) * 0.4f);

			if (_age > 50f)
				Modulate = new Color(1, 1, 1, Mathf.Sin(_age * 10f) * 0.5f + 0.5f);
		}

		if (_age >= _lifetime) QueueFree();
		QueueRedraw();
	}

	private void OnBodyEntered(Node body)
	{
		if (_collected) return;
		if (body is Player p)
		{
			if (p.CurrentHp >= p.MaxHp) { QueueFree(); return; }
			_collected  = true;
			_healTarget = p;
			Visible     = false;
			AudioManager.Instance?.PlayPickup();
		}
	}

	public override void _Draw()
	{
		if (_collected) return;
		Color c = new Color(1f, 0.2f, 0.3f);
		DrawCircle(new Vector2(-5f, -3f), 6f, c);
		DrawCircle(new Vector2( 5f, -3f), 6f, c);
		DrawColoredPolygon(new Vector2[]
		{
			new Vector2(-10f,  0f),
			new Vector2( 10f,  0f),
			new Vector2(  0f, 10f)
		}, c);
	}
}
