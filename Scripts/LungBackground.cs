// LungBackground.cs — alveolos (Voronoi por shader) e bronquios da fase 2.
//
// Puramente visual, sem colisao nenhuma. Tudo respira junto porque le o
// LungBreath, a mesma fonte que arrasta o jogador.
using Godot;
using System.Collections.Generic;

public partial class LungBackground : Node2D
{
	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	private const float ALVEOLI_SIZE = 260f;  // tamanho medio de um alveolo
	private const float ALVEOLI_GLOW = 0.55f; // brilho das paredes alveolares
	private const float BREATH_ZOOM  = 0.10f; // o quanto incham ao encher

	private const int   TREES        = 5;     // bronquios espalhados
	private const int   MAX_DEPTH    = 4;
	private const float TRUNK_LEN    = 420f;
	private const float LEN_SHRINK   = 0.70f;
	private const float TRUNK_WIDTH  = 30f;
	private const float WIDTH_SHRINK = 0.68f;
	private const float SPREAD       = 0.60f;
	private const float SWAY_AMOUNT  = 7f;
	private const float MIN_SPACING  = 900f;  // distancia minima entre troncos

	private static readonly Color BRONCHI = new Color(0.58f, 0.30f, 0.80f, 0.42f);
	private static readonly Color GLOW    = new Color(0.50f, 0.26f, 0.82f, 0.16f);
	// ──────────────────────────────────────────────────────────────────────

	private struct Branch
	{
		public Vector2 A, B;
		public float   Width;
		public float   Phase;
	}

	private readonly List<Branch> _branches = new();

	private ColorRect      _alveoli;
	private ShaderMaterial _mat;
	private float          _time = 0f;

	public override void _Ready()
	{
		ZIndex = -8;   // acima do tecido, abaixo de tudo que anda
		BuildAlveoli();
		BuildBronchi();
	}

