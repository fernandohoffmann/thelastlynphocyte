using Godot;

public partial class HomingProjectile : Area2D
{
	public int       Damage     = 8;
	public EnemyType DamageType = EnemyType.None;

	private float   _speed     = 200f;
	private float   _turnSpeed = 6f;
	private float   _lifetime  = 6f;
	private float   _age       = 0f;
	private Node2D  _target;
	private float  _retarget = 0f;
	private const float RETARGET_INTERVAL = 0.1f;
	private Sprite2D _sprite;

	public override void _Ready()
	{
		CollisionLayer = 4;
		CollisionMask  = 2;
		Monitoring     = true;
		Monitorable    = false;

		var col   = new CollisionShape2D();
		col.Shape = new CircleShape2D { Radius = 6f };
		AddChild(col);

		BodyEntered += OnBodyEntered;

		// Sprite com filtro azul
		_sprite = new Sprite2D();
		_sprite.Texture  = GD.Load<Texture2D>("res://Assets/Sprites/anticorpos.png");
		_sprite.Scale    = new Vector2(0.015f, 0.015f);
		_sprite.Modulate = new Color(0.4f, 0.7f, 1f, 1f); // filtro azul
		AddChild(_sprite);

		FindClosestTarget();
		// cada projetil comeca o ciclo de re-alvo num instante diferente
		_retarget = (float)GD.RandRange(0.0, RETARGET_INTERVAL);
	}

	public override void _PhysicsProcess(double delta)
	{
		_age += (float)delta;
		if (_age >= _lifetime) { QueueFree(); return; }

		// Antes isso rodava TODO frame, em cada projetil. Como a mira ja e
		// suavizada por LerpAngle, 10x por segundo da o mesmo resultado visual.
		_retarget -= (float)delta;
		if (_retarget <= 0f || !IsInstanceValid(_target))
		{
			_retarget = RETARGET_INTERVAL;
			FindClosestTarget();
		}

		if (IsInstanceValid(_target))
		{
			float wantAngle = (_target.GlobalPosition - GlobalPosition).Angle();
			Rotation = Mathf.LerpAngle(Rotation, wantAngle, _turnSpeed * (float)delta);
		}

		// Rotaciona o sprite
		if (_sprite != null)
			_sprite.Rotation = -Rotation + Mathf.Pi / 2f;

		Vector2 moveDir = new Vector2(Mathf.Cos(Rotation), Mathf.Sin(Rotation));
		Vector2 nextPos = GlobalPosition + moveDir * _speed * (float)delta;

		var space = GetWorld2D().DirectSpaceState;
		var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, nextPos, 8);
		var result = space.IntersectRay(query);
		if (result.Count > 0) { QueueFree(); return; }

		GlobalPosition = nextPos;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Enemy e && IsInstanceValid(e))
		{
			e.TakeDamage(Damage, DamageType);
			QueueFree();
		}
	}

	private void FindClosestTarget()
	{
		_target = Enemy.FindClosest(GlobalPosition);
	}
}

public partial class WeaponAnticorposGuiados : Node2D
{
	public int WeaponLevel { get; private set; } = 1;

	private int   _damage   = 8;
	private float _cooldown = 2.5f;
	private float _timer    = 0f;
	private int   _count    = 1;

	public void UpgradeLevel()
	{
		WeaponLevel++;
		_damage   += 5;
		_cooldown  = Mathf.Max(0.8f, _cooldown - 0.4f);
		if (WeaponLevel >= 3) _count = 2;
	}

	public override void _Process(double delta)
	{
		if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;
		float attackMult = Player.Instance?.AttackSpeedBonus ?? 1f;
		float cd         = _cooldown * (Player.Instance?.CooldownReduction ?? 1f) / attackMult;
		_timer += (float)delta;
		if (_timer >= cd) { _timer = 0f; for (int i = 0; i < _count; i++) Launch(); }
	}

	private void Launch()
	{
		var proj = new HomingProjectile { Damage = _damage };
		proj.Rotation = GD.Randf() * Mathf.Tau;
		proj.GlobalPosition = GlobalPosition;
		GetTree().CurrentScene.AddChild(proj);
		AudioManager.Instance?.PlayShoot();
	}
}
