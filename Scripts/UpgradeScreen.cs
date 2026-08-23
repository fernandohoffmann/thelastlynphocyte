using Godot;
using System.Collections.Generic;

public partial class UpgradeScreen : CanvasLayer
{
	private HBoxContainer _cardRow;

	public override void _Ready()
	{
		Layer       = 15;
		Visible     = false;
		ProcessMode = ProcessModeEnum.Always;

		var bg = new ColorRect { Color = new Color(0f, 0f, 0f, 0.75f) };
		bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(bg);

		var panel = new Panel();
		panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		panel.CustomMinimumSize = new Vector2(740, 300);
		panel.Position          = new Vector2(-370, -150);
		AddChild(panel);

		var root = new VBoxContainer();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.OffsetLeft   =  16;
		root.OffsetTop    =  16;
		root.OffsetRight  = -16;
		root.OffsetBottom = -16;
		root.AddThemeConstantOverride("separation", 10);
		panel.AddChild(root);

		var title = new Label
		{
			Text                = "SUBIU DE NIVEL  -  Escolha uma melhoria",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		title.AddThemeFontSizeOverride("font_size", 13);
		root.AddChild(title);

		root.AddChild(new HSeparator());

		_cardRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		_cardRow.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_cardRow.AddThemeConstantOverride("separation", 20);
		root.AddChild(_cardRow);
	}

	public void ShowOptions(List<UpgradeOption> options)
	{
		foreach (Node c in _cardRow.GetChildren()) c.QueueFree();
		foreach (var opt in options) _cardRow.AddChild(BuildCard(opt));
		Visible = true;
		GameManager.Instance.PauseGame();
	}

	private Control BuildCard(UpgradeOption opt)
	{
		var card = new UpgradeCard();
		card.Setup(opt, () => OnChoose(opt.Id));
		return card;
	}

	private void OnChoose(UpgradeId id)
	{
		UpgradeManager.Instance?.ApplyUpgrade(id);
		Visible = false;
		GameManager.Instance.ResumeGame();
	}
}

public partial class UpgradeCard : Control
{
	private UpgradeOption _opt;
	private Texture2D     _icon;
	private Label         _nameLabel;
	private Label         _lvlLabel;
	private Label         _descLabel;

	public void Setup(UpgradeOption opt, System.Action onChoose)
	{
		_opt              = opt;
		_icon             = IconAtlas.Get(opt.Id);
		CustomMinimumSize = new Vector2(210, 240);
		MouseFilter       = MouseFilterEnum.Ignore;

		// Fundo do card via Panel
		var panel = new Panel();
		panel.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.SetAnchorsPreset(LayoutPreset.FullRect);
		vbox.OffsetLeft   =  10;
		vbox.OffsetTop    =  10;
		vbox.OffsetRight  = -10;
		vbox.OffsetBottom = -10;
		vbox.AddThemeConstantOverride("separation", 6);
		AddChild(vbox);

		// Ícone desenhado num Control customizado
		var iconCtrl = new UpgradeIconControl();
		iconCtrl.Setup(_icon, opt.IsWeapon);
		iconCtrl.CustomMinimumSize = new Vector2(0, 80);
		iconCtrl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		vbox.AddChild(iconCtrl);

		var typeTag = new Label
		{
			Text                = opt.IsWeapon ? "[ ARMA ]" : "[ PASSIVA ]",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		typeTag.AddThemeFontSizeOverride("font_size", 9);
		vbox.AddChild(typeTag);

		_nameLabel = new Label
		{
			Text                = opt.Name,
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode        = TextServer.AutowrapMode.Word
		};
		_nameLabel.AddThemeFontSizeOverride("font_size", 11);
		vbox.AddChild(_nameLabel);

		_lvlLabel = new Label
		{
			Text                = $"Nivel {opt.CurrentLevel + 1} / {UpgradeManager.MAX_LEVEL}",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_lvlLabel.AddThemeFontSizeOverride("font_size", 9);
		vbox.AddChild(_lvlLabel);

		vbox.AddChild(new HSeparator());

		_descLabel = new Label
		{
			Text                = opt.LevelDesc,
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode        = TextServer.AutowrapMode.Word
		};
		_descLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
		_descLabel.AddThemeFontSizeOverride("font_size", 9);
		vbox.AddChild(_descLabel);

		var btn = new Button { Text = "Escolher" };
		btn.CustomMinimumSize = new Vector2(0, 32);
		btn.AddThemeFontSizeOverride("font_size", 10);
		btn.Pressed += () => onChoose();
		vbox.AddChild(btn);
	}
}

public partial class UpgradeIconControl : Control
{
	private Texture2D _icon;
	private bool      _isWeapon;

	public void Setup(Texture2D icon, bool isWeapon)
	{
		_icon     = icon;
		_isWeapon = isWeapon;
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Draw()
{
	float w = Size.X;
	float h = Size.Y;

	Color bgCol = _isWeapon
		? new Color(0.08f, 0.15f, 0.35f)
		: new Color(0.05f, 0.25f, 0.10f);
	DrawRect(new Rect2(0, 0, w, h), bgCol);

	if (_icon != null)
	{
		float pad    = 8f;
		float dstW   = w - pad * 2f;
		float dstH   = h - pad * 2f;
		float srcW   = _icon.GetWidth();
		float srcH   = _icon.GetHeight();

		// Mantém aspecto
		float scale  = Mathf.Min(dstW / srcW, dstH / srcH);
		float iW     = srcW * scale;
		float iH     = srcH * scale;
		float offX   = pad + (dstW - iW) / 2f;
		float offY   = pad + (dstH - iH) / 2f;

		DrawTextureRectRegion(
			_icon,
			new Rect2(offX, offY, iW, iH),
			new Rect2(0, 0, srcW, srcH),
			Colors.White
		);
	}
}
	}