	// ══════════════════════════════════════════════════════════════════════
	// Alveolos: celulas de Voronoi cobrindo a tela, inchando com a respiracao
	// ══════════════════════════════════════════════════════════════════════
	private void BuildAlveoli()
	{
		_mat = new ShaderMaterial { Shader = BuildShader() };
		_mat.SetShaderParameter("cell_size", ALVEOLI_SIZE);
		_mat.SetShaderParameter("intensity", ALVEOLI_GLOW);

		_alveoli = new ColorRect
		{
			Name        = "Alveoli",
			Material    = _mat,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		AddChild(_alveoli);
	}

	private static Shader BuildShader()
	{
		var shader = new Shader();
		shader.Code = @"
shader_type canvas_item;
render_mode blend_add;

uniform vec2  world_offset = vec2(0.0);
uniform float world_scale  = 1.0;

uniform float cell_size : hint_range(80.0, 900.0) = 260.0;
uniform float intensity : hint_range(0.0, 2.0)    = 0.55;
uniform float breath    : hint_range(0.0, 1.0)    = 0.5;
uniform float zoom_amt  : hint_range(0.0, 0.4)    = 0.10;

uniform vec3 wall_color : source_color = vec3(0.72, 0.42, 1.00);
uniform vec3 air_color  : source_color = vec3(0.30, 0.12, 0.55);

vec2 hash2(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return fract(sin(p) * 43758.5453);
}

// distancia ate a aresta mais proxima: e ela que vira a parede do alveolo
float voronoiEdge(vec2 uv, out float cellId) {
    vec2 i_uv = floor(uv);
    vec2 f_uv = fract(uv);

    float d1 = 8.0;
    float d2 = 8.0;
    vec2  best = vec2(0.0);

    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            vec2 n  = vec2(float(x), float(y));
            vec2 pt = hash2(i_uv + n);
            // um leve deslocamento por celula deixa o retalho irregular
            pt = 0.5 + 0.40 * sin(6.2831 * pt);
            float d = length(n + pt - f_uv);

            if (d < d1) { d2 = d1; d1 = d; best = i_uv + n; }
            else if (d < d2) { d2 = d; }
        }
    }

    cellId = fract(sin(dot(best, vec2(12.9898, 78.233))) * 43758.5453);
    return d2 - d1;
}

void fragment() {
    vec2 world = (SCREEN_UV - vec2(0.5)) / SCREEN_PIXEL_SIZE * world_scale + world_offset;

    // inspirar aproxima as celulas (elas incham); expirar afasta
    float zoom = 1.0 - zoom_amt * (breath - 0.5) * 2.0;
    vec2  uv   = world / (cell_size * zoom);

    float id;
    float edge = voronoiEdge(uv, id);

    // parede do alveolo
    float wall = 1.0 - smoothstep(0.0, 0.10, edge);
    // interior: cada alveolo com um brilho proprio, respirando junto
    float air  = (1.0 - smoothstep(0.10, 0.55, edge)) * (0.25 + 0.45 * id);

    float k = 0.55 + 0.45 * breath;   // tudo acende ao encher de ar

    vec3 c = wall_color * wall * 1.0 + air_color * air * 0.8;

    COLOR = vec4(c * intensity * k, 1.0);
}
";
		return shader;
	}

	// ══════════════════════════════════════════════════════════════════════
	// Bronquios
	// ══════════════════════════════════════════════════════════════════════
	private void BuildBronchi()
	{
		_branches.Clear();

		Rect2 area = GameManager.Instance != null
			? GameManager.Instance.PlayableArea
			: new Rect2(-3000, -3000, 6000, 6000);

		var used = new List<Vector2>();

		for (int i = 0; i < TREES; i++)
		{
			Vector2 origin = Vector2.Zero;

			// espaca os troncos: sem isso varias arvores nasciam coladas e as
			// ramificacoes ficavam empilhadas umas sobre as outras
			for (int tries = 0; tries < 40; tries++)
			{
				origin = new Vector2(
					(float)GD.RandRange(area.Position.X + 600, area.End.X - 600),
					(float)GD.RandRange(area.Position.Y + 600, area.End.Y - 600));

				bool ok = true;
				foreach (var u in used)
					if (u.DistanceTo(origin) < MIN_SPACING) { ok = false; break; }

				if (ok) break;
			}
			used.Add(origin);

			Grow(origin, (float)GD.RandRange(0.0, Mathf.Tau), TRUNK_LEN, TRUNK_WIDTH, 0);
		}

		GD.Print($"[LungBackground] {_branches.Count} ramos bronquicos");
	}

	private void Grow(Vector2 from, float angle, float len, float width, int depth)
	{
		Vector2 to = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * len;

		_branches.Add(new Branch
		{
			A     = from,
			B     = to,
			Width = width,
			Phase = (float)GD.RandRange(0.0, Mathf.Tau)
		});

		if (depth >= MAX_DEPTH) return;

		float spread = SPREAD * (float)GD.RandRange(0.7, 1.3);

		Grow(to, angle - spread + (float)GD.RandRange(-0.15, 0.15),
			 len * LEN_SHRINK, width * WIDTH_SHRINK, depth + 1);
		Grow(to, angle + spread + (float)GD.RandRange(-0.15, 0.15),
			 len * LEN_SHRINK, width * WIDTH_SHRINK, depth + 1);
	}

	// ══════════════════════════════════════════════════════════════════════
	public override void _Process(double delta)
	{
		_time += (float)delta;

		var cam = GetViewport()?.GetCamera2D();
		if (cam != null && _alveoli != null)
		{
			float   zoom      = Mathf.Max(0.0001f, cam.Zoom.X);
			Vector2 center    = cam.GetScreenCenterPosition();
			Vector2 worldSize = GetViewportRect().Size / zoom;

			// o retangulo acompanha a camera cobrindo exatamente a tela
			_alveoli.Size     = worldSize;
			_alveoli.Position = ToLocal(center - worldSize * 0.5f);

			_mat.SetShaderParameter("world_offset", center);
			_mat.SetShaderParameter("world_scale",  1f / zoom);
			_mat.SetShaderParameter("breath",       LungBreath.Instance?.Volume ?? 0.5f);
			_mat.SetShaderParameter("zoom_amt",     BREATH_ZOOM);
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		float breath = LungBreath.Instance?.Volume ?? 0.5f;
		float k      = 0.7f + 0.3f * breath;

		foreach (var br in _branches)
		{
			// ramo fino balanca mais que o tronco
			float amp = SWAY_AMOUNT * (1f - br.Width / TRUNK_WIDTH) + 2f;

			Vector2 sway = new Vector2(
				Mathf.Sin(_time * 0.45f * Mathf.Tau + br.Phase),
				Mathf.Cos(_time * 0.37f * Mathf.Tau + br.Phase)) * amp;

			Vector2 a = br.A + sway * 0.25f;
			Vector2 b = br.B + sway;

			DrawLine(a, b, new Color(GLOW.R, GLOW.G, GLOW.B, GLOW.A * k), br.Width * 2.0f);
			DrawLine(a, b, new Color(BRONCHI.R, BRONCHI.G, BRONCHI.B, BRONCHI.A * k), br.Width);
		}
	}
}
