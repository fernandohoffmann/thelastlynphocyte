// ArsenalScreen.cs
using Godot;
using System.Collections.Generic;

public partial class ArsenalScreen : CanvasLayer
{
	private Label              _dnaLabel;
	private PanelContainer     _tooltip;
	private Label              _tooltipTitle;
	private Label              _tooltipDesc;
	private Label              _tooltipCost;
	private Label              _tooltipLevel;
	private Control            _canvas;
	private List<DnaNode>      _nodes = new();

	private const float NODE_SIZE       = 60f;
	private const float LINE_OFFSET_TOP = 160f;
	private const float LINE_OFFSET_BOT = 160f;

	private static readonly Vector2[] WEAPON_ANCHORS = new[]
	{
		new Vector2(329,  449),
		new Vector2(565,  443),
		new Vector2(954,  414),
		new Vector2(1189, 472),
		new Vector2(1363, 462),
		new Vector2(1600, 435),
	};

	private static readonly Vector2[] PASSIVE_ANCHORS = new[]
	{
		new Vector2(326,  592),
		new Vector2(387,  578),
		new Vector2(567,  579),
		new Vector2(624,  680),
		new Vector2(688,  560),
		new Vector2(743,  650),
		new Vector2(904,  580),
		new Vector2(958,  680),
		new Vector2(1018, 601),
		new Vector2(1246, 581),
		new Vector2(1303, 680),
		new Vector2(1414, 575),
		new Vector2(1597, 595),
	};

