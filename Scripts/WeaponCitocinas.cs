// WeaponCitocinas.cs — a nuvem virou uma massa de fumaca por shader (fbm com
// domain warp), que se revolve e vai se rasgando ate sumir.
using Godot;
using System.Collections.Generic;

// ── Nuvem de veneno que aparece periodicamente ao redor do player ──
public partial class CitokinaCloud : Area2D
{
	public int   Damage   = 3;
	public float Radius   = 80f;
	public float Duration = 3f;

	private float          _age       = 0f;
	private float          _tickTimer = 0f;
	private const float    TICK       = 0.5f;
	private HashSet<ulong> _hit       = new();

	private ColorRect      _visual;
	private ShaderMaterial _mat;

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	// o quad precisa ser maior que o raio porque a nuvem incha ao se dissipar
	private const float RECT_FACTOR = 3.2f;
	private static readonly Color COLOR_CORE = new Color(0.78f, 1.00f, 0.38f);
	private static readonly Color COLOR_EDGE = new Color(0.22f, 0.70f, 0.16f);
	// ──────────────────────────────────────────────────────────────────────

	private static Shader _cloudShader;
	private static Shader CloudShader
	{
		get { if (_cloudShader == null) _cloudShader = BuildShader(); return _cloudShader; }
	}

	public override void _Ready()
	{
		CollisionLayer = 4;
		CollisionMask  = 2;
		Monitoring     = true;
		Monitorable    = false;

		var col   = new CollisionShape2D();
		col.Shape = new CircleShape2D { Radius = Radius };
		AddChild(col);

		BuildVisual();
	}

	private void BuildVisual()
	{
		float side = Radius * RECT_FACTOR;

		_mat = new ShaderMaterial { Shader = CloudShader };
		_mat.SetShaderParameter("color_core", COLOR_CORE);
		_mat.SetShaderParameter("color_edge", COLOR_EDGE);
		_mat.SetShaderParameter("life",       Duration);
		_mat.SetShaderParameter("age",        0f);
		_mat.SetShaderParameter("seed",       (float)GD.RandRange(0.0, 50.0));

		_visual = new ColorRect
		{
			Size        = new Vector2(side, side),
			Position    = new Vector2(-side * 0.5f, -side * 0.5f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Material    = _mat,
			ZIndex      = 1
		};
		AddChild(_visual);
	}

	public override void _Process(double delta)
	{
		_age       += (float)delta;
		_tickTimer += (float)delta;

		if (_tickTimer >= TICK)
		{
			_tickTimer = 0f;
			DamageOverlapping();
		}

		// o fade agora e feito dentro do shader (o Modulate nao vale com shader)
		_mat?.SetShaderParameter("age", _age);

		if (_age >= Duration) QueueFree();
	}

	private void DamageOverlapping()
	{
		foreach (var body in GetOverlappingBodies())
			if (body is Enemy e && IsInstanceValid(e))
				e.TakeDamage(Damage, EnemyType.None);

		foreach (var area in GetOverlappingAreas())
			if (area.GetParent() is Enemy e && IsInstanceValid(e))
				e.TakeDamage(Damage, EnemyType.None);
	}

	private static Shader BuildShader()
	{
		var shader = new Shader();
		shader.Code = @"
shader_type canvas_item;

uniform float age  = 0.0;
uniform float life = 3.0;
uniform float seed = 0.0;
uniform vec3  color_core : source_color = vec3(0.78, 1.00, 0.38);
uniform vec3  color_edge : source_color = vec3(0.22, 0.70, 0.16);
uniform float opacity : hint_range(0.0, 1.0) = 0.85;

vec2 hash2(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return fract(sin(p) * 43758.5453);
}

float valueNoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    float a = dot(hash2(i)                  - 0.5, vec2(1.0));
    float b = dot(hash2(i + vec2(1.0, 0.0)) - 0.5, vec2(1.0));
    float c = dot(hash2(i + vec2(0.0, 1.0)) - 0.5, vec2(1.0));
    float d = dot(hash2(i + vec2(1.0, 1.0)) - 0.5, vec2(1.0));
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

float fbm(vec2 p) {
    float v = 0.0;
    float amp = 0.5;
    for (int i = 0; i < 4; i++) {
        v += valueNoise(p) * amp;
        p *= 2.07;
        amp *= 0.5;
    }
    return v;
}

void fragment() {
    float k = clamp(age / life, 0.0, 1.0);

    // a massa incha enquanto se dissipa
    vec2 uv = (UV - vec2(0.5)) / (0.62 + 0.33 * k);
    float r = length(uv) * 2.0;

    // domain warp: e isso que faz a fumaca se revolver em vez de so piscar
    vec2 p = uv * 3.2 + vec2(seed, seed * 1.7);
    vec2 w = vec2(
        fbm(p + vec2(0.0, 0.0) + TIME * 0.20),
        fbm(p + vec2(5.2, 1.3) - TIME * 0.17)
    );
    float n = fbm(p * 1.6 + w * 1.4 + vec2(0.0, -TIME * 0.25));

    // borda redonda suave pra nuvem nao virar um quadrado
    float mask = 1.0 - smoothstep(0.55, 1.0, r);

    // com o tempo o limiar sobe: a nuvem vai se rasgando em tufos antes de sumir
    float dens = smoothstep(0.30 + 0.32 * k, 0.70 + 0.22 * k, n * 0.5 + 0.5) * mask;
    dens *= (1.0 - k * k);
    dens = clamp(dens, 0.0, 1.0);

    vec3 c = mix(color_edge, color_core, smoothstep(0.0, 0.65, dens));

    COLOR = vec4(c, dens * opacity);
}
";
		return shader;
	}
}

public partial class WeaponCitocinas : Node2D
{
	public int WeaponLevel { get; private set; } = 1;

	private int   _damage   = 3;
	private float _radius   = 80f;
	private float _cooldown = 5f;
	private float _duration = 3f;
	private float _timer    = 0f;

	public void UpgradeLevel()
	{
		WeaponLevel++;
		_damage   += 2;
		_cooldown  = Mathf.Max(2f, _cooldown - 0.5f);
		_radius   += 10f;
	}

	public override void _Process(double delta)
	{
		if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;

		float cd = _cooldown * (Player.Instance?.CooldownReduction ?? 1f);
		_timer += (float)delta;

		if (_timer >= cd)
		{
			_timer = 0f;
			SpawnCloud();
		}
	}

	private void SpawnCloud()
	{
		var cloud = new CitokinaCloud
		{
			Damage   = _damage,
			Radius   = _radius,
			Duration = _duration
		};
		GetTree().CurrentScene.AddChild(cloud);
		cloud.GlobalPosition = GlobalPosition;
		AudioManager.Instance?.PlayShoot();
	}
}
