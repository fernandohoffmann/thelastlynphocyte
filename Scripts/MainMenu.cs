using Godot;
using System.Collections.Generic;

// ── Batimento cardiaco: dois pulsos ("lub-dub") e uma pausa, nao uma senoide.
// E o que faz parecer coracao em vez de respiracao.
public static class MenuBeat
{
	public const float BPM = 54f;

	public static float Value()
	{
		float t = Time.GetTicksMsec() / 1000f * (BPM / 60f);
		float p = Mathf.PosMod(t, 1f);

		float lub = Mathf.Exp(-Mathf.Pow((p - 0.08f) * 14f, 2f));
		float dub = Mathf.Exp(-Mathf.Pow((p - 0.30f) * 16f, 2f)) * 0.6f;

		return Mathf.Min(1f, lub + dub);
	}
}

// ── Um item do menu: moldura arredondada, medalhao de icone e rotulo ──
// Tudo desenhado por codigo, sem asset de botao.
public partial class MenuEntry : Control
{
	public string       Text     = "";
	public string       Note     = "";      // ex: "EM BREVE"
	public bool         Enabled  = true;
	public Texture2D    Icon     = null;
	public int          Glyph    = 0;       // desenho de fallback quando nao ha icone
	public System.Action OnPressed;

	public bool  Selected = false;
	public float Beat     = 0f;   // 0..1, sincronizado com o coracao

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	public const float HEIGHT      = 96f;  // altura de cada item
	private const float RADIUS     = 16f;   // arredondamento da moldura
	private const float ICON_R     = 32f;   // raio do medalhao
	private const float ICON_X     = 52f;   // centro do medalhao
	private const float TEXT_X     = 110f;
	private const int   FONT_SIZE  = 16;

	// a paleta vive no MenuStyle: mexer la muda todas as telas de uma vez
	private static Color LINE     => MenuStyle.LINE;
	private static Color LINE_ON  => MenuStyle.LINE_ON;
	private static Color FILL     => MenuStyle.FILL;
	private static Color FILL_ON  => MenuStyle.FILL_ON;
	private static Color TEXT_COL => MenuStyle.TEXT;
	private static Color TEXT_OFF => MenuStyle.TEXT_OFF;
	private static Color ICON_COL => MenuStyle.ACCENT;
	// ──────────────────────────────────────────────────────────────────────

	private bool _hover = false;

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(0, HEIGHT);
		MouseFilter       = MouseFilterEnum.Stop;

