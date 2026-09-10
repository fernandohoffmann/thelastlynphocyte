// Enemy.cs
using Godot;

public enum EnemyType { None, Bacteria, Virus }

public partial class Enemy : CharacterBody2D
{
	public static event System.Action<Enemy> EnemyDied;

	public EnemyType Type       = EnemyType.Bacteria;
	public float BaseSpeed      = 80f;
	public int   BaseHp         = 10;
	public int   BaseDamage     = 2;
	public int   BaseXp         = 10;
	public Color BodyColor      = Colors.Green;
	public float BodyRadius     = 18f;
	public bool  UseSprite      = false;

	public float Speed    { get; private set; }
	public int   MaxHp    { get; private set; }
	public int   Hp       { get; private set; }
	public int   Damage   { get; private set; }
	public int   XpReward { get; private set; }

	private Player  _player;
	private float   _contactTimer = 0f;
	private const float CONTACT_INTERVAL = 0.8f;
	private float   _flashTimer   = 0f;
	private Vector2 _currentDir   = Vector2.Zero;
	private const float DIR_SMOOTH = 6f;

	public override void _Ready()
	{
		AddToGroup("Enemy");

		var col   = new CollisionShape2D();
		col.Shape = new CircleShape2D { Radius = BodyRadius };
		AddChild(col);

		CollisionLayer = 2;
		CollisionMask  = 8;

		Speed    = BaseSpeed;
		MaxHp    = BaseHp;
		Hp       = BaseHp;
		Damage   = BaseDamage;
		XpReward = BaseXp;

		_player = GetTree().GetFirstNodeInGroup("Player") as Player;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_player == null)
		{
			_player = GetTree().GetFirstNodeInGroup("Player") as Player;
			return;
		}

		if (_flashTimer > 0f)
		{
			_flashTimer -= (float)delta;
			Modulate = _flashTimer > 0f ? new Color(1f, 0.3f, 0.3f) : Colors.White;
			QueueRedraw();
		}

		Vector2 desiredDir = ComputeSteeringDir();
		_currentDir = _currentDir.Lerp(desiredDir, DIR_SMOOTH * (float)delta);
		Velocity    = _currentDir * Speed;
		MoveAndSlide();

