using Godot;
using System.Collections.Generic;

public partial class LoadoutScreen : CanvasLayer
{
	private List<UpgradeId> _selectedWeapons  = new();
	private List<UpgradeId> _selectedPassives = new();

	private GridContainer  _weaponGrid;
	private GridContainer  _passiveGrid;
	private Label          _weaponCountLabel;
	private Label          _passiveCountLabel;
	private PanelContainer _tooltip;
	private Label          _tooltipName;
	private Label          _tooltipDesc;

	private Dictionary<UpgradeId, IconSlot> _iconPanels = new();

	// Referências injetadas pelo MainMenu
	public static IntroScreen   IntroScreenRef   = null;
	public static ConfirmDialog ConfirmDialogRef = null;

	public override void _Ready()
	{
		Layer       = 20;
		Visible     = false;
		ProcessMode = ProcessModeEnum.Always;

		var bg = new ColorRect { Color = new Color(0.05f, 0.03f, 0.05f, 0.97f) };
		bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(bg);

		var root = new VBoxContainer();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.OffsetLeft   =  40;
		root.OffsetTop    =  30;
		root.OffsetRight  = -40;
		root.OffsetBottom = -30;
		root.AddThemeConstantOverride("separation", 14);
		AddChild(root);

		var topRow = new HBoxContainer();
		root.AddChild(topRow);

		var backBtn = new Button { Text = "<- Voltar" };
		backBtn.CustomMinimumSize = new Vector2(110, 36);
		backBtn.Pressed += () => Visible = false;
		topRow.AddChild(backBtn);

		var title = new Label
		{
			Text                = "SELECIONAR LOADOUT",
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		title.AddThemeFontSizeOverride("font_size", 16);
		topRow.AddChild(title);

		var spacer = new Control { CustomMinimumSize = new Vector2(110, 0) };
		topRow.AddChild(spacer);

		var subtitle = new Label
		{
			Text                = "Passe o mouse para ver descricao. Clique para equipar/remover.",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		subtitle.AddThemeFontSizeOverride("font_size", 9);
		root.AddChild(subtitle);

		root.AddChild(new HSeparator());

		var columns = new HBoxContainer();
		columns.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		columns.AddThemeConstantOverride("separation", 30);
		root.AddChild(columns);

		// ── Armas ─────────────────────────────────────────────────
		var weaponCol = new VBoxContainer();
		weaponCol.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		weaponCol.AddThemeConstantOverride("separation", 10);
		columns.AddChild(weaponCol);

		_weaponCountLabel = new Label
		{
			Text                = "ARMAS  (0 / 3)",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_weaponCountLabel.AddThemeFontSizeOverride("font_size", 12);
		weaponCol.AddChild(_weaponCountLabel);

		var weaponScroll = new ScrollContainer();
		weaponScroll.SizeFlagsVertical    = Control.SizeFlags.ExpandFill;
		weaponScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		weaponCol.AddChild(weaponScroll);

		_weaponGrid = new GridContainer { Columns = 4 };
		_weaponGrid.AddThemeConstantOverride("h_separation", 12);
		_weaponGrid.AddThemeConstantOverride("v_separation", 12);
		weaponScroll.AddChild(_weaponGrid);

		columns.AddChild(new VSeparator());

		// ── Passivas ──────────────────────────────────────────────
		var passiveCol = new VBoxContainer();
		passiveCol.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		passiveCol.AddThemeConstantOverride("separation", 10);
		columns.AddChild(passiveCol);

		_passiveCountLabel = new Label
		{
			Text                = "PASSIVAS  (0 / 5)",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_passiveCountLabel.AddThemeFontSizeOverride("font_size", 12);
		passiveCol.AddChild(_passiveCountLabel);

		var passiveScroll = new ScrollContainer();
		passiveScroll.SizeFlagsVertical    = Control.SizeFlags.ExpandFill;
		passiveScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		passiveCol.AddChild(passiveScroll);

		_passiveGrid = new GridContainer { Columns = 4 };
		_passiveGrid.AddThemeConstantOverride("h_separation", 12);
		_passiveGrid.AddThemeConstantOverride("v_separation", 12);
		passiveScroll.AddChild(_passiveGrid);

		root.AddChild(new HSeparator());

		var startBtn = new Button { Text = "INICIAR PARTIDA" };
		startBtn.CustomMinimumSize = new Vector2(0, 48);
		startBtn.AddThemeFontSizeOverride("font_size", 14);
		startBtn.Pressed += OnStart;
		root.AddChild(startBtn);

		// ── Tooltip ───────────────────────────────────────────────
		_tooltip = new PanelContainer();
		_tooltip.Visible = false;
		_tooltip.ZIndex  = 50;

		var ttStyle = new StyleBoxFlat();
		ttStyle.BgColor             = new Color(0.08f, 0.06f, 0.10f, 0.97f);
		ttStyle.BorderColor         = new Color(0.5f, 0.5f, 0.6f, 0.8f);
		ttStyle.BorderWidthLeft     = 1;
		ttStyle.BorderWidthRight    = 1;
		ttStyle.BorderWidthTop      = 1;
		ttStyle.BorderWidthBottom   = 1;
		ttStyle.ContentMarginLeft   = 10;
		ttStyle.ContentMarginRight  = 10;
		ttStyle.ContentMarginTop    =  8;
		ttStyle.ContentMarginBottom =  8;
		_tooltip.AddThemeStyleboxOverride("panel", ttStyle);
		AddChild(_tooltip);

		var ttVbox = new VBoxContainer();
		ttVbox.AddThemeConstantOverride("separation", 4);
		ttVbox.CustomMinimumSize = new Vector2(240, 0);
		_tooltip.AddChild(ttVbox);

		_tooltipName = new Label { Text = "" };
		_tooltipName.AddThemeFontSizeOverride("font_size", 10);
		_tooltipName.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
		ttVbox.AddChild(_tooltipName);

		_tooltipDesc = new Label
		{
			Text              = "",
			AutowrapMode      = TextServer.AutowrapMode.Word,
			CustomMinimumSize = new Vector2(240, 0)
		};
		_tooltipDesc.AddThemeFontSizeOverride("font_size", 8);
		ttVbox.AddChild(_tooltipDesc);
	}

	public void Populate()
	{
		_selectedWeapons.Clear();
		_selectedPassives.Clear();
		_iconPanels.Clear();

		foreach (Node c in _weaponGrid.GetChildren())  c.QueueFree();
		foreach (Node c in _passiveGrid.GetChildren()) c.QueueFree();

		var um = UpgradeManager.Instance;
		if (um == null) return;

		foreach (var opt in um.GetUnlockedWeapons())
			_weaponGrid.AddChild(BuildIcon(opt, isWeapon: true));

		foreach (var opt in um.GetUnlockedPassives())
			_passiveGrid.AddChild(BuildIcon(opt, isWeapon: false));

		UpdateCountLabels();
		RefreshIcons();
	}

	private Control BuildIcon(UpgradeOption opt, bool isWeapon)
	{
		var slot = new IconSlot();
		slot.Setup(
			opt, isWeapon,
			() =>
			{
				_tooltipName.Text = opt.Name;
				_tooltipDesc.Text = opt.Description;
				_tooltip.Visible  = true;
			},
			() => _tooltip.Visible = false,
			() => ToggleItem(opt.Id, isWeapon)
		);
		_iconPanels[opt.Id] = slot;
		return slot;
	}

	private void ToggleItem(UpgradeId id, bool isWeapon)
	{
		if (isWeapon)
		{
			if (_selectedWeapons.Contains(id))
				_selectedWeapons.Remove(id);
			else if (_selectedWeapons.Count < UpgradeManager.MAX_WEAPONS)
				_selectedWeapons.Add(id);
		}
		else
		{
			if (_selectedPassives.Contains(id))
				_selectedPassives.Remove(id);
			else if (_selectedPassives.Count < UpgradeManager.MAX_PASSIVES)
				_selectedPassives.Add(id);
		}

		UpdateCountLabels();
		RefreshIcons();
		AudioManager.Instance?.PlayUiClick();
	}

	private void RefreshIcons()
	{
		var um = UpgradeManager.Instance;
		if (um == null) return;

		foreach (var kv in _iconPanels)
		{
			var id   = kv.Key;
			var slot = kv.Value;

			bool isWeapon   = um.GetUpgrade(id)?.IsWeapon ?? false;
			bool isSelected = isWeapon
				? _selectedWeapons.Contains(id)
				: _selectedPassives.Contains(id);
			bool isFull = isWeapon
				? _selectedWeapons.Count  >= UpgradeManager.MAX_WEAPONS
				: _selectedPassives.Count >= UpgradeManager.MAX_PASSIVES;

			slot.SetState(isSelected, !isSelected && isFull);
		}
	}

	private void UpdateCountLabels()
	{
		_weaponCountLabel.Text  = $"ARMAS  ({_selectedWeapons.Count} / {UpgradeManager.MAX_WEAPONS})";
		_passiveCountLabel.Text = $"PASSIVAS  ({_selectedPassives.Count} / {UpgradeManager.MAX_PASSIVES})";
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

	private void OnStart()
	{
		if (_selectedWeapons.Count == 0)
			_selectedWeapons.Add(UpgradeId.Anticorpos);

		UpgradeManager.Instance?.ApplyLoadout(_selectedWeapons, _selectedPassives);
		Visible = false;

		// Toca a intro antes de entrar no jogo
		if (IntroScreenRef != null)
		{
			IntroScreenRef.Play(() =>
			{
				AudioManager.Instance?.StartRockMusic(fadeIn: true, fadeDuration: 3f);
				GetTree().ChangeSceneToFile("res://Scenes/game_scene.tscn");
			});
		}
		else
		{
			AudioManager.Instance?.StartRockMusic(fadeIn: true, fadeDuration: 3f);
			GetTree().ChangeSceneToFile("res://Scenes/game_scene.tscn");
		}
	}

	public new void Show()
	{
		Populate();
		Visible = true;
	}
}

public partial class IconSlot : Control
{
	private UpgradeOption _opt;
	private bool          _isWeapon;
	private bool          _selected = false;
	private bool          _greyed   = false;
	private Texture2D     _icon;

	private System.Action _onEnter;
	private System.Action _onExit;
	private System.Action _onClick;

	private const float ICON_SIZE = 90f;
	private const float NAME_H    = 16f;

	private static string ShortName(UpgradeId id) => id switch
	{
		UpgradeId.Anticorpos             => "Anticorpos",
		UpgradeId.AnticorposGuiados      => "Ant.Guiados",
		UpgradeId.ProteinaComplemento    => "Proteina",
		UpgradeId.TentaculoFagocitario   => "Tentaculo",
		UpgradeId.TempestandeCitocinas   => "Citocinas",
		UpgradeId.Macrofago              => "Macrofago",
		UpgradeId.SistemaImuneReforcado  => "Sist.Imune",
		UpgradeId.MetabolismoAcelerado   => "Metabolismo",
		UpgradeId.ProducaoAnticorpos     => "Prod.Antic.",
		UpgradeId.CirculacaoAcelerada    => "Circulacao",
		UpgradeId.SinteseDna             => "Sint.DNA",
		UpgradeId.Antibiotico            => "Antibiotico",
		UpgradeId.Antiviral              => "Antiviral",
		UpgradeId.HemoglobinaReativa     => "Hemog.Reat.",
		UpgradeId.FatorCrescimento       => "Fat.Cresc.",
		UpgradeId.Quimiotaxia            => "Quimiotaxia",
		UpgradeId.HemoglobinaAprimorada  => "Hemog.Apr.",
		UpgradeId.Plaquetas              => "Plaquetas",
		UpgradeId.Adrenalina             => "Adrenalina",
		_                                => "?"
	};

	public void Setup(UpgradeOption opt, bool isWeapon,
		System.Action onEnter, System.Action onExit, System.Action onClick)
	{
		_opt      = opt;
		_isWeapon = isWeapon;
		_icon     = IconAtlas.Get(opt.Id);
		_onEnter  = onEnter;
		_onExit   = onExit;
		_onClick  = onClick;

		CustomMinimumSize = new Vector2(100, ICON_SIZE + NAME_H);
		MouseFilter       = MouseFilterEnum.Stop;

		MouseEntered += () => _onEnter?.Invoke();
		MouseExited  += () => _onExit?.Invoke();
		GuiInput += (InputEvent e) =>
		{
			if (e is InputEventMouseButton mb && mb.Pressed
				&& mb.ButtonIndex == MouseButton.Left)
				_onClick?.Invoke();
		};
	}

	public void SetState(bool selected, bool greyed)
	{
		_selected = selected;
		_greyed   = greyed;
		QueueRedraw();
	}

	public override void _Draw()
	{
		float w = Size.X;

		// Fundo do quadrado
		Color bgCol = _isWeapon
			? new Color(0.08f, 0.15f, 0.35f)
			: new Color(0.05f, 0.25f, 0.10f);
		if (_greyed) bgCol = new Color(0.15f, 0.15f, 0.15f);
		DrawRect(new Rect2(0, 0, w, ICON_SIZE), bgCol);

		// Borda
		Color border = _selected
			? new Color(0.3f, 1f, 0.4f)
			: new Color(0.5f, 0.5f, 0.5f, 0.5f);
		DrawRect(new Rect2(0, 0, w, ICON_SIZE), border, false, 2f);

		// Ícone
		if (_icon != null)
		{
			float pad = 5f;
			DrawTextureRectRegion(
				_icon,
				new Rect2(pad, pad, w - pad * 2f, ICON_SIZE - pad * 2f),
				new Rect2(0, 0, _icon.GetWidth(), _icon.GetHeight()),
				_greyed ? new Color(1, 1, 1, 0.35f) : Colors.White
			);
		}

		// Overlay selecionado
		if (_selected)
		{
			DrawRect(new Rect2(0, 0, w, ICON_SIZE), new Color(0.3f, 1f, 0.4f, 0.18f));
			DrawString(ThemeDB.FallbackFont,
				new Vector2(w - 14f, 13f),
				"v", HorizontalAlignment.Left, -1, 12,
				new Color(0.3f, 1f, 0.4f));
		}

		// Nome centralizado
		string name = ShortName(_opt.Id);
		DrawString(ThemeDB.FallbackFont,
			new Vector2(0f, ICON_SIZE + 12f),
			name,
			HorizontalAlignment.Center,
			(int)w,
			7,
			new Color(0.9f, 0.9f, 0.9f, _greyed ? 0.4f : 1f));
	}
}
