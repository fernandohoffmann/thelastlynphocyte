// VeinMap.cs — rede de veias que funcionam como PAREDE.
//
// Modelo: a veia e um tubo fino (WALL_HALF de meia-espessura) que bloqueia.
// Todo o resto do mapa e andavel. Alguns trechos viram passagem (GAP_CHANCE)
// pra nunca existir uma area completamente isolada.
//
//  1. sorteia ramos partindo do centro (random walk com bifurcacoes)
//  2. rasteriza um campo com sinal: 0.5 = superficie da parede, <0.5 = dentro dela
//  3. shader desenha o tubo (pulsa e ondula de leve)
//  4. marching squares no mesmo campo gera a colisao
using Godot;
using System.Collections.Generic;

public partial class VeinMap : Node2D
{
	public static VeinMap Instance { get; private set; }

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	private const int   MASK_RES  = 512;   // resolucao do campo (potencia de 2)
	private const int   COLL_STEP = 2;     // 1 = colisao mais fiel, 4 = mais leve
	private const float BAND      = 70f;   // largura do degrade em volta da parede

	private const float WALL_HALF      = 55f;  // meia-espessura da veia (parede)
	private const float WALL_MIN       = 28f;  // meia-espessura minima nas pontas
	private const float SHRINK         = 0.95f;// o quanto a veia afina por trecho

	private const int   MAIN_BRANCHES  = 4;    // veias saindo do centro
	private const int   MAX_DEPTH      = 2;    // niveis de bifurcacao
	private const float BRANCH_CHANCE  = 0.30f;
	private const float STEP_LEN       = 420f; // ESPACO entre as veias: suba pra abrir mais
	private const float TURN           = 0.50f;// o quanto entorta por trecho (radianos)
	private const float GAP_CHANCE     = 0.18f;// trechos que viram passagem aberta
	private const int   SMOOTH_PASSES  = 3;    // passadas de Chaikin (mais = mais redondo)

	// Se as veias sao PAREDE (procedural) ou CAMINHO (mapa pintado com Line2D).
	// No modo pintado isso e forcado pra VeinsAreTunnels.
	public enum VeinMode { VeinsAreWalls, VeinsAreTunnels }
	private VeinMode _mode = VeinMode.VeinsAreWalls;
	public  bool VeinsAreWalls => _mode == VeinMode.VeinsAreWalls;

	private const float PAINTED_FALLBACK_WIDTH = 260f; // se a Line2D estiver com width 0
	private const bool  CONNECT_REGIONS = true;  // false = respeita 100% o mapa pintado

	// O brilho da parede se espalha alem do nucleo (BAND + o bloom do WorldEnvironment).
	// Sem esse acrescimo a colisao fica so no miolo e da pra entrar na parte acesa.
	//   veia = parede  -> vira raio a mais na capsula
	//   veia = caminho -> vira limiar mais alto no marching squares (encolhe o corredor)
	// Limite util: BAND. Alem disso o campo satura e o acrescimo nao tem efeito.
	private const float COLLIDE_PAD = 45f;

	// Raio (em texels) do borrao aplicado no campo depois de rasterizar.
	// A uniao de duas veias e um max() puro: onde elas se cruzam, sobra uma
	// quina viva. Borrar o campo uma vez arredonda todo canto concavo de uma
	// so tacada, e como colisao e visual saem do MESMO campo, os dois batem.
	private const int   ROUND_BLUR = 3;
	// ── Flow field (pathfinding) ──
	private const int   FLOW_STEP     = 4;    // texels por celula do campo (4 = 128x128)
	// folga do caminho: a da colisao + espaco pro corpo do inimigo
	private const float FLOW_MARGIN   = 60f;
	private const float FLOW_INTERVAL = 0.15f;// tempo minimo entre recalculos

	public  const int   WALL_Z = 20;   // z da parede (o resto do mundo fica abaixo)
	private const float CARVE_RADIUS   = 110f; // largura da passagem aberta pra ligar regioes
	private const bool  DEBUG_COLLISION = false; // true = desenha o contorno da colisao

	private static Color WALL_DEEP => GamePhase.Data.WallDeep;
	private static Color WALL_EDGE => GamePhase.Data.WallEdge;
	private static Color RIM_COLOR => GamePhase.Data.WallRim;
	// ──────────────────────────────────────────────────────────────────────

	public struct VeinSeg
	{
		public Vector2 A, B;
		public float   Ra, Rb;
	}

	private readonly List<VeinSeg> _segs = new List<VeinSeg>();
	private float[]       _field;     // MASK_RES², 1 = livre, <0.5 = dentro da parede
	private List<Vector2> _collPts;   // contorno (pares de pontos) — usado no debug
	private int[]         _region;    // por texel: -1 parede, >=0 id da regiao
	private int           _regionCount;
	private Rect2         _bounds;
	private Vector2       _texelSize;

	public Texture2D MaskTexture { get; private set; } // usada pelo VisionOverlay
	public Rect2     Bounds      => _bounds;

	public Vector2 SpawnPoint { get; private set; } = Vector2.Zero;

	private bool[]    _flowOpen;   // celula e andavel?
	private int[]     _flowDist;   // distancia ate o alvo em passos
	private Vector2[] _flowDir;    // pra onde andar
	private int       _flowN;
	private Vector2   _flowCell;   // tamanho da celula em px de mundo
	private int       _flowTargetCell = -1;
	private float     _flowTimer;