	public override void _Ready()
	{
		Layer       = 25;
		Visible     = false;
		ProcessMode = ProcessModeEnum.Always;

		MenuStyle.AddBackground(this, 0.93f);

		_canvas = new Control();
		_canvas.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_canvas.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(_canvas);

		var dnaImg = new TextureRect();
		dnaImg.Texture     = GD.Load<Texture2D>("res://Assets/fita_dna.png");
		dnaImg.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
		dnaImg.Position    = new Vector2(960 - 773, 540 - 355);
		dnaImg.Size        = new Vector2(1590, 672);
		dnaImg.MouseFilter = Control.MouseFilterEnum.Ignore;
		dnaImg.ZIndex      = 0;
		_canvas.AddChild(dnaImg);

		var ui = new Control();
		ui.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		ui.MouseFilter = Control.MouseFilterEnum.Ignore;
		ui.ZIndex      = 20;
		AddChild(ui);

		// ── Topo ─────────────────────────────────────────────────
		var topRow = new HBoxContainer();
		topRow.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		topRow.OffsetLeft   =  20;
		topRow.OffsetTop    =  12;
		topRow.OffsetRight  = -20;
		topRow.OffsetBottom =  56;
		ui.AddChild(topRow);

		var backBtn = MenuStyle.MakeButton("<- VOLTAR", 46f);
		backBtn.CustomMinimumSize = new Vector2(190, 46);
		backBtn.Pressed += () => Visible = false;
		topRow.AddChild(backBtn);

		var title = new Label
		{
			Text                = "ARSENAL GENETICO",
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		title.AddThemeFontSizeOverride("font_size", MenuStyle.FONT_TITLE);
		title.AddThemeColorOverride("font_color", new Color(0.95f, 0.30f, 0.34f));
		topRow.AddChild(title);

		_dnaLabel = new Label
		{
			Text                = $"DNA: {SaveData.TotalDna}",
			HorizontalAlignment = HorizontalAlignment.Right,
			CustomMinimumSize   = new Vector2(160, 0)
		};
		_dnaLabel.AddThemeFontSizeOverride("font_size", MenuStyle.FONT_BODY);
		_dnaLabel.AddThemeColorOverride("font_color", MenuStyle.DNA);
		topRow.AddChild(_dnaLabel);

		// ── Legenda ───────────────────────────────────────────────
		var legend = new HBoxContainer();
		legend.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		legend.OffsetLeft   =  20;
		legend.OffsetTop    =  62;
		legend.OffsetRight  = -20;
		legend.OffsetBottom =  86;
		legend.Alignment    = BoxContainer.AlignmentMode.Center;
		legend.AddThemeConstantOverride("separation", 20);
		ui.AddChild(legend);

		AddLegendItem(legend, new Color(0.15f, 0.45f, 1f),    "Arma desbloqueada");
		AddLegendItem(legend, new Color(0.1f,  0.65f, 0.3f),  "Passiva desbloqueada");
		AddLegendItem(legend, new Color(0.7f,  0.6f,  0.1f),  "Disponivel para compra");
		AddLegendItem(legend, new Color(0.25f, 0.25f, 0.25f), "Bloqueado");

		// ── Tooltip: PanelContainer auto-dimensiona com o conteúdo
		_tooltip = new PanelContainer();
		_tooltip.Visible = false;
		_tooltip.ZIndex  = 60;

		var ttStyle = MenuStyle.PanelFrame(16);
		_tooltip.AddThemeStyleboxOverride("panel", ttStyle);
		AddChild(_tooltip);

		var ttVbox = new VBoxContainer();
		ttVbox.AddThemeConstantOverride("separation", 4);
		ttVbox.CustomMinimumSize = new Vector2(260, 0);
		_tooltip.AddChild(ttVbox);

		_tooltipTitle = new Label();
		_tooltipTitle.AddThemeFontSizeOverride("font_size", MenuStyle.FONT_BODY);
		_tooltipTitle.AddThemeColorOverride("font_color", new Color(1f, 0.55f, 0.55f));
		ttVbox.AddChild(_tooltipTitle);

		_tooltipLevel = new Label();
		_tooltipLevel.AddThemeFontSizeOverride("font_size", 9);
		_tooltipLevel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
		ttVbox.AddChild(_tooltipLevel);

		ttVbox.AddChild(new HSeparator());

		_tooltipDesc = new Label
		{
			AutowrapMode      = TextServer.AutowrapMode.Word,
			CustomMinimumSize = new Vector2(260, 0)
		};
		_tooltipDesc.AddThemeFontSizeOverride("font_size", 9);
		ttVbox.AddChild(_tooltipDesc);

		_tooltipCost = new Label();
		_tooltipCost.AddThemeFontSizeOverride("font_size", 9);
		_tooltipCost.AddThemeColorOverride("font_color", new Color(0.4f, 1f, 0.5f));
		ttVbox.AddChild(_tooltipCost);

		CallDeferred(MethodName.Populate);
	}

	private void AddLegendItem(HBoxContainer parent, Color color, string text)
	{
		var dot = new ColorRect { Color = color, CustomMinimumSize = new Vector2(12, 12) };
		dot.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		parent.AddChild(dot);
		var lbl = new Label { Text = text };
		lbl.AddThemeFontSizeOverride("font_size", 9);
		lbl.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.75f));
		parent.AddChild(lbl);
	}

	public new void Show()
	{
		Visible        = true;
		_dnaLabel.Text = $"DNA: {SaveData.TotalDna}";
		CallDeferred(MethodName.Populate);
	}

	private void Populate()
	{
		foreach (var n in _nodes) { if (IsInstanceValid(n)) n.QueueFree(); }
		_nodes.Clear();

		foreach (Node c in _canvas.GetChildren())
			if (c is ColorRect) c.QueueFree();

		_dnaLabel.Text = $"DNA: {SaveData.TotalDna}";

		var um = UpgradeManager.Instance;
		if (um == null) return;

		var pool = um.GetPool();

		var weaponOrder = new[]
		{
			UpgradeId.Anticorpos,           UpgradeId.AnticorposGuiados,
			UpgradeId.ProteinaComplemento,  UpgradeId.TentaculoFagocitario,
			UpgradeId.TempestandeCitocinas, UpgradeId.Macrofago,
		};

		var passiveOrder = new[]
		{
			UpgradeId.SistemaImuneReforcado, UpgradeId.MetabolismoAcelerado,
			UpgradeId.ProducaoAnticorpos,    UpgradeId.CirculacaoAcelerada,
			UpgradeId.SinteseDna,            UpgradeId.Antibiotico,
			UpgradeId.Antiviral,             UpgradeId.HemoglobinaReativa,
			UpgradeId.FatorCrescimento,      UpgradeId.Quimiotaxia,
			UpgradeId.HemoglobinaAprimorada, UpgradeId.Plaquetas,
			UpgradeId.Adrenalina,
		};

		for (int i = 0; i < weaponOrder.Length; i++)
		{
			if (!pool.TryGetValue(weaponOrder[i], out var opt)) continue;
			if (i >= WEAPON_ANCHORS.Length) continue;

			bool prevUnlocked = i == 0 || SaveData.IsUnlocked(weaponOrder[i - 1].ToString());
			bool isUnlocked   = SaveData.IsUnlocked(opt.Id.ToString());
			bool canUnlock    = prevUnlocked && !isUnlocked && SaveData.TotalDna >= opt.DnaCost;

			Vector2 anchorPos = WEAPON_ANCHORS[i];
			float   nodeX     = anchorPos.X - NODE_SIZE / 2f;
			float   nodeY     = anchorPos.Y - NODE_SIZE - LINE_OFFSET_TOP;

			SpawnLine(anchorPos.X, anchorPos.Y, anchorPos.X, nodeY + NODE_SIZE, isUnlocked || canUnlock);

			var node = BuildNode(opt, isUnlocked, canUnlock, prevUnlocked, true);
			node.Position = new Vector2(nodeX, nodeY);
			_canvas.AddChild(node);
			_nodes.Add(node);
		}

		for (int i = 0; i < passiveOrder.Length; i++)
		{
			if (!pool.TryGetValue(passiveOrder[i], out var opt)) continue;
			if (i >= PASSIVE_ANCHORS.Length) continue;

			bool prevUnlocked = i == 0 || SaveData.IsUnlocked(passiveOrder[i - 1].ToString());
			bool isUnlocked   = SaveData.IsUnlocked(opt.Id.ToString());
			bool canUnlock    = prevUnlocked && !isUnlocked && SaveData.TotalDna >= opt.DnaCost;

			Vector2 anchorPos = PASSIVE_ANCHORS[i];
			float   nodeX     = anchorPos.X - NODE_SIZE / 2f;
			float   nodeY     = anchorPos.Y + LINE_OFFSET_BOT;

			SpawnLine(anchorPos.X, anchorPos.Y, anchorPos.X, nodeY, isUnlocked || canUnlock);

			var node = BuildNode(opt, isUnlocked, canUnlock, prevUnlocked, false);
			node.Position = new Vector2(nodeX, nodeY);
			_canvas.AddChild(node);
			_nodes.Add(node);
		}
	}

	private void SpawnLine(float x1, float y1, float x2, float y2, bool active)
	{
		float minX = Mathf.Min(x1, x2);
		float minY = Mathf.Min(y1, y2);
		bool  vert = Mathf.Abs(y2 - y1) > Mathf.Abs(x2 - x1);
		float w    = vert ? 2f : Mathf.Max(Mathf.Abs(x2 - x1), 2f);
		float h    = vert ? Mathf.Max(Mathf.Abs(y2 - y1), 2f) : 2f;

		var line = new ColorRect
		{
			Color    = active
				? new Color(0.75f, 0.75f, 0.75f, 0.65f)
				: new Color(0.3f,  0.3f,  0.3f,  0.3f),
			Position = new Vector2(minX, minY),
			Size     = new Vector2(w, h),
			ZIndex   = 1
		};
		line.MouseFilter = Control.MouseFilterEnum.Ignore;
		_canvas.AddChild(line);
	}

	private DnaNode BuildNode(UpgradeOption opt, bool isUnlocked, bool canUnlock,
							   bool prevUnlocked, bool isWeapon)
	{
		Color baseColor;
		if (isUnlocked)
			baseColor = isWeapon ? new Color(0.15f, 0.45f, 1f) : new Color(0.1f, 0.65f, 0.3f);
		else if (canUnlock)
			baseColor = new Color(0.7f, 0.6f, 0.1f);
		else
			baseColor = new Color(0.18f, 0.15f, 0.18f);

		float alpha = (isUnlocked || canUnlock) ? 1f : 0.4f;

		var node = new DnaNode();
		node.Init(opt, NODE_SIZE, baseColor, alpha, isUnlocked, canUnlock,
			OnHover, OnHoverExit, () => OnUnlock(opt));
		node.Size   = new Vector2(NODE_SIZE, NODE_SIZE);
		node.ZIndex = 10;
		return node;
	}

	private void OnHover(UpgradeOption opt, bool isUnlocked)
	{
		_tooltipTitle.Text = opt.Name;
		_tooltipLevel.Text = opt.LevelDesc;
		_tooltipDesc.Text  = opt.Description;
		_tooltipCost.Text  = isUnlocked
			? "Desbloqueado"
			: $"Custo: {opt.DnaCost} DNA | Voce tem: {SaveData.TotalDna}";
		_tooltip.Visible = true;
	}

	private void OnHoverExit() => _tooltip.Visible = false;

	private void OnUnlock(UpgradeOption opt)
	{
		if (SaveData.SpendDna(opt.DnaCost))
		{
			SaveData.UnlockItem(opt.Id.ToString());
			AudioManager.Instance?.PlayUiUnlock();
			Populate();
		}
	}

	public override void _Process(double delta)
	{
		if (!Visible || !_tooltip.Visible) return;
		var   mouse = GetViewport().GetMousePosition();
		var   vp    = GetViewport().GetVisibleRect().Size;
		float tx    = mouse.X + 16f;
		float ty    = mouse.Y + 16f;
		if (tx + _tooltip.Size.X > vp.X) tx = mouse.X - _tooltip.Size.X - 8f;
		if (ty + _tooltip.Size.Y > vp.Y) ty = mouse.Y - _tooltip.Size.Y - 8f;
		_tooltip.Position = new Vector2(tx, ty);
	}
}

