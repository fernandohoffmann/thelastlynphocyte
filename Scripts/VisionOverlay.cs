// VisionOverlay.cs — escurece tudo que esta atras de uma parede de veia.
//
// Para cada pixel da tela, o shader marcha do player ate aquele ponto lendo a
// mascara do VeinMap. Se encontrar parede no caminho, o pixel fica escondido.
// A face da parede continua visivel porque a marcha para um pouco antes do alvo.
using Godot;

public partial class VisionOverlay : CanvasLayer
{
	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	private const float DARKNESS  = 0.975f; // 1.0 = escuridao total atras da parede
	private const float MAX_VIEW  = 1500f;  // alcance da visao em px de mundo
	private const float STOP_GAP  = 26f;    // o quanto a marcha para antes do alvo
	private const float STEP_LEN  = 20f;    // passo da marcha em px de mundo (menor = mais suave)
	// ──────────────────────────────────────────────────────────────────────

	private ColorRect      _rect;
	private ShaderMaterial _mat;

	public override void _Ready()
	{
		Layer = 3; // acima do mundo, abaixo da vinheta (4) e do HUD (5)

		_rect = new ColorRect();
		_rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_rect.MouseFilter = Control.MouseFilterEnum.Ignore;
		_rect.Visible     = false;

		_mat = new ShaderMaterial { Shader = BuildShader() };
		_mat.SetShaderParameter("darkness", DARKNESS);
		_mat.SetShaderParameter("max_view", MAX_VIEW);
		_mat.SetShaderParameter("stop_gap", STOP_GAP);
		_mat.SetShaderParameter("step_len", STEP_LEN);

		_rect.Material = _mat;
		AddChild(_rect);
	}

	public override void _Process(double delta)
	{
		var map = VeinMap.Instance;
		var cam = GetViewport()?.GetCamera2D();

		if (map == null || cam == null || map.MaskTexture == null || Player.Instance == null)
		{
			_rect.Visible = false;
			return;
		}

		_rect.Visible = true;

		Rect2 b = map.Bounds;

		_mat.SetShaderParameter("mask",        map.MaskTexture);
		_mat.SetShaderParameter("map_origin",  b.Position);
		_mat.SetShaderParameter("map_size",    b.Size);
		_mat.SetShaderParameter("cam_center",  cam.GetScreenCenterPosition());
		_mat.SetShaderParameter("world_scale", 1f / Mathf.Max(0.0001f, cam.Zoom.X));
		_mat.SetShaderParameter("eye",         Player.Instance.GlobalPosition);
	}

	private static Shader BuildShader()
	{
		var shader = new Shader();
		shader.Code = @"
shader_type canvas_item;

uniform sampler2D mask : filter_linear, repeat_disable;

uniform vec2  map_origin  = vec2(0.0);
uniform vec2  map_size    = vec2(1.0);
uniform vec2  cam_center  = vec2(0.0);
uniform float world_scale = 1.0;
uniform vec2  eye         = vec2(0.0);

uniform float darkness : hint_range(0.0, 1.0) = 0.975;
uniform float max_view = 1500.0;
uniform float stop_gap = 26.0;
uniform float step_len = 20.0;

const int MAX_STEPS = 72;

float wallAt(vec2 world) {
    vec2 uv = (world - map_origin) / map_size;
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) return 0.0;
    // campo: <0.5 = dentro da parede. Faixa larga = transicao macia.
    return 1.0 - smoothstep(0.42, 0.52, texture(mask, uv).r);
}

// ruido barato por pixel, usado pra quebrar o degrau da marcha
float hash12(vec2 p) {
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}

void fragment() {
    vec2 world = (SCREEN_UV - vec2(0.5)) / SCREEN_PIXEL_SIZE * world_scale + cam_center;

    vec2  d    = world - eye;
    float dist = length(d);

    float blocked = 0.0;
    float limit   = dist - stop_gap;

    if (limit > 0.0) {
        vec2 dir = d / max(dist, 0.001);

        // cada pixel comeca a marcha num ponto ligeiramente diferente: o degrau
        // do passo vira granulado fino em vez de escada visivel
        float jitter = hash12(SCREEN_UV / SCREEN_PIXEL_SIZE);
        float s = step_len * (0.25 + 0.75 * jitter);

        for (int i = 0; i < MAX_STEPS; i++) {
            if (s >= limit) break;

            blocked = max(blocked, wallAt(eye + dir * s));
            if (blocked > 0.995) break;

            s += step_len;
        }
    }

    // alem do alcance tambem escurece, com transicao bem longa
    float far = smoothstep(max_view * 0.5, max_view, dist);

    float hide = clamp(max(blocked, far), 0.0, 1.0);
    hide = smoothstep(0.02, 0.98, hide); // tira os extremos duros

    COLOR = vec4(0.02, 0.0, 0.01, hide * darkness);
}
";
		return shader;
	}
}