	/// Usado pelo minimapa pra desenhar as veias reveladas.
	public IReadOnlyList<VeinSeg> Segments => _segs;
	public float WallHalfThickness => WALL_HALF;

	public override void _ExitTree()
	{
		// Sem isto o Instance continua apontando pro no da fase anterior, ja
		// liberado. Na fase 2, que nao cria VeinMap, qualquer chamada nele
		// estoura ObjectDisposedException.
		if (Instance == this) Instance = null;
	}

	public override void _Ready()
	{
		Instance = this;
		ZIndex   = 0;
	}

	// Chamar depois que o GameManager ja sabe a PlayableArea.
	// 'painted' = um Node2D com Line2D dentro (mapa desenhado a mao no editor).
	// Se vier null, o mapa e sorteado como antes.
	public void Build(Rect2 bounds, Node2D painted = null)
	{
		_bounds    = bounds;
		_texelSize = bounds.Size / MASK_RES;

		ulong t0 = Time.GetTicksMsec();

		int painted_count = painted != null ? CollectPaintedVeins(painted) : 0;

		if (painted_count > 0)
		{
			_mode = VeinMode.VeinsAreTunnels; // o que foi pintado e CAMINHO
			GD.Print($"[VeinMap] usando mapa pintado ({painted_count} linhas)");
		}
		else
		{
			_mode = VeinMode.VeinsAreWalls;
			GD.Print("[VeinMap] nenhum 'World/VeinPaint' com Line2D — sorteando o mapa");
			GenerateVeins();
		}

		RasterizeField();
		BlurField(ROUND_BLUR);
		if (CONNECT_REGIONS) ConnectRegions();   // garante que da pra chegar em tudo
		BuildVisual();
		BuildCollision();

		BuildFlowGrid();

		// nasce longe de qualquer parede (margem sempre <= BAND, senao e impossivel)
		SpawnPoint = FindOpenSpotNear(_bounds.GetCenter(), 60f);

		GD.Print($"[VeinMap] {_segs.Count} trechos, {_regionCount} regiao(oes), " +
				 $"{Time.GetTicksMsec() - t0}ms");
	}

	/// Constroi o mapa da fase 2: dois lobos de pulmao.
	/// Reusa toda a maquinaria do modo "veia = caminho" — o lobo e so uma veia
	/// muito gorda, entao a uniao das capsulas ja da a silhueta arredondada.
	public void BuildLung(Rect2 bounds)
	{
		_bounds    = bounds;
		_texelSize = bounds.Size / MASK_RES;
		_mode      = VeinMode.VeinsAreTunnels;

		ulong t0 = Time.GetTicksMsec();

		GenerateLung();
		RasterizeField();
		BlurField(ROUND_BLUR);
		if (CONNECT_REGIONS) ConnectRegions();
		BuildVisual();
		BuildCollision();
		BuildFlowGrid();

		SpawnPoint = FindOpenSpotNear(_bounds.GetCenter(), 60f);

		GD.Print($"[VeinMap] pulmao: {_segs.Count} trechos, {_regionCount} regiao(oes), " +
				 $"{Time.GetTicksMsec() - t0}ms");
	}

	private void GenerateLung()
	{
		_segs.Clear();

		// Espinha de cada lobo em coordenadas normalizadas (0..1 da area),
		// com o raio em fracao da LARGURA. Mexer aqui muda o formato do pulmao.
		var spine = new (float x, float y, float r)[]
		{
			(0.415f, 0.14f, 0.070f),
			(0.330f, 0.24f, 0.115f),
			(0.278f, 0.38f, 0.140f),
			(0.268f, 0.53f, 0.140f),
			(0.300f, 0.68f, 0.120f),
			(0.365f, 0.80f, 0.085f),
		};

		AddLobe(spine, false);  // lobo esquerdo
		AddLobe(spine, true);   // lobo direito (espelhado)

		// traqueia: liga os dois lobos no topo, senao o ConnectRegions abriria
		// um buraco em algum lugar aleatorio pra conectar
		float cx = _bounds.Position.X + _bounds.Size.X * 0.5f;
		_segs.Add(new VeinSeg
		{
			A  = new Vector2(cx, _bounds.Position.Y + _bounds.Size.Y * 0.07f),
			B  = new Vector2(cx, _bounds.Position.Y + _bounds.Size.Y * 0.20f),
			Ra = _bounds.Size.X * 0.055f,
			Rb = _bounds.Size.X * 0.055f,
		});
	}

	private void AddLobe((float x, float y, float r)[] spine, bool mirror)
	{
		Vector2 origin = _bounds.Position;
		Vector2 size   = _bounds.Size;

		Vector2 P(int i)
		{
			float x = mirror ? 1f - spine[i].x : spine[i].x;
			return origin + new Vector2(x * size.X, spine[i].y * size.Y);
		}

		for (int i = 0; i < spine.Length - 1; i++)
		{
			_segs.Add(new VeinSeg
			{
				A  = P(i),
				B  = P(i + 1),
				Ra = spine[i].r     * size.X,
				Rb = spine[i + 1].r * size.X,
			});
		}
	}

	// ══════════════════════════════════════════════════════════════════════
	// 1. Rede de veias
	// ══════════════════════════════════════════════════════════════════════
	private void GenerateVeins()
	{
		_segs.Clear();
		Vector2 center = _bounds.GetCenter();

		float offset = (float)GD.RandRange(0.0, Mathf.Tau);
		for (int i = 0; i < MAIN_BRANCHES; i++)
		{
			float ang = offset + Mathf.Tau * i / MAIN_BRANCHES
					  + (float)GD.RandRange(-0.3, 0.3);

			// comeca afastado do centro, pro player nao nascer em cima de uma veia
			Vector2 start = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 320f;
			Grow(start, ang, WALL_HALF, 0);
		}
	}

