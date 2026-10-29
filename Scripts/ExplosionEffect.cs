// ExplosionEffect.cs — estouro do macrofago feito por shader (anel rasgado + nucleo
// quente + tufos), no lugar do circulo liso.
//
// IMPORTANTE: apague a classe ExplosionEffect que esta no fim do WeaponMacrofago.cs,
// senao o C# reclama de classe duplicada.
using Godot;

public partial class ExplosionEffect : Node2D
{
	public float Radius = 100f;

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	private const float DURATION    = 0.45f;
	private const float RECT_FACTOR = 2.6f; // o quad precisa sobrar pro anel rasgado
	private static readonly Color COLOR_CORE = new Color(1.00f, 0.95f, 0.65f);
	private static readonly Color COLOR_EDGE = new Color(1.00f, 0.32f, 0.06f);
	// ──────────────────────────────────────────────────────────────────────

	private float          _age = 0f;
	private ColorRect      _visual;
	private ShaderMaterial _mat;
	private PointLight2D   _light;

	private static Shader _shader;
	private static Shader BlastShader
	{
		get { if (_shader == null) _shader = BuildShader(); return _shader; }
	}

	public override void _Ready()
	{
		float side = Radius * RECT_FACTOR;

		_mat = new ShaderMaterial { Shader = BlastShader };
		_mat.SetShaderParameter("color_core", COLOR_CORE);
		_mat.SetShaderParameter("color_edge", COLOR_EDGE);
		_mat.SetShaderParameter("life",       DURATION);
		_mat.SetShaderParameter("age",        0f);
		_mat.SetShaderParameter("seed",       (float)GD.RandRange(0.0, 50.0));

		_visual = new ColorRect
		{
			Size        = new Vector2(side, side),
			Position    = new Vector2(-side * 0.5f, -side * 0.5f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Material    = _mat,
			ZIndex      = 2
		};
		AddChild(_visual);

		// clarao rapido no tecido em volta
		_light = new PointLight2D
		{
			Color         = new Color(1f, 0.6f, 0.25f),
			Energy        = 2.2f,
			TextureScale  = Radius / 40f,
			BlendMode     = Light2D.BlendModeEnum.Add,
			Texture       = GD.Load<Texture2D>("res://Assets/light_texture.tres"),
			ShadowEnabled = false
		};
		AddChild(_light);
	}

	public override void _Process(double delta)
	{
		_age += (float)delta;

		_mat?.SetShaderParameter("age", _age);

		// a luz apaga bem mais rapido que a fumaca
		if (_light != null)
			_light.Energy = Mathf.Max(0f, 2.2f * (1f - _age / (DURATION * 0.45f)));

		if (_age >= DURATION) QueueFree();
	}

	private static Shader BuildShader()
	{
		var shader = new Shader();
		shader.Code = @"
shader_type canvas_item;
render_mode blend_add;

uniform float age  = 0.0;
uniform float life = 0.45;
uniform float seed = 0.0;
uniform vec3  color_core : source_color = vec3(1.00, 0.95, 0.65);
uniform vec3  color_edge : source_color = vec3(1.00, 0.32, 0.06);

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
        p *= 2.05;
        amp *= 0.5;
    }
    return v;
}

void fragment() {
    float k = clamp(age / life, 0.0, 1.0);

    vec2  uv = (UV - vec2(0.5)) * 2.0;   // -1 .. 1
    float r  = length(uv);
    float a  = atan(uv.y, uv.x);

    // ruido amostrado no circulo: nao tem costura quando o angulo da a volta
    vec2  ring = vec2(cos(a), sin(a)) * 2.6 + vec2(seed);
    float nA   = fbm(ring);

    // frente de onda irregular, desacelerando (ease-out)
    float t      = 1.0 - pow(1.0 - k, 2.2);
    float radius = t * (0.78 + 0.30 * nA);

    // casca: mais grossa conforme abre
    float thick = 0.10 + 0.30 * k;
    float shell = 1.0 - smoothstep(0.0, thick, abs(r - radius));

    // nucleo quente que apaga rapido
    float core = (1.0 - smoothstep(0.0, radius * 0.85, r)) * pow(1.0 - k, 2.0);

    // tufos de fumaca, senao vira um anel liso de novo
    float puff = 0.45 + 0.85 * (fbm(uv * 3.2 + vec2(seed * 1.7) - k * 0.6) * 0.5 + 0.5);

    float dens = (shell * (1.0 - k) * puff) + core * 0.9;
    dens *= 1.0 - smoothstep(0.85, 1.0, r); // corta antes da borda do quad
    dens = clamp(dens, 0.0, 1.0);

    vec3 c = mix(color_edge, color_core, smoothstep(0.15, 0.75, dens + core));

    COLOR = vec4(c * dens, 1.0);
}
";
		return shader;
	}
}