public partial class DnaNode : Control
{
	private UpgradeOption                      _opt;
	private float                              _size;
	private Color                              _baseColor;
	private float                              _alpha;
	private bool                               _isUnlocked;
	private bool                               _canUnlock;
	private System.Action<UpgradeOption, bool> _onHover;
	private System.Action                      _onHoverExit;
	private System.Action                      _onUnlock;
	private float                              _hoverAnim = 0f;
	private bool                               _hovered   = false;
	private Texture2D                          _icon;

	public void Init(
		UpgradeOption opt, float size,
		Color baseColor, float alpha,
		bool isUnlocked, bool canUnlock,
		System.Action<UpgradeOption, bool> onHover,
		System.Action onHoverExit,
		System.Action onUnlock)
	{
		_opt         = opt;
		_size        = size;
		_baseColor   = baseColor;
		_alpha       = alpha;
		_isUnlocked  = isUnlocked;
		_canUnlock   = canUnlock;
		_onHover     = onHover;
		_onHoverExit = onHoverExit;
		_onUnlock    = onUnlock;
		_icon        = IconAtlas.Get(opt.Id);

		CustomMinimumSize = new Vector2(size, size);
		MouseFilter       = MouseFilterEnum.Stop;

		MouseEntered += () => { _hovered = true;  _onHover?.Invoke(_opt, _isUnlocked); };
		MouseExited  += () => { _hovered = false; _onHoverExit?.Invoke(); };
		GuiInput     += (InputEvent e) =>
		{
			if (e is InputEventMouseButton mb && mb.Pressed
				&& mb.ButtonIndex == MouseButton.Left && _canUnlock)
				_onUnlock?.Invoke();
		};
	}

