// WeaponDisplay.cs
using Godot;
using System.Collections.Generic;

public partial class WeaponDisplay : CanvasLayer
{
	private VBoxContainer             _container;
	private Dictionary<string, Label> _levelLabels = new();
	private PanelContainer            _tooltip;
	private Label                     _ttName;
	private Label                     _ttDesc;

	public override void _Ready()
	{
		Layer = 6;

		var root = new Control();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(root);

		_container = new VBoxContainer();
		_container.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		_container.Position = new Vector2(10f, 130f);
		_container.AddThemeConstantOverride("separation", 8);
		_container.MouseFilter = Control.MouseFilterEnum.Ignore;
		root.AddChild(_container);

		_tooltip = new PanelContainer();
		_tooltip.Visible     = false;
		_tooltip.ZIndex      = 30;
		_tooltip.MouseFilter = Control.MouseFilterEnum.Ignore;

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
		root.AddChild(_tooltip);

		var ttVbox = new VBoxContainer();
		ttVbox.AddThemeConstantOverride("separation", 4);
		ttVbox.CustomMinimumSize = new Vector2(220, 0);
		ttVbox.MouseFilter       = Control.MouseFilterEnum.Ignore;
		_tooltip.AddChild(ttVbox);

		_ttName = new Label();
		_ttName.AddThemeFontSizeOverride("font_size", 10);
		_ttName.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
		_ttName.MouseFilter = Control.MouseFilterEnum.Ignore;
		ttVbox.AddChild(_ttName);

		_ttDesc = new Label
		{
			AutowrapMode      = TextServer.AutowrapMode.Word,
			CustomMinimumSize = new Vector2(220, 0),
			MouseFilter       = Control.MouseFilterEnum.Ignore
		};
		_ttDesc.AddThemeFontSizeOverride("font_size", 8);
		ttVbox.AddChild(_ttDesc);

		CallDeferred(MethodName.PopulateFromLoadout);
	}

	private void PopulateFromLoadout()
	{
		var um = UpgradeManager.Instance;
		if (um == null) return;

		foreach (var id in um.EquippedWeapons)
		{
			var opt = um.GetUpgrade(id);
			if (opt == null) continue;
			RegisterWeapon(id.ToString(), id);
			UpdateWeaponLevel(id.ToString(), 1);
		}
	}

	public void RegisterWeapon(string weaponName, UpgradeId id)
	{
		if (_levelLabels.ContainsKey(weaponName)) return;

		var opt  = UpgradeManager.Instance?.GetUpgrade(id);
		var slot = new WeaponSlot();
		slot.Setup(id,
			() => ShowTooltip(opt?.Name ?? "", opt?.Description ?? "", slot),
			() => _tooltip.Visible = false);
		_container.AddChild(slot);
		_levelLabels[weaponName] = slot.LevelLabel;
	}

	public void UpdateWeaponLevel(string weaponName, int level)
	{
		if (_levelLabels.TryGetValue(weaponName, out var label))
			label.Text = $"Nv.{level}";
	}

	private void ShowTooltip(string name, string desc, Control anchor)
	{
		_ttName.Text     = name;
		_ttDesc.Text     = desc;
		_tooltip.Visible = true;
		CallDeferred(MethodName.PositionTooltip, anchor);
	}

	private void PositionTooltip(Control anchor)
	{
		if (!IsInstanceValid(anchor)) return;
		// Posiciona acima do slot — igual ao PassiveDisplay
		float x = anchor.GlobalPosition.X;
		float y = anchor.GlobalPosition.Y - _tooltip.Size.Y - 6f;
		x = Mathf.Max(x, 4f);
		_tooltip.Position = new Vector2(x, y);
	}
}

public partial class WeaponSlot : Control
{
	public Label      LevelLabel { get; private set; }
	private Texture2D _icon;

	// Tudo no Setup — igual ao PassiveSlotControl que funciona
	public void Setup(UpgradeId id, System.Action onEnter, System.Action onExit)
	{
		_icon             = IconAtlas.Get(id);
		CustomMinimumSize = new Vector2(72, 80);
		MouseFilter       = MouseFilterEnum.Stop;
		MouseEntered     += () => onEnter();
		MouseExited      += () => onExit();

		LevelLabel = new Label
		{
			Text                = "Nv.1",
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter         = MouseFilterEnum.Ignore
		};
		LevelLabel.SetAnchorsPreset(LayoutPreset.BottomWide);
		LevelLabel.OffsetTop    = -12f;
		LevelLabel.OffsetBottom =   0f;
		LevelLabel.AddThemeFontSizeOverride("font_size", 7);
		AddChild(LevelLabel);
	}

	public override void _Draw()
	{
		float w = Size.X;
		float h = Size.Y - 12f;

		DrawRect(new Rect2(0, 0, w, h), new Color(0.08f, 0.15f, 0.35f, 0.85f));
		DrawRect(new Rect2(0, 0, w, h), new Color(0.3f, 0.5f, 0.9f, 0.5f), false, 1.5f);

		if (_icon != null)
		{
			float pad   = 4f;
			float dstW  = w - pad * 2f;
			float dstH  = h - pad * 2f;
			float scale = Mathf.Min(dstW / _icon.GetWidth(), dstH / _icon.GetHeight());
			float iW    = _icon.GetWidth()  * scale;
			float iH    = _icon.GetHeight() * scale;
			float offX  = pad + (dstW - iW) / 2f;
			float offY  = pad + (dstH - iH) / 2f;
			DrawTextureRectRegion(_icon,
				new Rect2(offX, offY, iW, iH),
				new Rect2(0, 0, _icon.GetWidth(), _icon.GetHeight()),
				Colors.White);
		}
	}
}
