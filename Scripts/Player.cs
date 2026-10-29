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

	// ── Febre (aplicada por inimigos como a Doenca de Chagas) ──
	private float _feverTimer         = 0f;
	private float _feverTickTimer     = 0f;
	private int   _feverDamagePerTick = 0;
	private const float FEVER_TICK_INTERVAL = 1f;
	public bool HasFever => _feverTimer > 0f;

	// ── Sprite / deformacao ───────────────────────────────────────────────
	// Funciona tanto com AnimatedSprite2D quanto com Sprite2D comum.
	private Node2D         _sprite;
	private ShaderMaterial _blendMat;
	private Vector2        _baseScale = Vector2.One;
	private float          _stretch   = 0f;

	// ── AJUSTE FINO DA "VIDA" DO SPRITE ───────────────────────────────────
	private const float BREATHE_AMOUNT = 0.040f; // respiracao (pulsar parado)
	private const float BREATHE_SPEED  = 0.55f;  // ciclos por segundo
	private const float STRETCH_AMOUNT = 0.14f;  // alongar na direcao do movimento
	private const float SHADOW_RADIUS  = 15f;    // tamanho da sombra de contato
	private const float SHADOW_ALPHA   = 0.35f;  // sombra do player mais fraca
	// ──────────────────────────────────────────────────────────────────────

	public override void _ExitTree()
	{
		// mesma armadilha do VeinMap: sem isto o Instance aponta pro player
		// da cena anterior, ja liberado
		if (Instance == this) Instance = null;
	}

	public override void _Ready()
	{
		Instance = this;
		AddToGroup("Player");

		_sprite = FindSprite();

		if (_sprite is AnimatedSprite2D anim)
		{
			if (anim.SpriteFrames != null && anim.SpriteFrames.HasAnimation("idle"))
				anim.Play("idle");
			else
				anim.Play();
		}

		SetupSceneBlend();

		var light = new PointLight2D();
		light.Color         = new Color(0.3f, 0.6f, 1f);
		light.Energy        = 0.8f;
		light.TextureScale  = 2.5f;
		light.BlendMode     = Light2D.BlendModeEnum.Add;
		light.Texture       = GD.Load<Texture2D>("res://Assets/light_texture.tres");
		light.ShadowEnabled = false;
		AddChild(light);

		CallDeferred(MethodName.EmitInitialSignals);
	}

	// Acha o sprite do player seja ele animado ou nao.
	private Node2D FindSprite()
	{
		var found = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D") as Node2D;
		if (found != null) return found;

		found = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (found != null) return found;

		foreach (var child in GetChildren())
		{
			if (child is AnimatedSprite2D a) return a;
			if (child is Sprite2D s && s.Name != "ContactShadow") return s;
		}
		return null;
	}

	// Wobble de membrana + luz ambiente + sombra de contato.
	private void SetupSceneBlend()
	{
		if (_sprite == null) return;

		_blendMat = SceneBlend.MakePlayerMaterial(_sprite.Scale.X);

		// AtlasTexture precisa dos uniforms de regiao, senao o wobble sai torto
		Texture2D tex = null;
		if (_sprite is Sprite2D spr) tex = spr.Texture;
		else if (_sprite is AnimatedSprite2D anim && anim.SpriteFrames != null
				 && anim.SpriteFrames.HasAnimation(anim.Animation)
				 && anim.SpriteFrames.GetFrameCount(anim.Animation) > 0)
			tex = anim.SpriteFrames.GetFrameTexture(anim.Animation, 0);

		SceneBlend.ApplyRegionUniforms(_blendMat, tex);

		if (_sprite is CanvasItem ci) ci.Material = _blendMat;

		// o shader encolhe a imagem pra ter margem de deformacao — compensa na escala
		_baseScale    = _sprite.Scale * (1f + SceneBlend.PLAYER_PAD);
		_sprite.Scale = _baseScale;

		var shadow = SceneBlend.MakeContactShadow(SHADOW_RADIUS, SHADOW_ALPHA);
		AddChild(shadow);
		MoveChild(shadow, 0);
	}

	// Com o shader ativo o Modulate nao vale mais — a cor vai pelo uniform tint.
	private Color _lastTint = Colors.White;

	private void SetSpriteTint(Color c)
	{
		// evita mandar o mesmo valor pro shader 60x por segundo
		if (c == _lastTint) return;
		_lastTint = c;

		if (_blendMat != null)
			_blendMat.SetShaderParameter("tint", c);
		else if (_sprite != null)
			_sprite.Modulate = c;
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

			SetSpriteTint((_blinkTimer % 0.15f < 0.075f)
				? new Color(1, 1, 1, 0.3f)
				: Colors.White);
		}
		else
		{
			// Tinge de laranja levemente enquanto está com febre
			SetSpriteTint(HasFever ? new Color(1f, 0.75f, 0.5f) : Colors.White);
		}

		// ── Febre: dano por segundo ──────────────────────────────
		if (_feverTimer > 0f)
		{
			_feverTimer     -= (float)delta;
			_feverTickTimer -= (float)delta;

			if (_feverTickTimer <= 0f)
			{
				_feverTickTimer = FEVER_TICK_INTERVAL;
				TakeFeverDamage(_feverDamagePerTick);
			}
		}

		// ── Sprite sempre aponta para o cursor, com rotação suave ──
		if (_sprite != null)
		{
			Vector2 mousePos = GetGlobalMousePosition();
			Vector2 lookDir  = mousePos - GlobalPosition;
			if (lookDir.LengthSquared() > 0.01f)
			{
				float targetAngle = lookDir.Angle();
				_sprite.Rotation = Mathf.LerpAngle(_sprite.Rotation, targetAngle, 12f * (float)delta);
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

		// o ar do pulmao arrasta: soma ao movimento, nao substitui, entao voce
		// ainda consegue andar contra ele — so custa mais
		Vector2 drift = LungBreath.Instance?.GetDrift(GlobalPosition) ?? Vector2.Zero;

		Velocity = _smoothVelocity + drift;
		MoveAndSlide();

		UpdateSquashStretch(delta);
	}

	// Respiracao + alongamento na direcao do movimento.
	// O wobble fino da membrana fica por conta do shader; isso aqui e o "corpo todo".
	private void UpdateSquashStretch(double delta)
	{
		if (_sprite == null) return;

		float t       = Time.GetTicksMsec() / 1000f;
		float breathe = 1f + Mathf.Sin(t * BREATHE_SPEED * Mathf.Tau) * BREATHE_AMOUNT;
		float side    = 1f + Mathf.Sin(t * BREATHE_SPEED * Mathf.Tau * 0.77f + 1.3f) * BREATHE_AMOUNT * 0.7f;

		// componente do movimento no eixo pra onde o sprite esta olhando
		float forward = 0f;
		if (_smoothVelocity.LengthSquared() > 1f && MoveSpeed > 0f)
		{
			Vector2 look = Vector2.Right.Rotated(_sprite.Rotation);
			forward = Mathf.Clamp(look.Dot(_smoothVelocity / MoveSpeed), -1f, 1f);
		}
		_stretch = Mathf.Lerp(_stretch, forward, 8f * (float)delta);

		var target = new Vector2(
			_baseScale.X * breathe * (1f + STRETCH_AMOUNT * _stretch),
			_baseScale.Y * side    * (1f - STRETCH_AMOUNT * 0.8f * _stretch)
		);

		_sprite.Scale = _sprite.Scale.Lerp(target, 12f * (float)delta);
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

	// Dano de status (febre) — ignora i-frames de propósito, é um efeito contínuo, não um hit
	public void TakeFeverDamage(int amount)
	{
		CurrentHp = Mathf.Max(0, CurrentHp - amount);
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);

		if (CurrentHp <= 0)
		{
			EmitSignal(SignalName.PlayerDied);
			GameManager.Instance.EndGame();
		}
	}

	// Aplica/renova a febre — chamado pelo ChagasBoss ao acertar o jogador
	public void ApplyFever(float duration, int damagePerTick)
	{
		_feverTimer         = Mathf.Max(_feverTimer, duration);
		_feverDamagePerTick = damagePerTick;
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

		var toHit = new List<Enemy>();
		foreach (Enemy enemy in Enemy.All)
			if (IsInstanceValid(enemy) && enemy.GlobalPosition.DistanceTo(position) <= HemoglobinRadius)
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
