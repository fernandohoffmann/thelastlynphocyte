// MenuStyle.cs — paleta e pecas visuais compartilhadas por todas as telas.
// Mexer aqui muda o menu principal, o loadout, a arvore e as opcoes de uma vez.
using Godot;

public static class MenuStyle
{
	// ── PALETA ────────────────────────────────────────────────────────────
	// Vem da fase selecionada: fase 1 vermelha, fase 2 roxa. Como sao
	// propriedades (e nao constantes), tudo que desenha no _Draw ja acompanha
	// a troca; o que usa StyleBox precisa passar pelo Restyle().
	public static Color LINE      => GamePhase.Data.Line;
	public static Color LINE_ON   => GamePhase.Data.LineOn;
	public static Color LINE_OFF  => GamePhase.Data.LineOff;
	public static Color FILL      => GamePhase.Data.Fill;
	public static Color FILL_ON   => GamePhase.Data.FillOn;
	public static Color PANEL     => GamePhase.Data.Panel;
	public static Color CURTAIN   => new Color(PANEL.R * 0.4f, PANEL.G * 0.4f, PANEL.B * 0.4f, 0.90f);

	public static readonly Color TEXT      = new Color(0.96f, 0.86f, 0.86f);
	public static readonly Color TEXT_DIM  = new Color(0.72f, 0.55f, 0.56f);
	public static readonly Color TEXT_OFF  = new Color(0.55f, 0.40f, 0.42f);
	public static Color ACCENT    => GamePhase.Data.Accent;
	public static readonly Color DNA       = new Color(0.40f, 1.00f, 0.50f);

	public const int RADIUS = 12;

	// A fonte e de pixel numa grade de 8: qualquer outro tamanho sai borrado.
	public const int FONT_TITLE = 24;
	public const int FONT_BODY  = 16;
	public const int FONT_SMALL = 8;
	// ──────────────────────────────────────────────────────────────────────

	/// Moldura arredondada padrao das telas.
	public static StyleBoxFlat Frame(Color bg, Color border, int width = 2, int radius = RADIUS)
	{
		var s = new StyleBoxFlat { BgColor = bg, BorderColor = border };
		s.BorderWidthLeft = s.BorderWidthRight = s.BorderWidthTop = s.BorderWidthBottom = width;
		s.CornerRadiusTopLeft = s.CornerRadiusTopRight =
		s.CornerRadiusBottomLeft = s.CornerRadiusBottomRight = radius;
		return s;
	}

	public static StyleBoxFlat PanelFrame(int margin = 22)
	{
		var s = Frame(PANEL, LINE, 2, RADIUS);
		s.ContentMarginLeft = s.ContentMarginRight  = margin;
		s.ContentMarginTop  = s.ContentMarginBottom = margin - 4;
		return s;
	}

