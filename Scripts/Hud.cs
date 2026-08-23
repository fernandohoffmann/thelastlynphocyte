using Godot;

public partial class Hud : CanvasLayer
{
	private Label       _levelLabel;
	private Label       _hpLabel;
	private ProgressBar _hpBar;
	private Label       _timerLabel;
	private ProgressBar _xpBar;
	private Label       _dnaLabel;

	public override void _Ready()
	{
		Layer = 5;

		var root = new Control();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		// ── Barra de XP no topo ──────────────────────────────────
		_xpBar = new ProgressBar
		{
			MinValue       = 0,
			MaxValue       = 100,
			Value          = 0,
			ShowPercentage = false
		};
		_xpBar.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		_xpBar.OffsetTop    = 0f;
		_xpBar.OffsetBottom = 10f;
		root.AddChild(_xpBar);

		// ── Timer no centro do topo ───────────────────────────────
		_timerLabel = new Label();
		_timerLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		_timerLabel.OffsetTop           = 16f;
		_timerLabel.OffsetBottom        = 44f;
		_timerLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_timerLabel.Text                = "20:00";
		_timerLabel.AddThemeFontSizeOverride("font_size", 16);
		root.AddChild(_timerLabel);

		// ── Canto superior esquerdo: Nível + HP ──────────────────
		var leftVBox = new VBoxContainer();
		leftVBox.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		leftVBox.OffsetLeft   = 12f;
		leftVBox.OffsetTop    = 16f;
		leftVBox.OffsetRight  = 260f;
		leftVBox.OffsetBottom = 120f;
		leftVBox.AddThemeConstantOverride("separation", 4);
		root.AddChild(leftVBox);

		_levelLabel = MakeLabel("Nivel 1", leftVBox, 10);
		_hpLabel    = MakeLabel("HP: 20 / 20", leftVBox, 10);

		_hpBar = new ProgressBar
		{
			MinValue       = 0,
			MaxValue       = 20,
			Value          = 20,
			ShowPercentage = false
		};
		_hpBar.CustomMinimumSize = new Vector2(200, 14);
		leftVBox.AddChild(_hpBar);

		// ── DNA — abaixo do minimapa ──────────────────────────────
		_dnaLabel = new Label();
		_dnaLabel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		_dnaLabel.OffsetLeft   = -220f;
		_dnaLabel.OffsetTop    = 228f;
		_dnaLabel.OffsetRight  = -10f;
		_dnaLabel.OffsetBottom = 250f;
		_dnaLabel.HorizontalAlignment = HorizontalAlignment.Right;
		_dnaLabel.Text = "DNA: 0";
		_dnaLabel.AddThemeFontSizeOverride("font_size", 10);
		root.AddChild(_dnaLabel);

		CallDeferred(MethodName.ConnectSignals);
	}

	private void ConnectSignals()
	{
		var player = GetTree().GetFirstNodeInGroup("Player") as Player;
		if (player == null) { CallDeferred(MethodName.ConnectSignals); return; }

		player.HpChanged += OnHpChanged;
		player.XpChanged += OnXpChanged;
		player.LevelUp   += OnLevelUp;

		if (GameManager.Instance != null)
			GameManager.Instance.DnaChanged += OnDnaChanged;

		_dnaLabel.Text = $"DNA: {SaveData.TotalDna}";
	}

	public override void _Process(double delta)
	{
		if (GameManager.Instance == null) return;

		if (Player.Instance != null)
		{
			_hpBar.MaxValue = Player.Instance.MaxHp;
			_hpBar.Value    = Player.Instance.CurrentHp;
			_hpLabel.Text   = $"HP: {Player.Instance.CurrentHp} / {Player.Instance.MaxHp}";
			_xpBar.MaxValue = Player.Instance.XpForNext;
			_xpBar.Value    = Player.Instance.CurrentXp;
		}

		float rem = GameManager.Instance.GetRemainingTime();
		int   min = (int)(rem / 60);
		int   sec = (int)(rem % 60);
		_timerLabel.Text = $"{min:00}:{sec:00}";
	}

	private void OnHpChanged(int cur, int max)
	{
		_hpLabel.Text   = $"HP: {cur} / {max}";
		_hpBar.MaxValue = max;
		_hpBar.Value    = cur;
	}

	private void OnXpChanged(int cur, int req)
	{
		_xpBar.MaxValue = req;
		_xpBar.Value    = cur;
	}

	private void OnLevelUp(int lvl) => _levelLabel.Text = $"Nivel {lvl}";

	private void OnDnaChanged(int total) => _dnaLabel.Text = $"DNA: {total}";

	private static Label MakeLabel(string text, Control parent, int fontSize = 12)
	{
		var lbl = new Label { Text = text };
		lbl.AddThemeFontSizeOverride("font_size", fontSize);
		parent.AddChild(lbl);
		return lbl;
	}
}
