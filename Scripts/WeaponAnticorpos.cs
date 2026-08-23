using Godot;

public partial class AnticorpoProjectile : CharacterBody2D
{
	public int       Damage     = 5;
	public EnemyType DamageType = EnemyType.None;

	private float    _speed     = 500f;
	private Vector2  _direction = Vector2.Right;
	private float    _lifetime  = 1.6f;
	private float    _age       = 0f;
	private Sprite2D _sprite;
	// Azul = guiado
	public bool IsGuided = false;

	public void Init(Vector2 direction, int damage, bool guided = false)
	{
		_direction = direction.Normalized();
		Damage     = damage;
		IsGuided   = guided;
	}

	public override void _Ready()
	{
		var col   = new CollisionShape2D();
		col.Shape = new CircleShape2D { Radius = 7f };
		AddChild(col);

		CollisionLayer = 4;
		CollisionMask  = 2 + 8;

		_sprite = new Sprite2D();
		var tex = GD.Load<Texture2D>("res://Assets/Sprites/anticorpos.png");
		_sprite.Texture = tex;
		_sprite.Scale   = new Vector2(0.02f, 0.02f);
		// Filtro azul para o guiado
		if (IsGuided)
			_sprite.Modulate = new Color(0.4f, 0.7f, 1f, 1f);
		AddChild(_sprite);
	}

	public override void _PhysicsProcess(double delta)
	{
		_age += (float)delta;
		if (_age >= _lifetime) { QueueFree(); return; }

		// Rotaciona o sprite na direção do movimento
		if (_sprite != null)
			_sprite.Rotation = _direction.Angle() + Mathf.Pi / 2f;

		Velocity = _direction * _speed;
		var collision = MoveAndCollide(Velocity * (float)delta);

		if (collision != null)
		{
			var collider = collision.GetCollider();
			if (collider is Enemy e) e.TakeDamage(Damage, DamageType);
			QueueFree();
		}
	}
}

public partial class WeaponAnticorpos : Node2D
{
	public int WeaponLevel { get; private set; } = 1;

	private int   _damage    = 5;
	private float _fireRate  = 2.0f;
	private float _fireTimer = 0f;

	private const int   TOTAL_FRAMES = 24;
	private const int   SHOOT_FRAME  = 12;
	private const float SPEED_SCALE  = 3.0f;

	public void UpgradeLevel()
	{
		WeaponLevel++;
		_damage   += 3;
		_fireRate += 0.5f;
		SyncAnimationFps();
	}

	public override void _Ready() => SyncAnimationFps();

	private void SyncAnimationFps()
	{
		var anim = Player.Instance?.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (anim == null) return;
		float fps = (SHOOT_FRAME * _fireRate) / SPEED_SCALE;
		anim.SpeedScale = SPEED_SCALE;
		anim.SpriteFrames?.SetAnimationSpeed("idle", fps);
		if (!anim.IsPlaying()) anim.Play("idle");
	}

	public override void _Process(double delta)
	{
		if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;
		float attackMult = Player.Instance?.AttackSpeedBonus ?? 1f;
		float cdMult     = Player.Instance?.CooldownReduction ?? 1f;
		float interval   = (1f / (_fireRate * attackMult)) * cdMult;
		_fireTimer += (float)delta;
		if (_fireTimer >= interval) { _fireTimer = 0f; Shoot(); }
	}

	private void Shoot()
	{
		var mousePos  = GetViewport().GetMousePosition();
		var camera    = GetViewport().GetCamera2D();
		Vector2 worldMouse = camera != null
			? mousePos + camera.GlobalPosition - GetViewportRect().Size / 2f
			: mousePos;

		Vector2 dir = (worldMouse - GlobalPosition).Normalized();
		if (dir == Vector2.Zero) dir = Vector2.Right;

		var proj = new AnticorpoProjectile();
		GetTree().CurrentScene.AddChild(proj);
		proj.GlobalPosition = GlobalPosition;
		proj.Init(dir, _damage, guided: false);
		AudioManager.Instance?.PlayShoot();
	}
}