		MouseEntered += () =>
		{
			_hover = true;
			if (Enabled) AudioManager.Instance?.PlayUiHover();
			QueueRedraw();
		};
		MouseExited += () => { _hover = false; QueueRedraw(); };
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.Pressed &&
			mb.ButtonIndex == MouseButton.Left)
			Activate();
	}

	public void Activate()
	{
		if (!Enabled) return;
		AudioManager.Instance?.PlayUiClick();
		OnPressed?.Invoke();
	}

	public override void _Draw()
	{
		bool on = (Selected || _hover) && Enabled;

		float w = Size.X, h = Size.Y;
		var rect = new Rect2(0, 0, w, h);

		// brilho externo quando ativo
		if (on)
		{
			float a  = 0.20f + 0.28f * Beat;
			float ex = 5f + 4f * Beat;
			var glow = MakeBox(new Color(0, 0, 0, 0), new Color(LINE_ON.R, LINE_ON.G, LINE_ON.B, a), 6);
			DrawStyleBox(glow, new Rect2(-ex, -ex, w + ex * 2f, h + ex * 2f));
		}

		Color line = !Enabled ? new Color(0.45f, 0.20f, 0.22f)
							  : (on ? LINE_ON : LINE);
		Color fill = !Enabled ? new Color(0.10f, 0.05f, 0.06f, 0.6f)
							  : (on ? FILL_ON : FILL);

		DrawStyleBox(MakeBox(fill, line, 2), rect);

		// ponto decorativo em cada canto
		foreach (var p in new[] {
			new Vector2(RADIUS, RADIUS), new Vector2(w - RADIUS, RADIUS),
			new Vector2(RADIUS, h - RADIUS), new Vector2(w - RADIUS, h - RADIUS) })
		{
			DrawCircle(p, 3.5f, new Color(line.R, line.G, line.B, 0.9f));
			DrawCircle(p, 1.6f, new Color(1f, 0.85f, 0.85f, on ? 1f : 0.6f));
		}

		DrawMedallion(new Vector2(ICON_X, h * 0.5f), line, on);

		var font = ThemeDB.FallbackFont;
		DrawString(font, new Vector2(TEXT_X, h * 0.5f + FONT_SIZE * 0.4f),
			Text, HorizontalAlignment.Left, (int)(w - TEXT_X - 16f), FONT_SIZE,
			Enabled ? TEXT_COL : TEXT_OFF);

		if (Note != "")
			DrawString(font, new Vector2(TEXT_X, h * 0.5f + FONT_SIZE * 0.4f),
				Note, HorizontalAlignment.Right, (int)(w - TEXT_X - 16f), 8,
				new Color(0.7f, 0.5f, 0.3f));
	}

	private void DrawMedallion(Vector2 c, Color line, bool on)
	{
		DrawCircle(c, ICON_R + 5f, new Color(line.R, line.G, line.B, on ? 0.30f : 0.15f));
		DrawCircle(c, ICON_R,      new Color(0.08f, 0.02f, 0.03f, 0.95f));
		DrawArc(c, ICON_R, 0f, Mathf.Tau, 28, line, 2f);

		if (Icon != null)
		{
			float s = ICON_R * 1.45f;
			DrawTextureRect(Icon, new Rect2(c.X - s * 0.5f, c.Y - s * 0.5f, s, s), false,
				Enabled ? Colors.White : new Color(1, 1, 1, 0.45f));
			return;
		}

		Color g = Enabled ? ICON_COL : new Color(ICON_COL.R, ICON_COL.G, ICON_COL.B, 0.4f);
		DrawGlyph(c, g);
	}

	// Desenhos simples pra cada item, usados enquanto nao houver textura.
	private void DrawGlyph(Vector2 c, Color col)
	{
		// os desenhos abaixo foram feitos pra um medalhao de raio 26;
		// o fator reescala tudo junto quando o ICON_R muda
		float k = ICON_R / 26f;
		DrawGlyphScaled(c, col, k);
	}

	private void DrawGlyphScaled(Vector2 c, Color col, float k)
	{
		switch (Glyph)
		{
			case 0: // virus: circulo com espinhos
				DrawArc(c, 9f * k, 0f, Mathf.Tau, 20, col, 2f);
				for (int i = 0; i < 8; i++)
				{
					float a = Mathf.Tau * i / 8f;
					var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
					DrawLine(c + d * 9f * k, c + d * 14f * k, col, 2f * k);
					DrawCircle(c + d * 15f * k, 2f * k, col);
				}
				break;

			case 1: // arvore de habilidades: no central com ramos
				DrawCircle(c + new Vector2(0, -10f) * k, 3.5f * k, col);
				DrawCircle(c + new Vector2(-11f, 6f) * k, 3.5f * k, col);
				DrawCircle(c + new Vector2(11f, 6f) * k, 3.5f * k, col);
				DrawCircle(c, 4.5f * k, col);
				DrawLine(c, c + new Vector2(0, -10f) * k, col, 2f * k);
				DrawLine(c, c + new Vector2(-11f, 6f) * k, col, 2f * k);
				DrawLine(c, c + new Vector2(11f, 6f) * k, col, 2f * k);
				break;

			case 2: // microscopio: corpo inclinado sobre uma base
				DrawLine(c + new Vector2(-2f, -12f) * k, c + new Vector2(6f, 2f) * k, col, 3f * k);
				DrawLine(c + new Vector2(6f, 2f) * k, c + new Vector2(-4f, 8f) * k, col, 3f * k);
				DrawLine(c + new Vector2(-12f, 12f) * k, c + new Vector2(12f, 12f) * k, col, 3f * k);
				DrawCircle(c + new Vector2(-2f, -12f) * k, 3f * k, col);
				break;

			case 3: // engrenagem
				DrawArc(c, 8f * k, 0f, Mathf.Tau, 24, col, 2.5f * k);
				for (int i = 0; i < 6; i++)
				{
					float a = Mathf.Tau * i / 6f;
					var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
					DrawLine(c + d * 8f * k, c + d * 13f * k, col, 3f * k);
				}
				break;

			default: // sair: porta com seta
				DrawRect(new Rect2(c.X - 10f * k, c.Y - 11f * k, 12f * k, 22f * k), col, false, 2f * k);
				DrawLine(c + new Vector2(0f, 0f), c + new Vector2(12f, 0f) * k, col, 2.5f * k);
				DrawLine(c + new Vector2(12f, 0f) * k, c + new Vector2(7f, -5f) * k, col, 2.5f * k);
				DrawLine(c + new Vector2(12f, 0f) * k, c + new Vector2(7f, 5f) * k, col, 2.5f * k);
				break;
		}
	}

	private static StyleBoxFlat MakeBox(Color bg, Color border, int width)
	{
		var s = new StyleBoxFlat { BgColor = bg, BorderColor = border };
		s.BorderWidthLeft = s.BorderWidthRight = s.BorderWidthTop = s.BorderWidthBottom = width;
		s.CornerRadiusTopLeft = s.CornerRadiusTopRight =
		s.CornerRadiusBottomLeft = s.CornerRadiusBottomRight = (int)RADIUS;
		return s;
	}
}