	// Le todas as Line2D dentro do no e transforma cada trecho num segmento de veia.
	// A largura do tunel vem do 'width' da propria Line2D.
	private int CollectPaintedVeins(Node2D root)
	{
		_segs.Clear();
		int lines = 0;

		CollectLinesRecursive(root, ref lines);
		return lines;
	}

	private void CollectLinesRecursive(Node node, ref int lines)
	{
		if (node is Line2D line && line.Points.Length >= 2)
		{
			float half = (line.Width > 1f ? line.Width : PAINTED_FALLBACK_WIDTH) * 0.5f;
			half *= Mathf.Abs(line.GlobalScale.X); // respeita escala do no

			// os pontos da Line2D sao cliques soltos: o Chaikin arredonda as quinas
			var run = new List<Vector2>();
			foreach (var pt in line.Points)
				run.Add(line.ToGlobal(pt));

			EmitRun(run, half, half);
			lines++;
		}

		foreach (var child in node.GetChildren())
			CollectLinesRecursive(child, ref lines);
	}

	// Chaikin: corta os cantos do caminho. Duas passadas ja transformam
	// qualquer quina em curva, e e o que arredonda as regioes do mapa.
	private static List<Vector2> Chaikin(List<Vector2> pts, int iterations)
	{
		var cur = pts;

		for (int it = 0; it < iterations; it++)
		{
			if (cur.Count < 3) break;

			var next = new List<Vector2> { cur[0] };
			for (int i = 0; i < cur.Count - 1; i++)
			{
				Vector2 a = cur[i], b = cur[i + 1];
				next.Add(a.Lerp(b, 0.25f));
				next.Add(a.Lerp(b, 0.75f));
			}
			next.Add(cur[cur.Count - 1]);

			cur = next;
		}
		return cur;
	}

	// Emite os segmentos de um traco ja suavizado, afinando do inicio ao fim.
	private void EmitRun(List<Vector2> run, float halfStart, float halfEnd)
	{
		if (run.Count < 2) return;

		var smooth = Chaikin(run, SMOOTH_PASSES);
		int n = smooth.Count - 1;

		for (int i = 0; i < n; i++)
		{
			float ta = (float)i / n;
			float tb = (float)(i + 1) / n;

			_segs.Add(new VeinSeg
			{
				A  = smooth[i],
				B  = smooth[i + 1],
				Ra = Mathf.Lerp(halfStart, halfEnd, ta),
				Rb = Mathf.Lerp(halfStart, halfEnd, tb)
			});
		}
	}

	private void Grow(Vector2 pos, float angle, float half, int depth)
	{
		int steps = (int)GD.RandRange(5, 11);

		// o traco e montado inteiro primeiro, suavizado, e so entao vira segmento
		var run       = new List<Vector2> { pos };
		float runHalf = half;
		var branches  = new List<(Vector2 p, float a, float h)>();

		for (int i = 0; i < steps; i++)
		{
			angle += (float)GD.RandRange(-TURN, TURN);

			// perto da borda, curva de volta pro centro
			float margin = half + BAND + 150f;
			bool  nearEdge = pos.X < _bounds.Position.X + margin
						  || pos.X > _bounds.End.X      - margin
						  || pos.Y < _bounds.Position.Y + margin
						  || pos.Y > _bounds.End.Y      - margin;

			if (nearEdge)
				angle = Mathf.LerpAngle(angle, (_bounds.GetCenter() - pos).Angle(), 0.6f);

			float   len  = STEP_LEN * (float)GD.RandRange(0.75, 1.3);
			Vector2 next = pos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * len;

			float nextHalf = Mathf.Max(WALL_MIN, half * SHRINK);

			// Um buraco encerra o traco atual e comeca outro depois dele:
			// e assim que nasce uma passagem entre duas regioes.
			if (GD.Randf() < GAP_CHANCE)
			{
				EmitRun(run, runHalf, half);
				run     = new List<Vector2> { next };
				runHalf = nextHalf;
			}
			else
			{
				run.Add(next);
			}

			pos  = next;
			half = nextHalf;

			if (depth < MAX_DEPTH && GD.Randf() < BRANCH_CHANCE)
			{
				float side = GD.Randf() < 0.5f ? 1f : -1f;
				branches.Add((pos, angle + side * (float)GD.RandRange(0.7, 1.4), half * 0.85f));
			}
		}

		EmitRun(run, runHalf, half);

		foreach (var b in branches)
			Grow(b.p, b.a, b.h, depth + 1);
	}

