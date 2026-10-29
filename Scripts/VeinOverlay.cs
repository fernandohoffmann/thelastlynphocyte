// VeinOverlay.cs — veias grandes, esparsas e vermelho-escuras (estilo "tecido vivo")
// Substitui a versao antiga (celulas pequenas com nucleo amarelo/branco).
using Godot;

public partial class VeinOverlay : Node2D
{
	private ColorRect      _rect;
	private ShaderMaterial _mat;

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	private const float CELL_SIZE     = 950f;    // maior = MENOS veias na tela
	private const float INTENSITY     = 0.55f;   // brilho geral das veias
	private const float HALO_WIDTH    = 0.060f;  // largura do brilho suave
	private const float CORE_WIDTH    = 0.0045f; // largura do fio brilhante
	private const float SMOKE_WIDTH   = 0.26f;   // nevoa larga em volta da veia
	private const float WARP_STRENGTH = 0.55f;   // o quanto as veias entortam
	// ──────────────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		// No mundo (e nao numa CanvasLayer) pra poder ficar ABAIXO da parede de veia.
		// Numa CanvasLayer ele sempre desenharia por cima de tudo.
		ZIndex = 1;

		_rect = new ColorRect();
		_rect.MouseFilter = Control.MouseFilterEnum.Ignore;

		var shader = new Shader();
		shader.Code = @"
shader_type canvas_item;
render_mode blend_add;

// posicao do centro da camera no mundo + quanto vale 1 pixel de tela em mundo
uniform vec2  world_offset = vec2(0.0);
uniform float world_scale  = 1.0;

uniform float cell_size     : hint_range(200.0, 3000.0) = 950.0;
uniform float warp_strength : hint_range(0.0, 1.5)      = 0.55;
uniform float intensity     : hint_range(0.0, 2.0)      = 0.55;
uniform float halo_width    : hint_range(0.01, 0.30)    = 0.060;
uniform float core_width    : hint_range(0.001, 0.05)   = 0.0045;
uniform float smoke_width   : hint_range(0.05, 0.60)    = 0.26;

uniform vec3 vein_color : source_color = vec3(0.85, 0.05, 0.04);
uniform vec3 core_color : source_color = vec3(1.00, 0.28, 0.16);

vec2 hash2(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return fract(sin(p) * 43758.5453);
}

float valueNoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    float n00 = dot(hash2(i + vec2(0.0, 0.0)) - 0.5, vec2(1.0));
    float n10 = dot(hash2(i + vec2(1.0, 0.0)) - 0.5, vec2(1.0));
    float n01 = dot(hash2(i + vec2(0.0, 1.0)) - 0.5, vec2(1.0));
    float n11 = dot(hash2(i + vec2(1.0, 1.0)) - 0.5, vec2(1.0));
    return mix(mix(n00, n10, u.x), mix(n01, n11, u.x), u.y);
}

float fbm(vec2 p) {
    float v = 0.0;
    float amp = 0.5;
    for (int i = 0; i < 3; i++) {
        v += valueNoise(p) * amp;
        p *= 2.03;
        amp *= 0.5;
    }
    return v;
}

float voronoiEdge(vec2 uv) {
    vec2 i_uv = floor(uv);
    vec2 f_uv = fract(uv);
    float d1 = 8.0;
    float d2 = 8.0;
    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            vec2 neighbor = vec2(float(x), float(y));
            vec2 pt = hash2(i_uv + neighbor);
            pt = 0.5 + 0.42 * sin(TIME * 0.05 + 6.2831 * pt);
            vec2 diff = neighbor + pt - f_uv;
            float d = length(diff);
            if (d < d1) { d2 = d1; d1 = d; }
            else if (d < d2) { d2 = d; }
        }
    }
    return d2 - d1;
}

void fragment() {
    // Coordenada de MUNDO real (respeita o zoom da camera).
    // Sem isso as veias escorregam por cima do tecido quando o player anda.
    vec2 world = (SCREEN_UV - vec2(0.5)) / SCREEN_PIXEL_SIZE * world_scale + world_offset;
    vec2 uv = world / cell_size;

    // o ruido do warp varia na MESMA escala do grid, senao a aresta fica reta
    vec2 warp = vec2(
        valueNoise(uv * 1.25 + vec2(0.0, 37.0) + TIME * 0.010),
        valueNoise(uv * 1.25 + vec2(58.0, 0.0) - TIME * 0.008)
    ) * warp_strength;

    uv += warp;

    float edge = voronoiEdge(uv);

    float core  = 1.0 - smoothstep(0.0, core_width,  edge);
    float halo  = 1.0 - smoothstep(0.0, halo_width,  edge);
    float smoke = 1.0 - smoothstep(0.0, smoke_width, edge);

    // nem todo trecho da veia brilha igual — deixa o traco organico
    float mask = 0.30 + 0.70 * clamp(fbm(uv * 1.8 + vec2(11.0, 5.0) + TIME * 0.02) * 1.6 + 0.5, 0.0, 1.0);

    float glow = smoke * 0.16 + halo * 0.55 + core * 0.85;
    glow *= mask * intensity;

    vec3 col = mix(vein_color, core_color, core * 0.85);

    // alpha = 1.0 porque em blend_add o resultado e dst + rgb * alpha
    COLOR = vec4(col * glow, 1.0);
}
";
		_mat = new ShaderMaterial();
		_mat.Shader = shader;

		_mat.SetShaderParameter("cell_size",     CELL_SIZE);
		_mat.SetShaderParameter("intensity",     INTENSITY);
		_mat.SetShaderParameter("halo_width",    HALO_WIDTH);
		_mat.SetShaderParameter("core_width",    CORE_WIDTH);
		_mat.SetShaderParameter("smoke_width",   SMOKE_WIDTH);
		_mat.SetShaderParameter("warp_strength", WARP_STRENGTH);

		_rect.Material = _mat;
		AddChild(_rect);
	}

	public override void _Process(double delta)
	{
		var cam = GetViewport()?.GetCamera2D();
		if (cam == null || _mat == null) return;

		float zoom = Mathf.Max(0.0001f, cam.Zoom.X);
		Vector2 center = cam.GetScreenCenterPosition();

		// o retangulo acompanha a camera cobrindo exatamente a tela
		Vector2 worldSize = GetViewportRect().Size / zoom;
		_rect.Size     = worldSize;
		_rect.Position = ToLocal(center - worldSize * 0.5f);

		_mat.SetShaderParameter("world_offset", center);
		_mat.SetShaderParameter("world_scale",  1f / zoom);
	}
}