public partial class MainMenu : Control
{
	private LoadoutScreen   _loadoutScreen;
	private ArsenalScreen   _arsenalScreen;
	private PanelContainer  _settingsPanel;
	private IntroScreen     _introScreen;
	private ConfirmDialog   _confirmDialog;
	private MicroscopeScreen _microscope;

	private readonly List<MenuEntry> _entries = new List<MenuEntry>();
	private int _selected = 0;

	private TextureRect    _bg;
	private ShaderMaterial _bgMat;
	private Texture2D      _bgTex;

	private TextureRect _logo;

	// Nada fica destacado ate o jogador usar as setas. Sem isso o primeiro item
	// nasce aceso e parece que ja esta selecionado.
	private bool _navActive = false;

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	private const float PANEL_W   = 860f;  // largura da coluna escura da esquerda
	private const float BTN_W     = 700f;
	private const float BTN_LEFT  = 70f;
	private const float BTN_TOP   = 330f;
	private const float BTN_GAP   = 24f;

	// ── PULSACAO DO CORACAO ──
	// Posicao do coracao DENTRO da textura de fundo, em UV (0..1).
	// Ligue HEART_MARKER pra ver um circulo verde em cima e acertar os valores.
	private static readonly Vector2 HEART_UV = new Vector2(0.690f, 0.380f);
	private const float HEART_RADIUS = 0.20f;  // alcance do efeito, em UV
	private const float HEART_BULGE  = 0.030f; // o quanto incha no pico
	private const float HEART_GLOW   = 0.20f;  // brilho extra no pico
	private const bool  HEART_MARKER = false;  // true = mostra onde o efeito pega

	// ── LOGO ──
	private const float LOGO_TOP = 24f;
	private const float LOGO_H   = 280f;   // altura da caixa; a logo cabe dentro
	// ──────────────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		SaveData.Load();

		// paleta da fase aplicada na raiz: vale pra todas as telas
		MenuStyle.ApplyTheme(GetTree());

		SetAnchorsPreset(LayoutPreset.FullRect);

