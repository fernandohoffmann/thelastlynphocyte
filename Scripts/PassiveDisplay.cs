using Godot;
using System.Collections.Generic;

public partial class PassiveDisplay : CanvasLayer
{
	private HBoxContainer  _container;
	private PanelContainer _tooltip;
	private Label          _ttName;
	private Label          _ttDesc;

	private struct PassiveSlot
	{
		public PassiveSlotControl Control;
		public string             Name;
		public int                Level;
		public UpgradeId          Id;
	}

	private List<PassiveSlot> _slots = new();

	public override void _Ready()
	{
		Layer = 6;

		var root = new Control();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		_container = new HBoxContainer();
		_container.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
		_container.OffsetLeft   =  10f;
		_container.OffsetTop    = -80f;
		_container.OffsetRight  =  500f;
		_container.OffsetBottom = -10f;
		_container.AddThemeConstantOverride("separation", 6);
		root.AddChild(_container);

		_tooltip = new PanelContainer();
		_tooltip.Visible = false;
		_tooltip.ZIndex  = 30;

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
		ttVbox.CustomMinimumSize = new Vector2(200, 0);
		_tooltip.AddChild(ttVbox);

		_ttName = new Label();
		_ttName.AddThemeFontSizeOverride("font_size", 10);
		_ttName.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
		ttVbox.AddChild(_ttName);

		_ttDesc = new Label { AutowrapMode = TextServer.AutowrapMode.Word, CustomMinimumSize = new Vector2(200, 0) };
		_ttDesc.AddThemeFontSizeOverride("font_size", 8);
		ttVbox.AddChild(_ttDesc);

		CallDeferred(MethodName.PopulateFromLoadout);
	}

	private void PopulateFromLoadout()
	{
		var um = UpgradeManager.Instance;
		if (um == null) return;
		foreach (var id in um.EquippedPassives)
		{
			var opt = um.GetUpgrade(id);
			if (opt == null) continue;
			RegisterPassive(opt.Name, opt.Description, id);
		}
	}

	public void RegisterPassive(string name, string description, UpgradeId id)
	{
		for (int i = 0; i < _slots.Count; i++)
		{
			if (_slots[i].Name == name)
			{
				var s = _slots[i];
				s.Level++;
				_slots[i] = s;
				s.Control.SetLevel(s.Level);
				return;
			}
		}

		var ctrl = new PassiveSlotControl();
		ctrl.Setup(id, name,
			() => ShowTooltip(name, description, ctrl),
			() => _tooltip.Visible = false);
		_container.AddChild(ctrl);

		_slots.Add(new PassiveSlot { Control = ctrl, Name = name, Level = 1, Id = id });
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
		_tooltip.Position = anchor.GlobalPosition + new Vector2(0, -_tooltip.Size.Y - 6f);
	}
}

public partial class PassiveSlotControl : Control
{
	private Texture2D _icon;
	private string    _name;
	private int        _level = 1;

	public void Setup(UpgradeId id, string name, System.Action onEnter, System.Action onExit)
	{
		_icon             = IconAtlas.Get(id);
		_name             = name.Length > 6 ? name.Substring(0, 6) : name;
		CustomMinimumSize = new Vector2(58, 68);
		MouseFilter       = MouseFilterEnum.Stop;
		MouseEntered     += () => onEnter();
		MouseExited      += () => onExit();
	}

	public void SetLevel(int level) { _level = level; QueueRedraw(); }

	public override void _Draw()
	{
		float w = Size.X;
		float h = Size.Y - 12f;

		DrawRect(new Rect2(0, 0, w, h), new Color(0.05f, 0.25f, 0.10f, 0.85f));
		DrawRect(new Rect2(0, 0, w, h), new Color(0.2f, 0.7f, 0.3f, 0.5f), false, 1.5f);

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

		DrawString(ThemeDB.FallbackFont,
			new Vector2(0f, h + 10f),
			$"Nv.{_level}",
			HorizontalAlignment.Center,
			(int)w, 7,
			new Color(0.85f, 0.85f, 0.85f));
	}
}