	/// Fundo comum: a arte do menu + cortina escura por cima.
	/// 'curtainAlpha' 0 = arte limpa, 1 = quase preto.
	/// Recebe Node (e nao Control) porque LoadoutScreen e ArsenalScreen sao
	/// CanvasLayer — o fundo entra como filho deles do mesmo jeito.
	public static void AddBackground(Node root, float curtainAlpha = 0.86f)
	{
		var tex = GD.Load<Texture2D>(GamePhase.Data.MenuBackground);

		if (tex != null)
		{
			var bg = new TextureRect
			{
				Texture     = tex,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			root.AddChild(bg);
		}

		var curtain = new ColorRect
		{
			Color       = new Color(CURTAIN.R, CURTAIN.G, CURTAIN.B, curtainAlpha),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		curtain.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(curtain);
	}

	/// Titulo da tela, no mesmo tom do menu.
	public static Label Title(string text)
	{
		var l = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
		l.AddThemeFontSizeOverride("font_size", FONT_TITLE);
		l.AddThemeColorOverride("font_color", new Color(0.95f, 0.30f, 0.34f));
		return l;
	}

	public static Label Body(string text, int size = FONT_BODY)
	{
		var l = new Label { Text = text };
		l.AddThemeFontSizeOverride("font_size", size);
		l.AddThemeColorOverride("font_color", TEXT);
		return l;
	}

	/// Botao no estilo das molduras: fundo escuro, borda vermelha, canto redondo.
	public static Button MakeButton(string text, float height = 52f, int fontSize = FONT_BODY)
	{
		var b = new Button { Text = text };
		b.CustomMinimumSize = new Vector2(0, height);
		b.AddThemeFontSizeOverride("font_size", fontSize);
		// as cores vem do Theme da raiz (ApplyTheme), pra troca de fase repintar
		// tudo sozinha; definir stylebox aqui bloquearia isso

		b.Pressed      += () => AudioManager.Instance?.PlayUiClick();
		b.MouseEntered += () => AudioManager.Instance?.PlayUiHover();
		return b;
	}

	/// Monta um Theme com a paleta da fase e o instala na raiz.
	/// Isso repinta qualquer Control da arvore, inclusive telas que eu nunca
	/// toquei — o Theme da raiz e herdado por todo mundo.
	/// Overrides feitos no proprio no (AddThemeStyleboxOverride) ganham do
	/// Theme, por isso o MakeButton nao define mais stylebox nenhuma.
	public static void ApplyTheme(SceneTree tree)
	{
		if (tree?.Root == null) return;

		var t = new Theme();

		var font = GD.Load<FontFile>("res://Assets/Fonts/PressStart2P-Regular.ttf");
		if (font != null) { t.DefaultFont = font; t.DefaultFontSize = FONT_SMALL; }

		t.SetStylebox("normal",   "Button", Frame(FILL,    LINE));
		t.SetStylebox("hover",    "Button", Frame(FILL_ON, LINE_ON));
		t.SetStylebox("pressed",  "Button", Frame(FILL_ON, LINE_ON, 3));
		t.SetStylebox("focus",    "Button", Frame(new Color(0, 0, 0, 0), LINE_ON));
		t.SetStylebox("disabled", "Button", Frame(new Color(0.10f, 0.05f, 0.06f, 0.6f), LINE_OFF));

		t.SetColor("font_color",          "Button", TEXT);
		t.SetColor("font_hover_color",    "Button", Colors.White);
		t.SetColor("font_pressed_color",  "Button", Colors.White);
		t.SetColor("font_disabled_color", "Button", TEXT_OFF);

		t.SetStylebox("panel", "PanelContainer", PanelFrame());
		t.SetStylebox("panel", "Panel",          PanelFrame());

		t.SetColor("font_color", "Label", TEXT);

		tree.Root.Theme = t;
	}

	/// Reaplica a paleta atual num no e em toda a sua descendencia.
	/// Necessario porque StyleBox guarda a cor de quando foi criado.
	public static void Restyle(Node node)
	{
		if (node is Button b) RestyleButton(b);
		else if (node is PanelContainer p) p.AddThemeStyleboxOverride("panel", PanelFrame());

		if (node is CanvasItem ci) ci.QueueRedraw();

		foreach (var child in node.GetChildren())
			Restyle(child);
	}

	// Remove overrides presos no no pra que o Theme da raiz volte a valer.
	private static void RestyleButton(Button b)
	{
		foreach (var name in new[] { "normal", "hover", "pressed", "focus", "disabled" })
			if (b.HasThemeStyleboxOverride(name)) b.RemoveThemeStyleboxOverride(name);

		foreach (var name in new[] { "font_color", "font_hover_color",
									 "font_pressed_color", "font_disabled_color" })
			if (b.HasThemeColorOverride(name)) b.RemoveThemeColorOverride(name);
	}

	/// Os quatro pontinhos de canto que marcam a identidade das molduras.
	/// Chamar de dentro de um _Draw.
	public static void DrawCornerDots(CanvasItem ci, Rect2 rect, Color line, bool bright)
	{
		float inset = RADIUS;

		var pts = new[]
		{
			new Vector2(rect.Position.X + inset,              rect.Position.Y + inset),
			new Vector2(rect.Position.X + rect.Size.X - inset, rect.Position.Y + inset),
			new Vector2(rect.Position.X + inset,              rect.Position.Y + rect.Size.Y - inset),
			new Vector2(rect.Position.X + rect.Size.X - inset, rect.Position.Y + rect.Size.Y - inset),
		};

		foreach (var p in pts)
		{
			ci.DrawCircle(p, 3.5f, new Color(line.R, line.G, line.B, 0.9f));
			ci.DrawCircle(p, 1.6f, new Color(1f, 0.85f, 0.85f, bright ? 1f : 0.6f));
		}
	}

	/// Medalhao circular usado nos itens (icone dentro de um anel).
	public static void DrawMedallion(CanvasItem ci, Vector2 center, float radius,
									 Color line, bool bright, Texture2D icon = null)
	{
		ci.DrawCircle(center, radius + 5f, new Color(line.R, line.G, line.B, bright ? 0.30f : 0.15f));
		ci.DrawCircle(center, radius,      new Color(0.08f, 0.02f, 0.03f, 0.95f));
		ci.DrawArc(center, radius, 0f, Mathf.Tau, 28, line, 2f);

		if (icon != null)
		{
			float s = radius * 1.45f;
			ci.DrawTextureRect(icon,
				new Rect2(center.X - s * 0.5f, center.Y - s * 0.5f, s, s), false);
		}
	}
}
