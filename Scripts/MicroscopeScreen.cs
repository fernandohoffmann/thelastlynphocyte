// MicroscopeScreen.cs — o Microscopio / Codex.
//
// Tres niveis de navegacao:
//   1. CORPO      — o corpo humano desenhado por codigo, com as regioes
//   2. CATEGORIAS — as seis categorias da regiao, com progresso
//   3. ENTRADA    — a ficha cientifica em si
//
// So a Fase 1 (sistema circulatorio) existe. As outras regioes aparecem
// trancadas, pra mostrar pra onde a campanha vai.
using Godot;
using System.Collections.Generic;

// ── Corpo humano: a arte de corpo.png com setas apontando as regioes ──
public partial class BodyMap : Control
{
	public System.Action OnRegion;   // recebe o clique da regiao liberada

	private class Marker
	{
		public string  Label;
		public Vector2 Uv;        // ponto anatomico, 0..1 dentro da IMAGEM
		public bool    Left;      // de que lado o rotulo fica
		public bool    Unlocked;
	}

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	// Ligue DEBUG_UV pra ver as coordenadas na tela e acertar os pontos.
	private const bool  DEBUG_UV   = false;
	private const float LABEL_GAP  = 26f;   // distancia do rotulo ate a imagem
	private const float DOT_R      = 7f;
	// ──────────────────────────────────────────────────────────────────────

	private readonly Marker[] _markers =
	{
		new Marker { Label = "CEREBRO",           Uv = new Vector2(0.525f, 0.075f), Left = false, Unlocked = false },
		new Marker { Label = "PULMOES",           Uv = new Vector2(0.430f, 0.280f), Left = true,  Unlocked = false },
		new Marker { Label = "FIGADO",            Uv = new Vector2(0.430f, 0.355f), Left = true,  Unlocked = false },
		new Marker { Label = "INTESTINO DELGADO", Uv = new Vector2(0.500f, 0.455f), Left = true,  Unlocked = false },
		new Marker { Label = "VEIAS",             Uv = new Vector2(0.660f, 0.620f), Left = false, Unlocked = true  },
	};

	private Texture2D _tex;
	private float     _pulse = 0f;
	private Marker    _hover = null;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Stop;
		_tex = GD.Load<Texture2D>("res://Assets/corpo.png");

		if (_tex == null)
			GD.PrintErr("[BodyMap] res://Assets/corpo.png nao encontrado");