	public override void _Process(double delta)
	{
		_hoverAnim = Mathf.Lerp(_hoverAnim, _hovered ? 1f : 0f, 14f * (float)delta);
		QueueRedraw();
	}

	public override void _Draw()
	{
		float half = _size / 2f;
		var   c    = new Vector2(half, half);

		// halo no hover, na cor da moldura do menu
		if (_hoverAnim > 0.01f)
			DrawCircle(c, half + 10f * _hoverAnim,
				new Color(MenuStyle.LINE_ON.R, MenuStyle.LINE_ON.G, MenuStyle.LINE_ON.B,
						  0.32f * _hoverAnim));

		// miolo escuro com um toque da cor do ramo, igual ao medalhao do menu
		DrawCircle(c, half - 2f, new Color(0.08f, 0.02f, 0.03f, 0.95f * _alpha));
		DrawCircle(c, half - 2f,
			new Color(_baseColor.R, _baseColor.G, _baseColor.B, _alpha * 0.32f));

		// Ícone dentro da bolinha
		if (_icon != null)
		{
			float pad = half * 0.25f;
			float iSz = _size - pad * 2f;
			DrawTextureRectRegion(
				_icon,
				new Rect2(pad, pad, iSz, iSz),
				new Rect2(0, 0, _icon.GetWidth(), _icon.GetHeight()),
				new Color(1f, 1f, 1f, _alpha)
			);
		}

		// Borda
		float borderA = _isUnlocked ? 1f : (_canUnlock ? 0.85f : 0.30f);
		Color ring    = _isUnlocked ? MenuStyle.LINE_ON
					  : _canUnlock  ? MenuStyle.LINE
									: MenuStyle.LINE_OFF;
		DrawArc(c, half - 2f, 0f, Mathf.Tau, 40,
			new Color(ring.R, ring.G, ring.B, borderA), 2.5f);

		// Indicador de estado
		if (!_isUnlocked && !_canUnlock)
		{
			DrawCircle(c, half * 0.42f, new Color(0f, 0f, 0f, 0.72f));
			DrawString(ThemeDB.FallbackFont,
				new Vector2(half - 4f, half + 5f),
				"?", HorizontalAlignment.Left, -1, 16,
				MenuStyle.TEXT_OFF);
		}
		else if (_canUnlock)
		{
			DrawCircle(c, half * 0.32f, new Color(0f, 0f, 0f, 0.65f));
			DrawString(ThemeDB.FallbackFont,
				new Vector2(half - 4f, half + 5f),
				"!", HorizontalAlignment.Left, -1, 16,
				new Color(1f, 0.75f, 0.35f));
		}

		// Nome 12px abaixo da bolinha, centralizado
		float nameWidth = _size + 30f;
		float nameX     = half - nameWidth / 2f;
		string shortName = _opt.Name.Length > 9 ? _opt.Name.Substring(0, 9) : _opt.Name;
		DrawString(ThemeDB.FallbackFont,
			new Vector2(nameX, _size + 12f),
			shortName,
			HorizontalAlignment.Center,
			(int)nameWidth,
			7,
			new Color(0.85f, 0.85f, 0.85f, _alpha));

		// Custo DNA 23px abaixo da bolinha
		if (!_isUnlocked && _opt.DnaCost > 0)
			DrawString(ThemeDB.FallbackFont,
				new Vector2(nameX, _size + 23f),
				$"{_opt.DnaCost}",
				HorizontalAlignment.Center,
				(int)nameWidth,
				7,
				new Color(
					_canUnlock ? 0.3f : 0.3f,
					_canUnlock ? 1f   : 0.3f,
					_canUnlock ? 0.4f : 0.3f,
					_alpha));
	}
}
