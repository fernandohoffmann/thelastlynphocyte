// AmbientShading.cs — manchas de sombra e de luz pelo cenario (agora travadas no mundo)
using Godot;

public partial class AmbientShading : CanvasLayer
{
	private ColorRect      _shadowRect;
	private ColorRect      _lightRect;
	private ShaderMaterial _shadowMat;
	private ShaderMaterial _lightMat;

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	private const float NOISE_SCALE = 0.0016f; // menor = manchas maiores
	private const float DARKNESS    = 0.38f;   // forca das sombras
	private const float BRIGHTNESS  = 0.10f;   // forca das manchas de luz
	// ──────────────────────────────────────────────────────────────────────

	private const string NOISE_LIB = @"
vec2 hash2(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return fract(sin(p) * 43758.5453);
}
float valueNoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    float a = dot(hash2(i) - 0.5, vec2(1.0));
    float b = dot(hash2(i + vec2(1.0,0.0)) - 0.5, vec2(1.0));
    float c = dot(hash2(i + vec2(0.0,1.0)) - 0.5, vec2(1.0));
    float d = dot(hash2(i + vec2(1.0,1.0)) - 0.5, vec2(1.0));
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
vec2 worldPos(vec2 suv, vec2 spx, vec2 off, float wscale) {
    return (suv - vec2(0.5)) / spx * wscale + off;
}
";

	public override void _Ready()
	{
		Layer = 2; // abaixo do VeinOverlay (3), Vignette (4) e HUD (5)

		// ── Camada de sombra (escurece regioes) ──────────────────
		_shadowRect = new ColorRect();
		_shadowRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_shadowRect.MouseFilter = Control.MouseFilterEnum.Ignore;

		var shadowShader = new Shader();
		shadowShader.Code = @"
shader_type canvas_item;

uniform vec2  world_offset = vec2(0.0);
uniform float world_scale  = 1.0;
uniform float scale    : hint_range(0.0005, 0.01) = 0.0016;
uniform float darkness : hint_range(0.0, 1.0)     = 0.38;
" + NOISE_LIB + @"
void fragment() {
    vec2 world = worldPos(SCREEN_UV, SCREEN_PIXEL_SIZE, world_offset, world_scale);
    float n = fbm(world * scale + vec2(TIME * 0.004, TIME * 0.003));
    float shadow = clamp(-n * 1.5, 0.0, 1.0) * darkness;
    COLOR = vec4(0.0, 0.0, 0.01, shadow);
}
";
		_shadowMat = new ShaderMaterial();
		_shadowMat.Shader = shadowShader;
		_shadowMat.SetShaderParameter("scale",    NOISE_SCALE);
		_shadowMat.SetShaderParameter("darkness", DARKNESS);
		_shadowRect.Material = _shadowMat;
		AddChild(_shadowRect);

		// ── Camada de luz (clareia regioes, soma brilho) ─────────
		_lightRect = new ColorRect();
		_lightRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_lightRect.MouseFilter = Control.MouseFilterEnum.Ignore;

		var lightShader = new Shader();
		lightShader.Code = @"
shader_type canvas_item;
render_mode blend_add;

uniform vec2  world_offset = vec2(0.0);
uniform float world_scale  = 1.0;
uniform float scale      : hint_range(0.0005, 0.01) = 0.0016;
uniform float brightness : hint_range(0.0, 1.0)     = 0.10;
uniform vec3  light_color : source_color = vec3(1.0, 0.22, 0.12);
" + NOISE_LIB + @"
void fragment() {
    vec2 world = worldPos(SCREEN_UV, SCREEN_PIXEL_SIZE, world_offset, world_scale);
    float n = fbm(world * scale + vec2(TIME * 0.004, TIME * 0.003));
    float light = clamp(n * 1.5, 0.0, 1.0) * brightness;
    COLOR = vec4(light_color * light, 1.0);
}
";
		_lightMat = new ShaderMaterial();
		_lightMat.Shader = lightShader;
		_lightMat.SetShaderParameter("scale",      NOISE_SCALE);
		_lightMat.SetShaderParameter("brightness", BRIGHTNESS);
		_lightRect.Material = _lightMat;
		AddChild(_lightRect);
	}

	public override void _Process(double delta)
	{
		var cam = GetViewport()?.GetCamera2D();
		if (cam == null) return;

		Vector2 center = cam.GetScreenCenterPosition();
		float   wscale = 1f / Mathf.Max(0.0001f, cam.Zoom.X);

		_shadowMat.SetShaderParameter("world_offset", center);
		_shadowMat.SetShaderParameter("world_scale",  wscale);
		_lightMat.SetShaderParameter("world_offset",  center);
		_lightMat.SetShaderParameter("world_scale",   wscale);
	}
}