		MouseExited += () => { _hover = null; QueueRedraw(); };
	}

	public override void _Process(double delta)
	{
		_pulse += (float)delta;
		QueueRedraw();
	}

	// A imagem e alta e estreita: cabe inteira no controle, centralizada,
	// deixando margem nos dois lados pros rotulos.
	private Rect2 ImageRect()
	{
		if (_tex == null) return new Rect2(Vector2.Zero, Size);

		Vector2 ts = _tex.GetSize();
		float   k  = Mathf.Min(Size.X / ts.X, Size.Y / ts.Y);
		Vector2 sz = ts * k;

		return new Rect2((Size - sz) * 0.5f, sz);
	}

	private Vector2 PointOf(Marker m)
	{
		var r = ImageRect();
		return r.Position + m.Uv * r.Size;
	}

	// area clicavel: um retangulo em volta do rotulo
	private Rect2 LabelRect(Marker m)
	{
		var   r = ImageRect();
		float w = 230f, h = 30f;
		float y = PointOf(m).Y - h * 0.5f;
		float x = m.Left ? r.Position.X - LABEL_GAP - w
						 : r.End.X + LABEL_GAP;
		return new Rect2(x, y, w, h);
	}

	public override void _GuiInput(InputEvent e)
	{
		if (e is InputEventMouseMotion mm)
		{
			Marker found = null;
			foreach (var m in _markers)
				if (LabelRect(m).HasPoint(mm.Position) ||
					PointOf(m).DistanceTo(mm.Position) < 18f) { found = m; break; }

			if (found != _hover) { _hover = found; QueueRedraw(); }

			if (DEBUG_UV)
			{
				var r = ImageRect();
				GD.Print($"[BodyMap] uv = {((mm.Position - r.Position) / r.Size)}");
			}
		}
		else if (e is InputEventMouseButton mb && mb.Pressed &&
				 mb.ButtonIndex == MouseButton.Left &&
				 _hover != null && _hover.Unlocked)
		{
			AudioManager.Instance?.PlayUiClick();
			OnRegion?.Invoke();
		}
	}

	public override void _Draw()
	{
		var rect = ImageRect();

		if (_tex != null)
			DrawTextureRect(_tex, rect, false);

		var   font = ThemeDB.FallbackFont;
		float beat = 0.5f + 0.5f * Mathf.Sin(_pulse * 2.4f);

		foreach (var m in _markers)
		{
			bool on   = m.Unlocked;
			bool over = (_hover == m) && on;

			Color col = !on  ? MenuStyle.LINE_OFF
					  : over ? Colors.White
							 : MenuStyle.LINE_ON;

			Vector2 p   = PointOf(m);
			Rect2   lr  = LabelRect(m);

			// ponta da seta encostando no rotulo
			Vector2 tail = new Vector2(m.Left ? lr.End.X : lr.Position.X, lr.Position.Y + lr.Size.Y * 0.5f);
			// cotovelo: sai reto do rotulo e depois diagonal ate o alvo
			Vector2 elbow = new Vector2(tail.X + (m.Left ? 26f : -26f), tail.Y);

			DrawLine(tail, elbow, col, 2f);
			DrawLine(elbow, p, col, 2f);
			DrawArrowHead(p, (p - elbow).Normalized(), col);

			// alvo
			float r = DOT_R + (on ? 3f * beat : 0f);
			DrawCircle(p, r + 4f, new Color(col.R, col.G, col.B, on ? 0.30f * beat : 0.12f));
			DrawCircle(p, r, new Color(0.06f, 0.02f, 0.03f, 0.9f));
			DrawArc(p, r, 0f, Mathf.Tau, 20, col, 2f);

			// rotulo
			DrawString(font,
				new Vector2(lr.Position.X, lr.Position.Y + 20f),
				m.Label,
				m.Left ? HorizontalAlignment.Right : HorizontalAlignment.Left,
				(int)lr.Size.X, MenuStyle.FONT_SMALL,
				on ? (over ? Colors.White : MenuStyle.TEXT) : MenuStyle.TEXT_OFF);

			if (!on) DrawLock(new Vector2(m.Left ? lr.Position.X - 16f : lr.End.X + 16f,
										  lr.Position.Y + lr.Size.Y * 0.5f));
		}

		if (DEBUG_UV)
			DrawRect(rect, new Color(0f, 1f, 0f, 0.15f));
	}

	private void DrawArrowHead(Vector2 tip, Vector2 dir, Color col)
	{
		if (dir.LengthSquared() < 0.001f) return;

		Vector2 back = tip - dir * 14f;
		Vector2 side = new Vector2(-dir.Y, dir.X) * 6f;

		DrawColoredPolygon(new[] { tip, back + side, back - side }, col);
	}

	private void DrawLock(Vector2 c)
	{
		DrawRect(new Rect2(c.X - 5f, c.Y - 1f, 10f, 8f), MenuStyle.TEXT_OFF);
		DrawArc(new Vector2(c.X, c.Y - 1f), 4f, Mathf.Pi, Mathf.Tau, 12, MenuStyle.TEXT_OFF, 2f);
	}
}

public partial class MicroscopeScreen : CanvasLayer
{
	private enum Level { Body, Categories, Entry }

	private Control _bodyPage;
	private Control _catPage;
	private Control _entryPage;

	private Label _entryName, _entryMeta, _entryText;
	private VBoxContainer _catList;

	private Level        _level = Level.Body;
	private CodexEntry   _current;

	public override void _Ready()
	{
		Layer   = 9;
		Visible = false;

		MenuStyle.AddBackground(this, 0.92f);

		BuildBodyPage();
		BuildCategoriesPage();
		BuildEntryPage();

		GoTo(Level.Body);
	}

	public new void Show()
	{
		Visible = true;
		GoTo(Level.Body);
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (!Visible) return;

		if (e.IsActionPressed("ui_cancel"))
		{
			GetViewport().SetInputAsHandled();

			if (_level == Level.Entry)           GoTo(Level.Categories);
			else if (_level == Level.Categories) GoTo(Level.Body);
			else                                 Visible = false;
		}
	}

	private void GoTo(Level level)
	{
		_level = level;
		_bodyPage.Visible  = level == Level.Body;
		_catPage.Visible   = level == Level.Categories;
		_entryPage.Visible = level == Level.Entry;

		if (level == Level.Categories) RefreshCategories();
	}

	// ══════════════════════════════════════════════════════════════════════
	private Control MakePage(string title, System.Action onBack)
	{
		var page = new Control();
		page.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(page);

		var root = new VBoxContainer();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.OffsetLeft = 60; root.OffsetTop = 40;
		root.OffsetRight = -60; root.OffsetBottom = -40;
		root.AddThemeConstantOverride("separation", 18);
		page.AddChild(root);

		var top = new HBoxContainer();
		root.AddChild(top);

		var back = MenuStyle.MakeButton("<- VOLTAR", 46f);
		back.CustomMinimumSize = new Vector2(190, 46);
		back.Pressed += onBack;
		top.AddChild(back);

		var lbl = MenuStyle.Title(title);
		lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		top.AddChild(lbl);

		top.AddChild(new Control { CustomMinimumSize = new Vector2(190, 0) });

		root.AddChild(new HSeparator());
		return page;
	}

