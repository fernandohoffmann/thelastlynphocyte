using Godot;
using System.Collections.Generic;

public enum EnemyType { None, Bacteria, Virus }

public partial class Enemy : CharacterBody2D
{
	public static event System.Action<Enemy> EnemyDied;

	// Registro proprio dos inimigos vivos.
	// GetNodesInGroup("Enemy") aloca um Godot.Array e marshala a lista inteira
	// a cada chamada — e isso estava rodando varias vezes por frame, por projetil.
	// Esta lista e C# puro: percorrer nao aloca nada.
	public static readonly List<Enemy> All = new List<Enemy>();

	/// Inimigo vivo mais proximo de 'from' (null se nao houver nenhum no alcance).
	public static Enemy FindClosest(Vector2 from, float maxDist = float.MaxValue)
	{
		Enemy best = null;
		float bestD = maxDist;

		for (int i = 0; i < All.Count; i++)
		{
			var e = All[i];
			if (!IsInstanceValid(e)) continue;

			float d = from.DistanceTo(e.GlobalPosition);
			if (d < bestD) { bestD = d; best = e; }
		}
		return best;
	}

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

	protected Player _player;
	private float   _contactTimer = 0f;
	private const float CONTACT_INTERVAL = 0.8f;
	private float   _flashTimer   = 0f;
	private Vector2 _currentDir   = Vector2.Zero;
	private const float DIR_SMOOTH = 6f;

	// Perseguicao: em linha reta quando enxerga o player, pelo flow field quando
	// tem parede no meio. O teste de visao e caro pra rodar todo frame, entao
	// roda a cada LOS_INTERVAL com o relogio de cada inimigo defasado.
	private bool  _hasLos    = true;
	private float _losTimer  = 0f;
	private const float LOS_INTERVAL = 0.2f;

	// materiais dos sprites deste inimigo (usados pro flash de dano)
	private readonly List<ShaderMaterial> _blendMats = new List<ShaderMaterial>();

	public override void _Ready()
	{
		AddToGroup("Enemy");
		All.Add(this);

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

		// espalha os testes de visao entre os frames
		_losTimer = (float)GD.RandRange(0.0, LOS_INTERVAL);

		// deferred: as subclasses (Bacteria/Virus) so criam o sprite depois do base._Ready()
		CallDeferred(MethodName.SetupSceneBlend);
	}

	// Integra o inimigo ao cenario: sombra de contato + shader de luz ambiente.
	private void SetupSceneBlend()
	{
		if (!IsInstanceValid(this)) return;

		foreach (var child in GetChildren())
		{
			if (child is AnimatedSprite2D anim)
			{
				var mat = SceneBlend.MakeEnemyMaterial(anim.Scale.X);
				anim.Material = mat;
				_blendMats.Add(mat);
			}
			else if (child is Sprite2D spr && spr.Name != "ContactShadow")
			{
				var mat = SceneBlend.MakeEnemyMaterial(spr.Scale.X);
				spr.Material = mat;
				_blendMats.Add(mat);
			}
		}

		var shadow = SceneBlend.MakeContactShadow(BodyRadius);
		AddChild(shadow);
		MoveChild(shadow, 0); // desenha antes (por baixo) de tudo
	}

	public override void _ExitTree()
	{
		All.Remove(this);
	}

	private void SetFlash(float value)
	{
		foreach (var mat in _blendMats)
			mat.SetShaderParameter("flash", value);

		// inimigos sem sprite (Colony, ChagasBoss) ainda usam Modulate
		if (!UseSprite)
			Modulate = value > 0f ? new Color(1f, 0.3f, 0.3f) : Colors.White;
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
			SetFlash(_flashTimer > 0f ? 1f : 0f);
			QueueRedraw();
		}

		Vector2 desiredDir = ChooseDirection((float)delta);
		_currentDir = _currentDir.Lerp(desiredDir, DIR_SMOOTH * (float)delta);
		Velocity    = _currentDir * Speed;
		MoveAndSlide();

		_contactTimer -= (float)delta;
		if (_contactTimer <= 0f)
		{
			float dist = GlobalPosition.DistanceTo(_player.GlobalPosition);
			if (dist < BodyRadius + 16f)
			{
				OnContactHit();
				_contactTimer = CONTACT_INTERVAL;
			}
		}
	}

	// Linha reta se enxerga; contorno pelo flow field se nao.
	private Vector2 ChooseDirection(float delta)
	{
		Vector2 toPlayer = (_player.GlobalPosition - GlobalPosition).Normalized();

		var map = VeinMap.Instance;
		if (map == null) return toPlayer;

		_losTimer -= delta;
		if (_losTimer <= 0f)
		{
			_losTimer = LOS_INTERVAL;
			_hasLos   = map.HasLineOfSight(GlobalPosition, _player.GlobalPosition);
		}

		if (_hasLos) return toPlayer;

		Vector2 flow = map.GetFlowDirection(GlobalPosition);

		// sem caminho conhecido: volta pro comportamento antigo em vez de parar
		return flow == Vector2.Zero ? toPlayer : flow;
	}

	protected virtual void OnContactHit()
	{
		_player.TakeDamage(Damage);
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
		SetFlash(1f);
		QueueRedraw();
		AudioManager.Instance?.PlayEnemyHit();

		if (Hp <= 0) Die();
	}

	public void TakeDamageNoHemoglobina(int amount)
	{
		if (!IsInstanceValid(this)) return;

		Hp -= amount;
		_flashTimer = 0.12f;
		SetFlash(1f);
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