		BuildBackground();
		BuildLogo();
		BuildButtons();
		BuildFooter();
		BuildSettingsPanel();

		CallDeferred(MethodName.InitScreens);
	}

	// ══════════════════════════════════════════════════════════════════════
	private void BuildBackground()
	{
		_bgTex = GD.Load<Texture2D>(GamePhase.Data.MenuBackground);

		_bg = new TextureRect();
		_bg.Texture = _bgTex;
		// Scale (e nao KeepAspectCovered) porque assim o UV do shader mapeia
		// exatamente 0..1 na textura inteira — o recorte e feito pelo tamanho
		// do proprio retangulo, calculado no LayoutBackground.
		_bg.StretchMode = TextureRect.StretchModeEnum.Scale;
		_bg.SetAnchorsPreset(LayoutPreset.TopLeft);
		_bg.MouseFilter = MouseFilterEnum.Ignore;

		_bgMat = new ShaderMaterial { Shader = BuildHeartShader() };
		_bgMat.SetShaderParameter("heart_uv",     HEART_UV);
		_bgMat.SetShaderParameter("heart_radius", HEART_RADIUS);
		_bgMat.SetShaderParameter("bulge",        HEART_BULGE);
		_bgMat.SetShaderParameter("glow",         HEART_GLOW);
		_bgMat.SetShaderParameter("marker",       HEART_MARKER ? 1f : 0f);
		// o coracao so existe na arte da fase 1
		_bgMat.SetShaderParameter("glow",  GamePhase.Current == 1 ? HEART_GLOW  : 0f);
		_bgMat.SetShaderParameter("bulge", GamePhase.Current == 1 ? HEART_BULGE : 0f);
		_bgMat.SetShaderParameter("beat",         0f);
		_bg.Material = _bgMat;

		AddChild(_bg);

		LayoutBackground();
		GetViewport().SizeChanged += LayoutBackground;
		GamePhase.Changed += OnPhaseChanged;

		// Cortina escura na esquerda pra coluna de botoes ter contraste.
		// Gradiente horizontal: opaco na borda, transparente no meio da tela.
		var grad = new Gradient();
		grad.SetOffset(0, 0f);  grad.SetColor(0, new Color(0.03f, 0.00f, 0.02f, 0.94f));
		grad.AddPoint(0.62f,        new Color(0.03f, 0.00f, 0.02f, 0.80f));
		grad.SetOffset(1, 1f);  grad.SetColor(1, new Color(0.03f, 0.00f, 0.02f, 0.00f));

		var curtain = new TextureRect
		{
			Texture = new GradientTexture2D
			{
				Gradient = grad,
				Width    = 256,
				Height   = 8,
				Fill     = GradientTexture2D.FillEnum.Linear,
				FillFrom = new Vector2(0f, 0.5f),
				FillTo   = new Vector2(1f, 0.5f)
			},
			StretchMode = TextureRect.StretchModeEnum.Scale,
			MouseFilter = MouseFilterEnum.Ignore
		};
		curtain.SetAnchorsPreset(LayoutPreset.LeftWide);
		curtain.OffsetLeft = 0f; curtain.OffsetRight = PANEL_W;
		AddChild(curtain);
	}

	// Cobre a tela mantendo a proporcao: o retangulo fica maior que a janela e
	// centralizado, entao o excesso simplesmente cai pra fora da tela.
	private void LayoutBackground()
	{
		if (_bg == null || _bgTex == null) return;

		Vector2 vp = GetViewportRect().Size;
		Vector2 ts = _bgTex.GetSize();
		if (ts.X <= 0f || ts.Y <= 0f) return;

		float   scale = Mathf.Max(vp.X / ts.X, vp.Y / ts.Y);
		Vector2 size  = ts * scale;

		_bg.Size     = size;
		_bg.Position = (vp - size) * 0.5f;

		_bgMat?.SetShaderParameter("aspect", size.X / size.Y);
	}

	private static Shader BuildHeartShader()
	{
		var shader = new Shader();
		shader.Code = @"
shader_type canvas_item;

uniform vec2  heart_uv     = vec2(0.69, 0.38);
uniform float heart_radius = 0.20;
uniform float bulge        = 0.030;
uniform float glow         = 0.20;
uniform float aspect       = 1.78;
uniform float beat         = 0.0;   // 0..1, vem do C#
uniform float marker       = 0.0;
uniform vec3  glow_color : source_color = vec3(1.0, 0.32, 0.30);

void fragment() {
    vec2 d = UV - heart_uv;
    d.x *= aspect;                  // sem isso o alcance sai como elipse

    float r = length(d) / heart_radius;

    // queda suave: forte no centro, zero na borda do alcance
    float falloff = 1.0 - smoothstep(0.0, 1.0, r);
    falloff *= falloff;

    vec2 dir = normalize(d + vec2(0.00001));
    dir.x /= aspect;

    // amostrar mais PERTO do centro amplia a imagem ali: e o inchaco
    vec2 uv = UV - dir * bulge * heart_radius * falloff * beat;

    vec4 col = texture(TEXTURE, uv);
    col.rgb += glow_color * glow * falloff * beat;

    if (marker > 0.5)
        col.rgb = mix(col.rgb, vec3(0.1, 1.0, 0.2), falloff * 0.55);

    COLOR = col;
}
";
		return shader;
	}

	// Troca de fase no loadout: repinta o menu inteiro sem reconstruir nada.
	private void OnPhaseChanged()
	{
		var data = GamePhase.Data;

		_bgTex = GD.Load<Texture2D>(data.MenuBackground);
		if (_bg != null) { _bg.Texture = _bgTex; LayoutBackground(); }

		_bgMat?.SetShaderParameter("glow",  GamePhase.Current == 1 ? HEART_GLOW  : 0f);
		_bgMat?.SetShaderParameter("bulge", GamePhase.Current == 1 ? HEART_BULGE : 0f);

		MenuStyle.ApplyTheme(GetTree());   // repinta tudo que herda o Theme
		MenuStyle.Restyle(this);           // solta overrides presos nos nos
	}

	private void BuildLogo()
	{
		var logoTex = GD.Load<Texture2D>("res://Assets/logo.png");

		if (logoTex != null)
		{
			_logo = MakeLogoRect(logoTex);
			AddChild(_logo);
			return;
		}

		// sem logo.png: monta o titulo com texto mesmo
		GD.Print("[MainMenu] logo.png nao encontrado — usando titulo em texto");

		var box = new VBoxContainer();
		box.SetAnchorsPreset(LayoutPreset.TopLeft);
		box.OffsetLeft = BTN_LEFT; box.OffsetTop = 60f;
		box.OffsetRight = BTN_LEFT + BTN_W; box.OffsetBottom = 260f;
		box.AddThemeConstantOverride("separation", 6);
		box.MouseFilter = MouseFilterEnum.Ignore;
		AddChild(box);

		var l1 = new Label { Text = "THE LAST" };
		l1.AddThemeFontSizeOverride("font_size", 32);
		l1.AddThemeColorOverride("font_color", new Color(0.92f, 0.20f, 0.24f));
		box.AddChild(l1);

		var l2 = new Label { Text = "LYMPHOCYTE" };
		l2.AddThemeFontSizeOverride("font_size", 48);
		l2.AddThemeColorOverride("font_color", new Color(0.78f, 0.10f, 0.16f));
		box.AddChild(l2);
	}

	private TextureRect MakeLogoRect(Texture2D tex)
	{
		var r = new TextureRect
		{
			Texture     = tex,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			ExpandMode  = TextureRect.ExpandModeEnum.IgnoreSize,
			MouseFilter = MouseFilterEnum.Ignore
		};
		// mesma largura e mesma margem esquerda dos botoes: a coluna fica alinhada
		r.SetAnchorsPreset(LayoutPreset.TopLeft);
		r.OffsetLeft   = BTN_LEFT;
		r.OffsetTop    = LOGO_TOP;
		r.OffsetRight  = BTN_LEFT + BTN_W;
		r.OffsetBottom = LOGO_TOP + LOGO_H;
		return r;
	}

	public override void _Process(double delta)
	{
		float k = MenuBeat.Value();

		_bgMat?.SetShaderParameter("beat", k);

		// so pulsa o item se a navegacao por teclado ja estiver em uso
		if (_navActive && _entries.Count > 0)
		{
			var sel = _entries[_selected];
			sel.Beat = k;
			sel.QueueRedraw();
		}
	}

	private void BuildButtons()
	{
		var col = new VBoxContainer();
		col.SetAnchorsPreset(LayoutPreset.TopLeft);
		col.OffsetLeft   = BTN_LEFT;
		col.OffsetTop    = BTN_TOP;
		col.OffsetRight  = BTN_LEFT + BTN_W;
		col.OffsetBottom = BTN_TOP + 6 * (MenuEntry.HEIGHT + BTN_GAP);
		col.AddThemeConstantOverride("separation", (int)BTN_GAP);
		AddChild(col);

		AddEntry(col, "COMECAR JOGO",         0, true,  () => _loadoutScreen?.Show());
		AddEntry(col, "ARVORE DE HABILIDADES", 1, true,  () => _arsenalScreen?.Show());
		AddEntry(col, "MICROSCOPIO",           2, true,  () => _microscope?.Show());
		AddEntry(col, "OPCOES",                3, true,  () => ToggleSettings());
		AddEntry(col, "SAIR",                  4, true,  () => GetTree().Quit());

		SelectIndex(0);
	}

	private void AddEntry(VBoxContainer parent, string text, int glyph, bool enabled,
						  System.Action action, string note = "")
	{
		var e = new MenuEntry
		{
			Text      = text,
			Glyph     = glyph,
			Enabled   = enabled,
			Note      = note,
			OnPressed = action
		};
		parent.AddChild(e);
		_entries.Add(e);
	}

	private void BuildFooter()
	{
		var dna = new Label { Text = $"DNA: {SaveData.TotalDna}" };
		dna.SetAnchorsPreset(LayoutPreset.BottomLeft);
		dna.OffsetLeft = BTN_LEFT; dna.OffsetTop    = -46f;
		dna.OffsetRight = BTN_LEFT + 300f; dna.OffsetBottom = -22f;
		dna.AddThemeFontSizeOverride("font_size", 8);
		dna.AddThemeColorOverride("font_color", new Color(0.4f, 1f, 0.5f, 0.85f));
		dna.MouseFilter = MouseFilterEnum.Ignore;
		dna.Name = "DnaLabel";
		AddChild(dna);

		var version = new Label { Text = "v0.8 Early Access" };
		version.SetAnchorsPreset(LayoutPreset.BottomRight);
		version.OffsetLeft = -240f; version.OffsetTop    = -28f;
		version.OffsetRight = -12f; version.OffsetBottom = -6f;
		version.HorizontalAlignment = HorizontalAlignment.Right;
		version.AddThemeFontSizeOverride("font_size", 8);
		version.AddThemeColorOverride("font_color", new Color(0.6f, 0.45f, 0.45f, 0.7f));
		version.MouseFilter = MouseFilterEnum.Ignore;
		AddChild(version);
	}

	// ══════════════════════════════════════════════════════════════════════
	private void SelectIndex(int index)
	{
		if (_entries.Count == 0) return;

		_selected = Mathf.PosMod(index, _entries.Count);

		for (int i = 0; i < _entries.Count; i++)
		{
			_entries[i].Selected = _navActive && (i == _selected);
			_entries[i].QueueRedraw();
		}
	}

	private void MoveSelection(int step)
	{
		// a primeira seta so acende o item atual, sem pular
		if (!_navActive)
		{
			_navActive = true;
			SelectIndex(_selected);
			AudioManager.Instance?.PlayUiHover();
			return;
		}

		int i = _selected;
		for (int guard = 0; guard < _entries.Count; guard++)
		{
			i = Mathf.PosMod(i + step, _entries.Count);
			if (_entries[i].Enabled) break;   // pula o que esta desabilitado
		}
		SelectIndex(i);
		AudioManager.Instance?.PlayUiHover();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_settingsPanel != null && _settingsPanel.Visible)
		{
			if (@event.IsActionPressed("ui_cancel")) ToggleSettings();
			return;
		}

		if (@event.IsActionPressed("ui_down"))   MoveSelection(1);
		else if (@event.IsActionPressed("ui_up")) MoveSelection(-1);
		else if (@event.IsActionPressed("ui_accept"))
			_entries[_selected].Activate();
	}

	private void ToggleSettings()
	{
		_settingsPanel.Visible = !_settingsPanel.Visible;
	}

	// ══════════════════════════════════════════════════════════════════════
	private void BuildSettingsPanel()
	{
		_settingsPanel         = new PanelContainer();
		_settingsPanel.Visible = false;
		_settingsPanel.SetAnchorsPreset(LayoutPreset.Center);

		var settingsStyle = MenuStyle.PanelFrame(24);
		_settingsPanel.AddThemeStyleboxOverride("panel", settingsStyle);
		AddChild(_settingsPanel);

		var settingsVbox = new VBoxContainer();
		settingsVbox.CustomMinimumSize = new Vector2(420, 0);
		settingsVbox.AddThemeConstantOverride("separation", 14);
		_settingsPanel.AddChild(settingsVbox);

		settingsVbox.AddChild(MenuStyle.Title("OPCOES"));
		settingsVbox.AddChild(new HSeparator());

		// Volume música
		var volRow = new HBoxContainer();
		volRow.AddThemeConstantOverride("separation", 10);
		settingsVbox.AddChild(volRow);
		volRow.AddChild(MenuStyle.Body("MUSICA", MenuStyle.FONT_SMALL));
		var volSlider = new HSlider { MinValue = 0, MaxValue = 100, Value = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume * 100 : 80, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		volSlider.ValueChanged += (double v) => AudioManager.Instance?.SetMusicVolume((float)(v / 100.0));
		volRow.AddChild(volSlider);

		// Volume SFX
		var sfxRow = new HBoxContainer();
		sfxRow.AddThemeConstantOverride("separation", 10);
		settingsVbox.AddChild(sfxRow);
		sfxRow.AddChild(MenuStyle.Body("EFEITOS", MenuStyle.FONT_SMALL));
		var sfxSlider = new HSlider { MinValue = 0, MaxValue = 100, Value = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume * 100 : 100, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		sfxSlider.ValueChanged += (double v) => AudioManager.Instance?.SetSfxVolume((float)(v / 100.0));
		sfxRow.AddChild(sfxSlider);

		settingsVbox.AddChild(new HSeparator());

		// Resolucao
		var resRow = new HBoxContainer();
		resRow.AddThemeConstantOverride("separation", 10);
		settingsVbox.AddChild(resRow);
		resRow.AddChild(MenuStyle.Body("RESOLUCAO", MenuStyle.FONT_SMALL));

		var resOption = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		for (int i = 0; i < DisplaySettings.RESOLUTIONS.Length; i++)
			resOption.AddItem(DisplaySettings.Label(i), i);
		resOption.Selected  = DisplaySettings.CurrentIndex();
		resOption.Disabled  = SaveData.Fullscreen; // em tela cheia nao tem efeito
		resOption.ItemSelected += (long idx) => DisplaySettings.SetResolution((int)idx);
		resRow.AddChild(resOption);

		// Tela cheia
		var fullscreenRow = new HBoxContainer();
		fullscreenRow.AddThemeConstantOverride("separation", 10);
		settingsVbox.AddChild(fullscreenRow);
		fullscreenRow.AddChild(MenuStyle.Body("TELA CHEIA", MenuStyle.FONT_SMALL));
		var fullscreenCheck = new CheckBox { ButtonPressed = SaveData.Fullscreen };
		fullscreenCheck.Toggled += (bool on) =>
		{
			DisplaySettings.SetFullscreen(on);
			resOption.Disabled = on;
		};
		fullscreenRow.AddChild(fullscreenCheck);

		// ── RESETAR PROGRESSO ─────────────────────────────────────
		var resetBtn = MenuStyle.MakeButton("RESETAR PROGRESSO", 46f, MenuStyle.FONT_SMALL);
		// vermelho mais forte: e a unica acao destrutiva da tela
		resetBtn.AddThemeStyleboxOverride("normal",
			MenuStyle.Frame(new Color(0.42f, 0.04f, 0.06f, 0.85f), MenuStyle.LINE_ON));
		resetBtn.Pressed += () =>
		{
			_confirmDialog?.Ask(
				"Isso vai apagar TODO o seu progresso:\nDNA, habilidades desbloqueadas e saves.\n\nEssa acao nao pode ser desfeita!",
				() =>
				{
					SaveData.Reset();
					var dna = GetNodeOrNull<Label>("DnaLabel");
					if (dna != null) dna.Text = $"DNA: {SaveData.TotalDna}";
				}
			);
		};
		settingsVbox.AddChild(resetBtn);

		settingsVbox.AddChild(new HSeparator());

		var closeBtn = MenuStyle.MakeButton("FECHAR", 46f);
		closeBtn.Pressed += () => _settingsPanel.Visible = false;
		settingsVbox.AddChild(closeBtn);
	}

	// ══════════════════════════════════════════════════════════════════════
	private void InitScreens()
	{
		if (AudioManager.Instance == null)
		{
			var am = new AudioManager { Name = "AudioManager" };
			GetTree().Root.AddChild(am);
		}
		if (UpgradeManager.Instance == null)
		{
			var um = new UpgradeManager { Name = "UpgradeManager" };
			GetTree().Root.AddChild(um);
		}
		if (GameManager.Instance == null)
		{
			var gm = new GameManager { Name = "GameManager" };
			GetTree().Root.AddChild(gm);
		}

		_introScreen = new IntroScreen { Name = "IntroScreen" };
		GetTree().Root.AddChild(_introScreen);

		_confirmDialog = new ConfirmDialog { Name = "ConfirmDialog" };
		GetTree().Root.AddChild(_confirmDialog);

		_loadoutScreen = new LoadoutScreen { Name = "LoadoutScreen" };
		GetTree().Root.AddChild(_loadoutScreen);

		_arsenalScreen = new ArsenalScreen { Name = "ArsenalScreen" };
		GetTree().Root.AddChild(_arsenalScreen);

		_microscope = new MicroscopeScreen { Name = "MicroscopeScreen" };
		GetTree().Root.AddChild(_microscope);

		// Expõe IntroScreen e ConfirmDialog para LoadoutScreen
		LoadoutScreen.IntroScreenRef   = _introScreen;
		LoadoutScreen.ConfirmDialogRef = _confirmDialog;
	}

	public override void _ExitTree()
	{
		GamePhase.Changed -= OnPhaseChanged;

		if (IsInstanceValid(_loadoutScreen)) _loadoutScreen.QueueFree();
		if (IsInstanceValid(_arsenalScreen)) _arsenalScreen.QueueFree();
		if (IsInstanceValid(_microscope))    _microscope.QueueFree();
		if (IsInstanceValid(_introScreen))   _introScreen.QueueFree();
		if (IsInstanceValid(_confirmDialog)) _confirmDialog.QueueFree();
	}
}
