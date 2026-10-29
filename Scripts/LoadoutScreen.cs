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
		GamePhase.Changed += OnPhaseChanged;

		Layer       = 20;
		Visible     = false;
		ProcessMode = ProcessModeEnum.Always;

		MenuStyle.AddBackground(this, 0.90f);

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

		var backBtn = MenuStyle.MakeButton("<- VOLTAR", 46f);
		backBtn.CustomMinimumSize = new Vector2(190, 46);
		backBtn.Pressed += () => Visible = false;
		topRow.AddChild(backBtn);

		var title = new Label
		{
			Text                = "SELECIONAR LOADOUT",
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		title.AddThemeFontSizeOverride("font_size", MenuStyle.FONT_TITLE);
		title.AddThemeColorOverride("font_color", new Color(0.95f, 0.30f, 0.34f));
		topRow.AddChild(title);

		var spacer = new Control { CustomMinimumSize = new Vector2(190, 0) };
		topRow.AddChild(spacer);

		var subtitle = new Label
		{
			Text                = "Passe o mouse para ver descricao. Clique para equipar/remover.",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		subtitle.AddThemeFontSizeOverride("font_size", 16);
		root.AddChild(subtitle);

		root.AddChild(new HSeparator());

		BuildPhaseSelector(root);

		var columns = new HBoxContainer();
		columns.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		columns.AddThemeConstantOverride("separation", 48);
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
		_weaponCountLabel.AddThemeFontSizeOverride("font_size", 16);
		weaponCol.AddChild(_weaponCountLabel);

		var weaponScroll = new ScrollContainer();
		weaponScroll.SizeFlagsVertical    = Control.SizeFlags.ExpandFill;
		weaponScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		weaponCol.AddChild(weaponScroll);

		_weaponGrid = new GridContainer { Columns = 5 };
		_weaponGrid.AddThemeConstantOverride("h_separation", 20);
		_weaponGrid.AddThemeConstantOverride("v_separation", 20);
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
		_passiveCountLabel.AddThemeFontSizeOverride("font_size", 16);
		passiveCol.AddChild(_passiveCountLabel);

		var passiveScroll = new ScrollContainer();
		passiveScroll.SizeFlagsVertical    = Control.SizeFlags.ExpandFill;
		passiveScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		passiveCol.AddChild(passiveScroll);

		_passiveGrid = new GridContainer { Columns = 5 };
		_passiveGrid.AddThemeConstantOverride("h_separation", 20);
		_passiveGrid.AddThemeConstantOverride("v_separation", 20);
		passiveScroll.AddChild(_passiveGrid);

		root.AddChild(new HSeparator());

		var startBtn = new Button { Text = "INICIAR PARTIDA" };
		startBtn.CustomMinimumSize = new Vector2(0, 60);
		startBtn.AddThemeFontSizeOverride("font_size", 16);
		startBtn.Pressed += OnStart;
		root.AddChild(startBtn);

		// ── Tooltip ───────────────────────────────────────────────
		_tooltip = new PanelContainer();
		_tooltip.Visible = false;
		_tooltip.ZIndex  = 50;

		var ttStyle = MenuStyle.PanelFrame(16);
		_tooltip.AddThemeStyleboxOverride("panel", ttStyle);
		AddChild(_tooltip);

		var ttVbox = new VBoxContainer();
		ttVbox.AddThemeConstantOverride("separation", 4);
		ttVbox.CustomMinimumSize = new Vector2(520, 0);
		_tooltip.AddChild(ttVbox);

		_tooltipName = new Label { Text = "" };
		_tooltipName.AddThemeFontSizeOverride("font_size", 16);
		_tooltipName.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
		ttVbox.AddChild(_tooltipName);

		_tooltipDesc = new Label
		{
			Text              = "",
			AutowrapMode      = TextServer.AutowrapMode.Word,
			CustomMinimumSize = new Vector2(520, 0)
		};
		_tooltipDesc.AddThemeFontSizeOverride("font_size", 16);
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

	// ── Seletor de fase ───────────────────────────────────────────────────
	private HBoxContainer _phaseRow;

	private void BuildPhaseSelector(Control parent)
	{
		_phaseRow = new HBoxContainer();
		_phaseRow.Alignment = BoxContainer.AlignmentMode.Center;
		_phaseRow.AddThemeConstantOverride("separation", 16);
		parent.AddChild(_phaseRow);

		RefreshPhaseButtons();
	}

	private void RefreshPhaseButtons()
	{
		if (_phaseRow == null) return;

		// RemoveChild antes do QueueFree: o free e diferido, e sem remover agora
		// os botoes antigos continuam visiveis lado a lado com os novos
		foreach (var c in _phaseRow.GetChildren())
		{
			_phaseRow.RemoveChild(c);
			c.QueueFree();
		}

		for (int n = 1; n <= GamePhase.MAX_PHASE; n++)
		{
			int  number   = n;
			var  data     = GamePhase.Get(n);
			bool unlocked = GamePhase.IsUnlocked(n);
			bool current  = GamePhase.Current == n;

			string label = unlocked ? $"FASE {n} - {data.Name}" : $"FASE {n} - BLOQUEADA";

			var btn = MenuStyle.MakeButton(label, 52f, MenuStyle.FONT_SMALL);
			btn.CustomMinimumSize = new Vector2(420, 52);
			// a fase atual fica travada e acesa, marcando a selecao
			btn.Disabled = !unlocked || current;

			// a fase atual fica acesa; as outras liberadas ficam clicaveis
			if (current)
				btn.AddThemeStyleboxOverride("disabled",
					MenuStyle.Frame(MenuStyle.FILL_ON, MenuStyle.LINE_ON, 3));

			if (!unlocked)
				btn.TooltipText = "Conclua a fase anterior para liberar.";

			btn.Pressed += () => GamePhase.Select(number);

			_phaseRow.AddChild(btn);
		}
	}

	// Troca de fase: repinta a tela inteira com a paleta nova.
	private void OnPhaseChanged()
	{
		MenuStyle.ApplyTheme(GetTree());
		MenuStyle.Restyle(this);
		RefreshPhaseButtons();
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

	private const float ICON_SIZE = 150f;
	private const float NAME_H    = 30f;

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

		CustomMinimumSize = new Vector2(ICON_SIZE + 10f, ICON_SIZE + NAME_H);
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

		var rect = new Rect2(0, 0, w, ICON_SIZE);

		// mesma moldura do menu: interior escuro, borda vermelha, canto redondo
		Color border = _greyed   ? MenuStyle.LINE_OFF
					 : _selected ? MenuStyle.LINE_ON
								 : MenuStyle.LINE;
		Color fill   = _greyed   ? new Color(0.10f, 0.05f, 0.06f, 0.6f)
					 : _selected ? MenuStyle.FILL_ON
								 : MenuStyle.FILL;

		// brilho externo quando escolhido
		if (_selected)
			DrawStyleBox(
				MenuStyle.Frame(new Color(0, 0, 0, 0),
					new Color(MenuStyle.LINE_ON.R, MenuStyle.LINE_ON.G, MenuStyle.LINE_ON.B, 0.28f), 5),
				new Rect2(-5, -5, w + 10, ICON_SIZE + 10));

		DrawStyleBox(MenuStyle.Frame(fill, border), rect);
		MenuStyle.DrawCornerDots(this, rect, border, _selected);

		// Ícone
		if (_icon != null)
		{
			float pad = 9f;
			DrawTextureRectRegion(
				_icon,
				new Rect2(pad, pad, w - pad * 2f, ICON_SIZE - pad * 2f),
				new Rect2(0, 0, _icon.GetWidth(), _icon.GetHeight()),
				_greyed ? new Color(1, 1, 1, 0.35f) : Colors.White
			);
		}

		// marca de equipado
		if (_selected)
		{
			var mark = new Vector2(w - 26f, 26f);
			DrawCircle(mark, 12f, new Color(MenuStyle.LINE_ON.R, MenuStyle.LINE_ON.G, MenuStyle.LINE_ON.B, 0.85f));
			DrawString(ThemeDB.FallbackFont,
				new Vector2(mark.X - 12f, mark.Y + 6f),
				"v", HorizontalAlignment.Center, 24, 16, Colors.White);
		}

		// Nome centralizado
		string name = ShortName(_opt.Id);
		DrawString(ThemeDB.FallbackFont,
			new Vector2(-10f, ICON_SIZE + 22f),
			name,
			HorizontalAlignment.Center,
			(int)(w + 20f),
			MenuStyle.FONT_BODY,
			_greyed ? MenuStyle.TEXT_OFF : MenuStyle.TEXT);
	}
}