	private void BuildBodyPage()
	{
		_bodyPage = MakePage("MICROSCOPIO", () => Visible = false);
		var root  = _bodyPage.GetChild<VBoxContainer>(0);

		var sub = MenuStyle.Body(
			"Regioes exploradas ficam ativas. Clique em VEIAS para abrir o registro.",
			MenuStyle.FONT_SMALL);
		sub.HorizontalAlignment = HorizontalAlignment.Center;
		sub.AddThemeColorOverride("font_color", MenuStyle.TEXT_DIM);
		root.AddChild(sub);

		var center = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		center.Alignment = BoxContainer.AlignmentMode.Center;
		root.AddChild(center);

		var body = new BodyMap
		{
			// largura extra dos dois lados pros rotulos das setas caberem
			CustomMinimumSize = new Vector2(1100, 780),
			OnRegion          = () => GoTo(Level.Categories)
		};
		center.AddChild(body);
	}

	private void BuildCategoriesPage()
	{
		_catPage = MakePage("SISTEMA CIRCULATORIO", () => GoTo(Level.Body));
		var root = _catPage.GetChild<VBoxContainer>(0);

		var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		root.AddChild(scroll);

		_catList = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_catList.AddThemeConstantOverride("separation", 14);
		scroll.AddChild(_catList);
	}

	private void RefreshCategories()
	{
		foreach (var child in _catList.GetChildren()) child.QueueFree();

		Codex.ProgressTotal(out int totalKnown, out int total);

		var head = MenuStyle.Body($"DESCOBERTAS  {totalKnown}/{total}", MenuStyle.FONT_BODY);
		head.AddThemeColorOverride("font_color", MenuStyle.DNA);
		_catList.AddChild(head);

		foreach (CodexCategory cat in System.Enum.GetValues(typeof(CodexCategory)))
		{
			Codex.Progress(cat, out int known, out int count);
			if (count == 0) continue;

			var section = new PanelContainer();
			section.AddThemeStyleboxOverride("panel", MenuStyle.PanelFrame(16));
			_catList.AddChild(section);

			var box = new VBoxContainer();
			box.AddThemeConstantOverride("separation", 10);
			section.AddChild(box);

			var title = MenuStyle.Body($"{cat.ToString().ToUpper()}   {known}/{count}", MenuStyle.FONT_BODY);
			title.AddThemeColorOverride("font_color", new Color(1f, 0.45f, 0.45f));
			box.AddChild(title);

			var grid = new GridContainer { Columns = 3 };
			grid.AddThemeConstantOverride("h_separation", 12);
			grid.AddThemeConstantOverride("v_separation", 12);
			box.AddChild(grid);

			foreach (var entry in Codex.ByCategory(cat))
			{
				bool known1 = Codex.IsKnown(entry.Id);

				var btn = MenuStyle.MakeButton(known1 ? entry.Name.ToUpper() : "? ? ?",
											   52f, MenuStyle.FONT_SMALL);
				btn.CustomMinimumSize = new Vector2(300, 52);
				btn.Disabled = !known1;

				if (known1)
				{
					var captured = entry;
					btn.Pressed += () => OpenEntry(captured);
				}
				else
				{
					btn.TooltipText = $"Como descobrir: {entry.Trigger}";
				}

				grid.AddChild(btn);
			}
		}
	}

	private void BuildEntryPage()
	{
		_entryPage = MakePage("REGISTRO", () => GoTo(Level.Categories));
		var root   = _entryPage.GetChild<VBoxContainer>(0);

		var panel = new PanelContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		panel.AddThemeStyleboxOverride("panel", MenuStyle.PanelFrame(24));
		root.AddChild(panel);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 16);
		panel.AddChild(box);

		_entryName = MenuStyle.Body("", MenuStyle.FONT_TITLE);
		_entryName.AddThemeColorOverride("font_color", new Color(1f, 0.45f, 0.45f));
		box.AddChild(_entryName);

		_entryMeta = MenuStyle.Body("", MenuStyle.FONT_SMALL);
		_entryMeta.AddThemeColorOverride("font_color", MenuStyle.ACCENT);
		box.AddChild(_entryMeta);

		box.AddChild(new HSeparator());

		var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		box.AddChild(scroll);

		_entryText = MenuStyle.Body("", MenuStyle.FONT_SMALL);
		_entryText.AutowrapMode        = TextServer.AutowrapMode.WordSmart;
		_entryText.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_entryText.CustomMinimumSize   = new Vector2(900, 0);
		_entryText.AddThemeColorOverride("font_color", MenuStyle.TEXT);
		scroll.AddChild(_entryText);
	}

	private void OpenEntry(CodexEntry entry)
	{
		_current = entry;

		_entryName.Text = entry.Name.ToUpper();
		_entryMeta.Text = $"{entry.Category.ToString().ToUpper()}   ·   SISTEMA CIRCULATORIO";
		_entryText.Text = entry.Text;

		GoTo(Level.Entry);
	}
}
