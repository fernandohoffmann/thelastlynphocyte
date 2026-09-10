using Godot;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
	public static Player Instance { get; private set; }

	[Signal] public delegate void PlayerDiedEventHandler();
	[Signal] public delegate void LevelUpEventHandler(int newLevel);
	[Signal] public delegate void XpChangedEventHandler(int current, int required);
	[Signal] public delegate void HpChangedEventHandler(int current, int max);

	[Export] public float MoveSpeed = 220f;
	public int MaxHp { get; private set; } = 20;
	public int CurrentHp { get; private set; } = 20;
	public int Level { get; private set; } = 1;
	public int CurrentXp { get; private set; } = 0;
	public int XpForNext => GetXpForNextLevel();

	private static readonly int[] XpThresholds = { 100, 200, 400, 600, 800, 1000, 1200 };

	public float CooldownReduction     { get; set; } = 1f;
	public float BonusDmgVsBacteria    { get; set; } = 1f;
	public float BonusDmgVsVirus       { get; set; } = 1f;
	public bool  HasHemoglobinaReativa { get; set; } = false;
	public float HemoglobinRadius      { get; set; } = 80f;
	public float HemoglobinDamage      { get; set; } = 5f;
	public float XpBonus               { get; set; } = 0f;
	public float MagnetRadius          { get; set; } = 80f;
	public float AttackSpeedBonus      { get; set; } = 1f;
	public float HealBonus             { get; set; } = 1f;
	public float DnaBonus              { get; set; } = 1f;
	public bool  HasPlaquetas          { get; set; } = false;
	public float PlaquetaTimer         { get; set; } = 0f;
	public float PlaquetaDuration      { get; set; } = 3f;
	public float AdrenalineDamage      { get; set; } = 0f;
	public float AdrenalineSpeed       { get; set; } = 0f;

	private float   _iFrameTimer     = 0f;
	private const float IFRAME_DURATION = 0.8f;
	private float   _blinkTimer      = 0f;
	private Vector2 _smoothVelocity  = Vector2.Zero;
	private const float ACCEL        = 1800f;
	private const float DECEL        = 1400f;

	private bool _hemoglobinaActive = false;

	private AnimatedSprite2D _animSprite;

	public override void _Ready()
	{
		Instance = this;
		AddToGroup("Player");

		_animSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_animSprite != null)
			_animSprite.Play("idle");

		var light = new PointLight2D();
		light.Color         = new Color(0.3f, 0.6f, 1f);
		light.Energy        = 1.2f;
		light.TextureScale  = 3.5f;
		light.BlendMode     = Light2D.BlendModeEnum.Add;
		light.Texture       = GD.Load<Texture2D>("res://Assets/light_texture.tres");
		light.ShadowEnabled = false;
		AddChild(light);

		CallDeferred(MethodName.EmitInitialSignals);
	}

	private void EmitInitialSignals()
	{
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
		EmitSignal(SignalName.XpChanged, CurrentXp, GetXpForNextLevel());
		EmitSignal(SignalName.LevelUp, Level);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning)
			return;

		if (PlaquetaTimer > 0f)
			PlaquetaTimer -= (float)delta;

		if (_iFrameTimer > 0f)
		{
			_iFrameTimer -= (float)delta;
			_blinkTimer  += (float)delta;

			if (_animSprite != null)
				_animSprite.Modulate = (_blinkTimer % 0.15f < 0.075f)
					? new Color(1, 1, 1, 0.3f)
					: Colors.White;
		}
		else if (_animSprite != null)
		{
			_animSprite.Modulate = Colors.White;
		}

		// ── Sprite sempre aponta para o cursor, com rotação suave ──
		if (_animSprite != null)
		{
			Vector2 mousePos = GetGlobalMousePosition();
			Vector2 lookDir  = mousePos - GlobalPosition;
			if (lookDir.LengthSquared() > 0.01f)
			{
				float targetAngle = lookDir.Angle();
				_animSprite.Rotation = Mathf.LerpAngle(_animSprite.Rotation, targetAngle, 12f * (float)delta);
			}
		}

		// ── Movimento ────────────────────────────────────────────
		Vector2 dir = Vector2.Zero;
		if (Input.IsActionPressed("move_right")) dir.X += 1;
		if (Input.IsActionPressed("move_left"))  dir.X -= 1;
		if (Input.IsActionPressed("move_down"))  dir.Y += 1;
		if (Input.IsActionPressed("move_up"))    dir.Y -= 1;
		dir = dir.Normalized();

		float speedMult = 1f + AdrenalineSpeed;
		float speed     = MoveSpeed * speedMult;

		if (dir != Vector2.Zero)
			_smoothVelocity = _smoothVelocity.MoveToward(dir * speed, ACCEL * (float)delta);
		else
			_smoothVelocity = _smoothVelocity.MoveToward(Vector2.Zero, DECEL * (float)delta);

		Velocity = _smoothVelocity;
		MoveAndSlide();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			if (GameManager.Instance.IsPaused)
				GameManager.Instance.ResumeGame();
			else
				GameManager.Instance.PauseGame();
		}
	}

	public void TakeDamage(int amount)
	{
		if (_iFrameTimer > 0f) return;
		if (HasPlaquetas && PlaquetaTimer > 0f) return;

		CurrentHp    = Mathf.Max(0, CurrentHp - amount);
		_iFrameTimer = IFRAME_DURATION;
		_blinkTimer  = 0f;
		AudioManager.Instance?.PlayPlayerHit();

		if (HasPlaquetas)
			PlaquetaTimer = PlaquetaDuration;

		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);

		if (CurrentHp <= 0)
		{
			EmitSignal(SignalName.PlayerDied);
			GameManager.Instance.EndGame();
		}
	}

	public void Heal(int amount)
	{
		int healed = Mathf.RoundToInt(amount * HealBonus);
		CurrentHp  = Mathf.Min(MaxHp, CurrentHp + healed);
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
	}

	public void GainXp(int amount)
	{
		CurrentXp += amount;
		int required = GetXpForNextLevel();

		while (CurrentXp >= required)
		{
			CurrentXp -= required;
			DoLevelUp();
			required = GetXpForNextLevel();
		}

		EmitSignal(SignalName.XpChanged, CurrentXp, GetXpForNextLevel());
	}

	private void DoLevelUp()
	{
		Level++;
		MaxHp     += 1;
		CurrentHp += 1;
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
		EmitSignal(SignalName.LevelUp, Level);
		AudioManager.Instance?.PlayLevelUp();
	}

	public void AddMaxHp(int amount)
	{
		MaxHp     += amount;
		CurrentHp += amount;
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
	}

	private int GetXpForNextLevel()
	{
		int idx = Level - 1;
		if (idx < XpThresholds.Length)
			return XpThresholds[idx];
		return XpThresholds[XpThresholds.Length - 1] + (idx - XpThresholds.Length + 1) * 200;
	}

	public void TriggerHemoglobinaExplosion(Vector2 position)
	{
		if (!HasHemoglobinaReativa) return;
		if (_hemoglobinaActive) return;
		_hemoglobinaActive = true;

		var smoke = new HemoglobinaSmoke();
		smoke.Radius = HemoglobinRadius;
		GetTree().CurrentScene.AddChild(smoke);
		smoke.GlobalPosition = position;

		var enemies = GetTree().GetNodesInGroup("Enemy");
		var toHit   = new List<Enemy>();
		foreach (Node node in enemies)
			if (node is Enemy enemy && IsInstanceValid(enemy) && enemy.GlobalPosition.DistanceTo(position) <= HemoglobinRadius)
				toHit.Add(enemy);

		foreach (var enemy in toHit)
			if (IsInstanceValid(enemy))
				enemy.TakeDamageNoHemoglobina((int)HemoglobinDamage);

		_hemoglobinaActive = false;
	}

	public float GetAdrenalineFactor()
	{
		return 1f - ((float)CurrentHp / MaxHp);
	}
}