	// ══════════════════════════════════════════════════════════════════════
	// 2. Campo com sinal (so a bbox de cada trecho e visitada: e rapido)
	// ══════════════════════════════════════════════════════════════════════
	private void RasterizeField()
	{
		bool veinIsWall = VeinsAreWalls;

		_field = new float[MASK_RES * MASK_RES];
		// veia = parede  -> comeca tudo livre e a veia RETIRA espaco
		// veia = caminho -> comeca tudo parede e a veia ABRE espaco
		float start = veinIsWall ? 1f : 0f;
		for (int i = 0; i < _field.Length; i++)
			_field[i] = start;

		foreach (var s in _segs)
		{
			float maxR = Mathf.Max(s.Ra, s.Rb) + BAND;

			Vector2 lo = new Vector2(Mathf.Min(s.A.X, s.B.X), Mathf.Min(s.A.Y, s.B.Y))
					   - new Vector2(maxR, maxR);
			Vector2 hi = new Vector2(Mathf.Max(s.A.X, s.B.X), Mathf.Max(s.A.Y, s.B.Y))
					   + new Vector2(maxR, maxR);

			int x0 = Mathf.Clamp(TexelX(lo.X), 0, MASK_RES - 1);
			int x1 = Mathf.Clamp(TexelX(hi.X), 0, MASK_RES - 1);
			int y0 = Mathf.Clamp(TexelY(lo.Y), 0, MASK_RES - 1);
			int y1 = Mathf.Clamp(TexelY(hi.Y), 0, MASK_RES - 1);

			for (int y = y0; y <= y1; y++)
			{
				for (int x = x0; x <= x1; x++)
				{
					Vector2 p = WorldOfTexel(x, y);

					float t;
					float d = DistToSegment(p, s.A, s.B, out t);
					float r = Mathf.Lerp(s.Ra, s.Rb, t);

					// Convencao unica do campo: >= 0.5 = andavel.
					// Parede: dentro da veia o valor cai. Tunel: dentro da veia sobe.
					float v = veinIsWall
						? 0.5f + 0.5f * Mathf.Clamp((d - r) / BAND, -1f, 1f)
						: 0.5f + 0.5f * Mathf.Clamp((r - d) / BAND, -1f, 1f);

					int idx = y * MASK_RES + x;
					if (veinIsWall) { if (v < _field[idx]) _field[idx] = v; }
					else            { if (v > _field[idx]) _field[idx] = v; }
				}
			}
		}
	}

	// Borrao separavel (horizontal + vertical). Duas passadas de media movel.
	private void BlurField(int radius)
	{
		if (radius <= 0) return;

		var tmp = new float[_field.Length];
		float inv = 1f / (radius * 2 + 1);

		// horizontal
		for (int y = 0; y < MASK_RES; y++)
		{
			int row = y * MASK_RES;
			for (int x = 0; x < MASK_RES; x++)
			{
				float sum = 0f;
				for (int k = -radius; k <= radius; k++)
					sum += _field[row + Mathf.Clamp(x + k, 0, MASK_RES - 1)];
				tmp[row + x] = sum * inv;
			}
		}

		// vertical
		for (int x = 0; x < MASK_RES; x++)
		{
			for (int y = 0; y < MASK_RES; y++)
			{
				float sum = 0f;
				for (int k = -radius; k <= radius; k++)
					sum += tmp[Mathf.Clamp(y + k, 0, MASK_RES - 1) * MASK_RES + x];
				_field[y * MASK_RES + x] = sum * inv;
			}
		}
	}

	private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b, out float t)
	{
		Vector2 ab   = b - a;
		float   len2 = ab.LengthSquared();
		t = len2 < 0.0001f ? 0f : Mathf.Clamp((p - a).Dot(ab) / len2, 0f, 1f);
		return p.DistanceTo(a + ab * t);
	}

	private int TexelX(float wx) => Mathf.FloorToInt((wx - _bounds.Position.X) / _texelSize.X);
	private int TexelY(float wy) => Mathf.FloorToInt((wy - _bounds.Position.Y) / _texelSize.Y);

	private Vector2 WorldOfTexel(int x, int y) => new Vector2(
		_bounds.Position.X + (x + 0.5f) * _texelSize.X,
		_bounds.Position.Y + (y + 0.5f) * _texelSize.Y);

	// ══════════════════════════════════════════════════════════════════════
	// 3. Visual
	// ══════════════════════════════════════════════════════════════════════
	private void BuildVisual()
	{
		var bytes = new byte[MASK_RES * MASK_RES * 4];
		for (int i = 0; i < _field.Length; i++)
		{
			byte v = (byte)Mathf.Clamp(Mathf.RoundToInt(_field[i] * 255f), 0, 255);
			bytes[i * 4 + 0] = v;
			bytes[i * 4 + 1] = v;
			bytes[i * 4 + 2] = v;
			bytes[i * 4 + 3] = 255;
		}

		var img = Image.CreateFromData(MASK_RES, MASK_RES, false, Image.Format.Rgba8, bytes);
		var tex = ImageTexture.CreateFromImage(img);
		MaskTexture = tex;

		var mat = new ShaderMaterial { Shader = BuildWallShader() };
		mat.SetShaderParameter("wall_deep", WALL_DEEP);
		mat.SetShaderParameter("wall_edge", WALL_EDGE);
		mat.SetShaderParameter("rim_color", RIM_COLOR);

		var spr = new Sprite2D
		{
			Name          = "Walls",
			Texture       = tex,
			Centered      = true,
			Position      = _bounds.GetCenter(),
			Scale         = _bounds.Size / MASK_RES,
			Material      = mat,
			TextureFilter = CanvasItem.TextureFilterEnum.Linear,
			// acima de tudo que anda no mundo: inimigos, hemoglobina e tiros
			// passam POR TRAS da parede em vez de por cima dela
			ZIndex        = WALL_Z
		};
		AddChild(spr);
	}

	private static Shader BuildWallShader()
	{
		var shader = new Shader();
		shader.Code = @"
shader_type canvas_item;

uniform vec3  wall_deep : source_color = vec3(0.30, 0.020, 0.030);
uniform vec3  wall_edge : source_color = vec3(0.85, 0.120, 0.110);
uniform vec3  rim_color : source_color = vec3(1.00, 0.32, 0.18);

uniform float wall_alpha   : hint_range(0.0, 1.0) = 1.00; // parede solida
uniform float rim_strength : hint_range(0.0, 3.0) = 1.25;
uniform float pulse_amount : hint_range(0.0, 0.1) = 0.015; // pulsacao da membrana
uniform float pulse_speed  : hint_range(0.0, 5.0) = 1.15;
uniform float warp_amount  : hint_range(0.0, 0.05)= 0.003; // ondulacao lenta
uniform float grain        : hint_range(0.0, 1.0) = 0.45;  // textura dentro da parede

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
        p *= 2.03;
        amp *= 0.5;
    }
    return v;
}