		_contactTimer -= (float)delta;
		if (_contactTimer <= 0f)
		{
			float dist = GlobalPosition.DistanceTo(_player.GlobalPosition);
			if (dist < BodyRadius + 16f)
			{
				_player.TakeDamage(Damage);
				_contactTimer = CONTACT_INTERVAL;
			}
		}
	}

	private Vector2 ComputeSteeringDir()
	{
		Vector2 toPlayer = (_player.GlobalPosition - GlobalPosition).Normalized();
		var     space    = GetWorld2D().DirectSpaceState;
		float   rayLen   = 80f;
		int     rays     = 16;
		float   bestScore = float.MinValue;
		Vector2 bestDir  = toPlayer;

		for (int i = 0; i < rays; i++)
		{
			float   angle = (Mathf.Tau / rays) * i;
			Vector2 dir   = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

			var query = PhysicsRayQueryParameters2D.Create(
				GlobalPosition + dir * (BodyRadius + 2f),
				GlobalPosition + dir * rayLen,
				8
			);
			var result = space.IntersectRay(query);

			float wallPenalty = 0f;
			if (result.Count > 0)
			{
				float hitDist = ((Vector2)result["position"]).DistanceTo(GlobalPosition);
				wallPenalty   = -3f * (1f - hitDist / rayLen);
			}

			float inertiaBias = dir.Dot(_currentDir) * 0.3f;
			float score       = dir.Dot(toPlayer) + wallPenalty + inertiaBias;

			if (score > bestScore)
			{
				bestScore = score;
				bestDir   = dir;
			}
		}

		return bestDir;
	}

	public void ApplyDifficultyScale(float mult)
	{
		float speedMult = Mathf.Min(mult, 2.0f);
		Speed    = BaseSpeed * Mathf.Sqrt(speedMult);
		MaxHp    = Mathf.RoundToInt(BaseHp * mult);
		Hp       = MaxHp;
		Damage   = Mathf.RoundToInt(BaseDamage * mult);
		XpReward = Mathf.RoundToInt(BaseXp * Mathf.Sqrt(mult));
	}

	public void TakeDamage(int amount, EnemyType dmgType)
	{
		if (Player.Instance != null)
		{
			if (Type == EnemyType.Bacteria && dmgType != EnemyType.None)
				amount = Mathf.RoundToInt(amount * Player.Instance.BonusDmgVsBacteria);
			else if (Type == EnemyType.Virus && dmgType != EnemyType.None)
				amount = Mathf.RoundToInt(amount * Player.Instance.BonusDmgVsVirus);
		}

		Hp -= amount;
		_flashTimer = 0.12f;
		QueueRedraw();
		AudioManager.Instance?.PlayEnemyHit();

		if (Hp <= 0) Die();
	}

	// Versão sem hemoglobina — evita recursão infinita
	public void TakeDamageNoHemoglobina(int amount)
	{
		if (!IsInstanceValid(this)) return;

		Hp -= amount;
		_flashTimer = 0.12f;
		QueueRedraw();
		AudioManager.Instance?.PlayEnemyHit();

		if (Hp <= 0)
		{
			SpawnXpDrop();
			if (GD.Randf() < 0.05f) SpawnHeartDrop();
			GameManager.Instance?.GainDna(1);
			EnemyDied?.Invoke(this);
			QueueFree();
		}
	}

	private void Die()
	{
		SpawnXpDrop();
		if (GD.Randf() < 0.05f) SpawnHeartDrop();
		Player.Instance?.TriggerHemoglobinaExplosion(GlobalPosition);
		GameManager.Instance?.GainDna(1);
		EnemyDied?.Invoke(this);
		QueueFree();
	}

	private void SpawnXpDrop()
	{
		XpDropType type;
		float roll = GD.Randf();

		if (MaxHp >= 50)
		{
			if      (roll < 0.05f) type = XpDropType.Raro;
			else if (roll < 0.25f) type = XpDropType.Grande;
			else if (roll < 0.60f) type = XpDropType.Medio;
			else                   type = XpDropType.Pequeno;
		}
		else if (MaxHp >= 20)
		{
			if      (roll < 0.02f) type = XpDropType.Raro;
			else if (roll < 0.12f) type = XpDropType.Grande;
			else if (roll < 0.45f) type = XpDropType.Medio;
			else                   type = XpDropType.Pequeno;
		}
		else
		{
			if      (roll < 0.01f) type = XpDropType.Raro;
			else if (roll < 0.05f) type = XpDropType.Grande;
			else if (roll < 0.20f) type = XpDropType.Medio;
			else                   type = XpDropType.Pequeno;
		}

		var drop = new XpDrop();
		drop.Init(type);
		GetTree().CurrentScene.AddChild(drop);
		drop.GlobalPosition = GlobalPosition + new Vector2(
			(float)GD.RandRange(-15.0, 15.0),
			(float)GD.RandRange(-15.0, 15.0)
		);
	}

	private void SpawnHeartDrop()
	{
		var heart = new HeartDrop();
		GetTree().CurrentScene.AddChild(heart);
		heart.GlobalPosition = GlobalPosition;
	}

	public override void _Draw()
	{
		if (UseSprite) return;

		Color c = _flashTimer > 0f ? new Color(1f, 0.3f, 0.3f) : BodyColor;
		DrawCircle(Vector2.Zero, BodyRadius, c);
		DrawArc(Vector2.Zero, BodyRadius, 0f, Mathf.Tau, 32,
			Colors.White * new Color(1, 1, 1, 0.4f), 2f);
	}
}