void fragment() {
    // ondulacao lenta: a veia parece viva, mas so alguns pixels
    vec2 w = vec2(
        fbm(UV * 7.0 + vec2(0.0, 0.0) + TIME * 0.035),
        fbm(UV * 7.0 + vec2(4.3, 1.7) - TIME * 0.030)
    );
    vec2 uv = UV + w * warp_amount;

    float v = texture(TEXTURE, uv).r;

    // a superficie respira (sistole/diastole)
    float surface = 0.5 + pulse_amount * sin(TIME * pulse_speed);

    // v < surface = dentro da parede
    float wallMask = 1.0 - smoothstep(surface - 0.02, surface + 0.02, v);

    // membrana acesa na borda da veia
    float rim = exp(-pow((v - surface) * 12.0, 2.0));

    // nucleo da veia mais escuro, beirada mais clara
    vec3 c = mix(wall_deep, wall_edge, smoothstep(surface - 0.40, surface, v));

    c *= 0.70 + grain * (fbm(uv * 26.0) * 0.5 + 0.5);
    c += rim_color * rim * rim_strength;

    float a = max(wallMask * wall_alpha, rim * 0.8);

    COLOR = vec4(c, clamp(a, 0.0, 1.0));
}
";
		return shader;
	}

	// ══════════════════════════════════════════════════════════════════════
	// 4. Colisao (marching squares -> segmentos -> ConcavePolygonShape2D)
	// ══════════════════════════════════════════════════════════════════════
	private void BuildCollision()
	{
		var body = new StaticBody2D
		{
			Name           = "VeinWalls",
			CollisionLayer = 8,   // mesma layer das paredes da borda
			CollisionMask  = 0
		};
		AddChild(body);

		MarchingSquares(); // sempre, serve de contorno pro debug

		if (VeinsAreWalls) BuildCapsuleCollision(body);
		else               BuildContourCollision(body);

		if (DEBUG_COLLISION) QueueRedraw();
	}

	// Quando a veia E a parede, a colisao exata sao capsulas: uma por trecho,
	// igualzinha ao tubo que o shader desenha. Mais simples e mais confiavel
	// que reconstruir o contorno.
	private void BuildCapsuleCollision(StaticBody2D body)
	{
		int made = 0;

		foreach (var s in _segs)
		{
			float len = s.A.DistanceTo(s.B);
			if (len < 1f) continue;

			float r = (s.Ra + s.Rb) * 0.5f + COLLIDE_PAD;

			var cap = new CapsuleShape2D
			{
				Radius = r,
				Height = len + r * 2f   // Height inclui as duas pontas arredondadas
			};

			body.AddChild(new CollisionShape2D
			{
				Shape    = cap,
				Position = (s.A + s.B) * 0.5f,
				// a capsula nasce deitada no eixo Y, por isso o +90 graus
				Rotation = (s.B - s.A).Angle() + Mathf.Pi * 0.5f
			});
			made++;
		}

		GD.Print($"[VeinMap] colisao: {made} capsulas");
	}

	// Mapa pintado: a parede e o complemento do desenho, entao a colisao vem
	// do contorno extraido pelo marching squares.
	private void BuildContourCollision(StaticBody2D body)
	{
		var shape = new ConcavePolygonShape2D { Segments = _collPts.ToArray() };
		body.AddChild(new CollisionShape2D { Shape = shape });

		GD.Print($"[VeinMap] colisao: {_collPts.Count / 2} segmentos de contorno " +
				 $"(pad {COLLIDE_PAD}px de {BAND}px de BAND)");
	}

	private void MarchingSquares()
	{
		_collPts = new List<Vector2>();

		// No modo pintado a parede e o complemento do desenho: subir o limiar
		// encolhe o corredor, ou seja, empurra a parede pra dentro do brilho.
		float T = VeinsAreWalls
			? 0.5f
			: 0.5f + 0.5f * Mathf.Clamp(COLLIDE_PAD / BAND, 0f, 0.9f);

		int n = MASK_RES / COLL_STEP;

		for (int j = 0; j < n - 1; j++)
		{
			for (int i = 0; i < n - 1; i++)
			{
				int x0 = i * COLL_STEP, y0 = j * COLL_STEP;
				int x1 = x0 + COLL_STEP, y1 = y0 + COLL_STEP;

				float v00 = _field[y0 * MASK_RES + x0];
				float v10 = _field[y0 * MASK_RES + x1];
				float v11 = _field[y1 * MASK_RES + x1];
				float v01 = _field[y1 * MASK_RES + x0];

				int code = 0;
				if (v00 > T) code |= 1;
				if (v10 > T) code |= 2;
				if (v11 > T) code |= 4;
				if (v01 > T) code |= 8;

				if (code == 0 || code == 15) continue;

				Vector2 p00 = WorldOfTexel(x0, y0);
				Vector2 p10 = WorldOfTexel(x1, y0);
				Vector2 p11 = WorldOfTexel(x1, y1);
				Vector2 p01 = WorldOfTexel(x0, y1);

				Vector2 top    = Interp(p00, p10, v00, v10, T);
				Vector2 right  = Interp(p10, p11, v10, v11, T);
				Vector2 bottom = Interp(p01, p11, v01, v11, T);
				Vector2 left   = Interp(p00, p01, v00, v01, T);

				switch (code)
				{
					case 1:  case 14: Add(left, top);      break;
					case 2:  case 13: Add(top, right);     break;
					case 3:  case 12: Add(left, right);    break;
					case 4:  case 11: Add(right, bottom);  break;
					case 6:  case 9:  Add(top, bottom);    break;
					case 7:  case 8:  Add(left, bottom);   break;
					case 5:  Add(left, top);   Add(right, bottom); break;
					case 10: Add(top, right);  Add(left, bottom);  break;
				}
			}
		}
	}

	// descarta segmentos degenerados: eles podem invalidar a shape inteira
	private void Add(Vector2 a, Vector2 b)
	{
		if (a.DistanceSquaredTo(b) < 0.01f) return;
		_collPts.Add(a);
		_collPts.Add(b);
	}

	private static Vector2 Interp(Vector2 pa, Vector2 pb, float va, float vb, float t)
	{
		float d = vb - va;
		if (Mathf.Abs(d) < 0.00001f) return pa;
		return pa.Lerp(pb, Mathf.Clamp((t - va) / d, 0f, 1f));
	}

	public override void _Draw()
	{
		if (!DEBUG_COLLISION || _collPts == null) return;

		var green = new Color(0.2f, 1f, 0.3f, 0.9f);
		for (int i = 0; i + 1 < _collPts.Count; i += 2)
			DrawLine(_collPts[i], _collPts[i + 1], green, 2f);
	}

	// ══════════════════════════════════════════════════════════════════════
	// 2b. Regioes e conectividade
	// ══════════════════════════════════════════════════════════════════════

	// Flood fill: agrupa os texels livres em regioes conectadas.
	private void ComputeRegions()
	{
		int n = MASK_RES * MASK_RES;
		_region = new int[n];

		const int WALL = -1, FREE = -2;
		for (int i = 0; i < n; i++)
			_region[i] = _field[i] >= 0.5f ? FREE : WALL;

		_regionCount = 0;
		var q = new Queue<int>();

		for (int start = 0; start < n; start++)
		{
			if (_region[start] != FREE) continue;

			int id = _regionCount++;
			_region[start] = id;
			q.Enqueue(start);

			while (q.Count > 0)
			{
				int c  = q.Dequeue();
				int cx = c % MASK_RES;
				int cy = c / MASK_RES;

				if (cx > 0            && _region[c - 1]        == FREE) { _region[c - 1]        = id; q.Enqueue(c - 1); }
				if (cx < MASK_RES - 1 && _region[c + 1]        == FREE) { _region[c + 1]        = id; q.Enqueue(c + 1); }
				if (cy > 0            && _region[c - MASK_RES] == FREE) { _region[c - MASK_RES] = id; q.Enqueue(c - MASK_RES); }
				if (cy < MASK_RES - 1 && _region[c + MASK_RES] == FREE) { _region[c + MASK_RES] = id; q.Enqueue(c + MASK_RES); }
			}
		}
	}

	// Abre passagens ate que toda regiao livre esteja ligada a maior delas.
	// BFS a partir da regiao principal atravessando TAMBEM as paredes: a primeira
	// vez que a onda toca uma regiao nova, o caminho de volta e a parede mais fina
	// entre as duas — e e exatamente ali que a passagem e furada.
	private void ConnectRegions()
	{
		ComputeRegions();
		if (_regionCount <= 1) return;

		int n = MASK_RES * MASK_RES;

		var size = new int[_regionCount];
		for (int i = 0; i < n; i++)
			if (_region[i] >= 0) size[_region[i]]++;

		int main = 0;
		for (int r = 1; r < _regionCount; r++)
			if (size[r] > size[main]) main = r;

		var connected = new bool[_regionCount];
		connected[main] = true;
		int remaining = _regionCount - 1;

		var parent  = new int[n];
		var visited = new bool[n];
		var q       = new Queue<int>();

		for (int i = 0; i < n; i++)
		{
			parent[i] = -1;
			if (_region[i] == main) { visited[i] = true; q.Enqueue(i); }
		}

		while (q.Count > 0 && remaining > 0)
		{
			int c  = q.Dequeue();
			int cx = c % MASK_RES;
			int cy = c / MASK_RES;

			if (cx > 0)            TryVisit(c - 1,        c, parent, visited, q, connected, ref remaining, main);
			if (cx < MASK_RES - 1) TryVisit(c + 1,        c, parent, visited, q, connected, ref remaining, main);
			if (cy > 0)            TryVisit(c - MASK_RES, c, parent, visited, q, connected, ref remaining, main);
			if (cy < MASK_RES - 1) TryVisit(c + MASK_RES, c, parent, visited, q, connected, ref remaining, main);
		}

		ComputeRegions(); // agora tudo virou uma regiao so
	}

	private void TryVisit(int nb, int from, int[] parent, bool[] visited,
						  Queue<int> q, bool[] connected, ref int remaining, int main)
	{
		if (visited[nb]) return;

		visited[nb] = true;
		parent[nb]  = from;
		q.Enqueue(nb);

		int rg = _region[nb];
		if (rg >= 0 && !connected[rg])
		{
			connected[rg] = true;
			remaining--;
			CarveBack(nb, parent, main);
		}
	}

	// Fura um corredor de volta pelo caminho do BFS ate encontrar a regiao principal.
	private void CarveBack(int cell, int[] parent, int main)
	{
		int c = cell;
		int guard = 0;

		while (c >= 0 && guard++ < MASK_RES * 4)
		{
			CarveDisk(c);
			if (_region[c] == main) break;
			c = parent[c];
		}
	}

	private void CarveDisk(int cell)
	{
		int cx = cell % MASK_RES;
		int cy = cell / MASK_RES;

		float rTex    = CARVE_RADIUS / Mathf.Max(_texelSize.X, 0.001f);
		float bandTex = BAND         / Mathf.Max(_texelSize.X, 0.001f);
		int   reach   = Mathf.CeilToInt(rTex + bandTex);

		for (int y = cy - reach; y <= cy + reach; y++)
		{
			if (y < 0 || y >= MASK_RES) continue;

			for (int x = cx - reach; x <= cx + reach; x++)
			{
				if (x < 0 || x >= MASK_RES) continue;

				float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

				// 1 no centro da passagem, 0.5 na borda dela, 0 fora.
				// Como so escrevemos quando o valor SOBE, isso abre espaco sem
				// nunca criar parede onde ja havia caminho.
				float v = 0.5f + 0.5f * Mathf.Clamp((rTex - d) / Mathf.Max(bandTex, 0.001f), -1f, 1f);

				int idx = y * MASK_RES + x;
				if (v > _field[idx]) _field[idx] = v;
			}
		}
	}

	// ══════════════════════════════════════════════════════════════════════
	// 5. Flow field: um unico BFS a partir do player serve TODOS os inimigos
	// ══════════════════════════════════════════════════════════════════════

	private void BuildFlowGrid()
	{
		_flowN    = MASK_RES / FLOW_STEP;
		_flowCell = _bounds.Size / _flowN;

		_flowOpen = new bool[_flowN * _flowN];
		_flowDist = new int[_flowN * _flowN];
		_flowDir  = new Vector2[_flowN * _flowN];

		int open = 0;
		for (int j = 0; j < _flowN; j++)
		{
			for (int i = 0; i < _flowN; i++)
			{
				bool ok = IsWalkable(FlowCenter(i, j), FLOW_MARGIN);
				_flowOpen[j * _flowN + i] = ok;
				if (ok) open++;
			}
		}

		GD.Print($"[VeinMap] flow field {_flowN}x{_flowN}, {open} celulas livres");
	}

	private Vector2 FlowCenter(int i, int j) => new Vector2(
		_bounds.Position.X + (i + 0.5f) * _flowCell.X,
		_bounds.Position.Y + (j + 0.5f) * _flowCell.Y);

	private int FlowIndex(Vector2 world)
	{
		int i = Mathf.FloorToInt((world.X - _bounds.Position.X) / _flowCell.X);
		int j = Mathf.FloorToInt((world.Y - _bounds.Position.Y) / _flowCell.Y);
		if (i < 0 || j < 0 || i >= _flowN || j >= _flowN) return -1;
		return j * _flowN + i;
	}

	public override void _Process(double delta)
	{
		if (_flowOpen == null || Player.Instance == null) return;

		_flowTimer -= (float)delta;

		int target = FlowIndex(Player.Instance.GlobalPosition);
		if (target < 0) return;

		// so recalcula quando o player troca de celula, e nunca mais rapido
		// que FLOW_INTERVAL
		if (target != _flowTargetCell && _flowTimer <= 0f)
		{
			_flowTargetCell = target;
			_flowTimer      = FLOW_INTERVAL;
			BuildFlowField(target);
		}
	}

	// BFS a partir da celula do player + direcao pro vizinho mais perto dele.
	private void BuildFlowField(int target)
	{
		int n = _flowN * _flowN;

		for (int i = 0; i < n; i++) _flowDist[i] = int.MaxValue;

		// se o player estiver colado numa parede a celula dele pode estar fechada;
		// nesse caso usa ela mesmo como origem pra nao travar tudo
		var q = new Queue<int>();
		_flowDist[target] = 0;
		q.Enqueue(target);

		while (q.Count > 0)
		{
			int c  = q.Dequeue();
			int cx = c % _flowN;
			int cy = c / _flowN;
			int d  = _flowDist[c] + 1;

			if (cx > 0)           TryFlow(c - 1,      d, q);
			if (cx < _flowN - 1)  TryFlow(c + 1,      d, q);
			if (cy > 0)           TryFlow(c - _flowN, d, q);
			if (cy < _flowN - 1)  TryFlow(c + _flowN, d, q);
		}

		// direcao = vizinho (dos 8) com a menor distancia
		for (int j = 0; j < _flowN; j++)
		{
			for (int i = 0; i < _flowN; i++)
			{
				int c = j * _flowN + i;
				_flowDir[c] = Vector2.Zero;

				if (!_flowOpen[c] || _flowDist[c] == int.MaxValue) continue;

				int best = _flowDist[c];
				int bi = i, bj = j;

				for (int dy = -1; dy <= 1; dy++)
				{
					for (int dx = -1; dx <= 1; dx++)
					{
						if (dx == 0 && dy == 0) continue;

						int nx = i + dx, ny = j + dy;
						if (nx < 0 || ny < 0 || nx >= _flowN || ny >= _flowN) continue;

						int nc = ny * _flowN + nx;
						if (!_flowOpen[nc]) continue;

						// diagonal so vale se os dois lados estiverem livres,
						// senao o bicho corta quina e entra na parede
						if (dx != 0 && dy != 0)
						{
							if (!_flowOpen[j * _flowN + nx] || !_flowOpen[ny * _flowN + i])
								continue;
						}

						if (_flowDist[nc] < best)
						{
							best = _flowDist[nc];
							bi = nx; bj = ny;
						}
					}
				}

				if (bi != i || bj != j)
					_flowDir[c] = new Vector2(bi - i, bj - j).Normalized();
			}
		}
	}

	private void TryFlow(int nb, int d, Queue<int> q)
	{
		if (!_flowOpen[nb] || _flowDist[nb] <= d) return;
		_flowDist[nb] = d;
		q.Enqueue(nb);
	}

	/// Direcao que leva ao player contornando as paredes.
	/// Vector2.Zero = sem caminho conhecido (o chamador que decida o que fazer).
	public Vector2 GetFlowDirection(Vector2 world)
	{
		if (_flowDir == null) return Vector2.Zero;

		int c = FlowIndex(world);
		if (c < 0) return Vector2.Zero;

		if (_flowDir[c] != Vector2.Zero) return _flowDir[c];

		// celula fechada (encostado na parede): pega a vizinha aberta mais util
		int cx = c % _flowN, cy = c / _flowN;
		for (int dy = -1; dy <= 1; dy++)
		{
			for (int dx = -1; dx <= 1; dx++)
			{
				int nx = cx + dx, ny = cy + dy;
				if (nx < 0 || ny < 0 || nx >= _flowN || ny >= _flowN) continue;

				var dir = _flowDir[ny * _flowN + nx];
				if (dir != Vector2.Zero) return dir;
			}
		}
		return Vector2.Zero;
	}

	// ══════════════════════════════════════════════════════════════════════
	// API
	// ══════════════════════════════════════════════════════════════════════

	/// true se da pra ir de 'a' ate 'b' em linha reta sem cruzar parede.
	/// E o teste que importa pros inimigos, que andam sempre em linha reta.
	public bool HasLineOfSight(Vector2 a, Vector2 b, float margin = 0f)
	{
		if (_field == null) return true;

		// A colisao fica no limiar do COLLIDE_PAD, nao em 0.5. Testar a visao
		// com margem 0 enxergava "caminho livre" atraves da faixa acesa da veia,
		// e o inimigo ia reto ate encostar na parede e travar ali.
		float m = Mathf.Max(margin, COLLIDE_PAD);

		float dist  = a.DistanceTo(b);
		int   steps = Mathf.Max(2, Mathf.CeilToInt(dist / 24f));

		for (int i = 1; i < steps; i++)
		{
			Vector2 p = a.Lerp(b, (float)i / steps);
			if (!IsWalkable(p, m)) return false;
		}
		return true;
	}

	/// Folga que a colisao real exige. Quem for testar caminho deve usar >= isto.
	public float CollisionMargin => COLLIDE_PAD;

	/// id da regiao conectada em que o ponto esta (-1 = parede ou fora do mapa).
	public int RegionAt(Vector2 world)
	{
		if (_region == null) return 0;

		int x = TexelX(world.X);
		int y = TexelY(world.Y);
		if (x < 0 || y < 0 || x >= MASK_RES || y >= MASK_RES) return -1;

		return _region[y * MASK_RES + x];
	}

	public bool SameRegion(Vector2 a, Vector2 b)
	{
		int ra = RegionAt(a);
		return ra >= 0 && ra == RegionAt(b);
	}

	/// margin em pixels de mundo: o quanto o ponto precisa estar longe da parede.
	public bool IsWalkable(Vector2 world, float margin = 0f)
	{
		if (_field == null) return true;

		int x = TexelX(world.X);
		int y = TexelY(world.Y);
		if (x < 0 || y < 0 || x >= MASK_RES || y >= MASK_RES) return false;

		// a margem so consegue exigir ate BAND px de folga: alem disso o campo
		// satura em 1.0 e a condicao viraria impossivel
		float need = Mathf.Min(1f, 0.5f + 0.5f * (margin / BAND));
		return _field[y * MASK_RES + x] >= need;
	}

	public Vector2 RandomWalkablePoint(float margin = 60f, int tries = 200)
	{
		for (int i = 0; i < tries; i++)
		{
			var p = new Vector2(
				(float)GD.RandRange(_bounds.Position.X, _bounds.End.X),
				(float)GD.RandRange(_bounds.Position.Y, _bounds.End.Y));

			if (IsWalkable(p, margin)) return p;
		}
		return _bounds.GetCenter();
	}

	// Ponto livre mais proximo de 'origin', em espiral.
	private Vector2 FindOpenSpotNear(Vector2 origin, float margin)
	{
		if (IsWalkable(origin, margin)) return origin;

		for (float r = 80f; r < 2000f; r += 80f)
		{
			for (int i = 0; i < 16; i++)
			{
				float a = Mathf.Tau * i / 16f;
				var p = origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
				if (IsWalkable(p, margin)) return p;
			}
		}
		return origin;
	}
}
