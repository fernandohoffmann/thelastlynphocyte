using Godot;

public partial class Hud : CanvasLayer
{
	private Label       _levelLabel;
	private Label       _hpLabel;
	private ProgressBar _hpBar;
	private Label       _timerLabel;
	private ProgressBar _xpBar;
	private Label       _dnaLabel;

	private Label       _missionTitleLabel;
	private Label       _missionProgressLabel;

	private ProgressBar _bossBar;
	private Label       _bossLabel;

	public override void _Ready()
	{
		Layer = 5;

		var root = new Control();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		_xpBar = new ProgressBar { MinValue = 0, MaxValue = 100, Value = 0, ShowPercentage = false };
		_xpBar.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		_xpBar.OffsetTop    = 0f;
		_xpBar.OffsetBottom = 10f;
		root.AddChild(_xpBar);

		_timerLabel = new Label();
		_timerLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		_timerLabel.OffsetTop           = 16f;
		_timerLabel.OffsetBottom        = 44f;
		_timerLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_timerLabel.Text                = "20:00";
		_timerLabel.AddThemeFontSizeOverride("font_size", 16);
		root.AddChild(_timerLabel);

		// ── Barra de vida do boss — topo, some quando não há boss ───
		_bossLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center, Text = "DOENCA DE CHAGAS" };
		_bossLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		_bossLabel.OffsetTop    = 50f;
		_bossLabel.OffsetBottom = 66f;
		_bossLabel.AddThemeFontSizeOverride("font_size", 10);
		_bossLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.3f, 0.9f));
		_bossLabel.Visible = false;
		root.AddChild(_bossLabel);

		_bossBar = new ProgressBar { MinValue = 0, MaxValue = 1, Value = 1, ShowPercentage = false };
		_bossBar.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		_bossBar.OffsetLeft   = 300f;
		_bossBar.OffsetRight  = -300f;
		_bossBar.OffsetTop    = 68f;
		_bossBar.OffsetBottom = 80f;
		_bossBar.Visible = false;
		root.AddChild(_bossBar);

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

		_hpBar = new ProgressBar { MinValue = 0, MaxValue = 20, Value = 20, ShowPercentage = false };
		_hpBar.CustomMinimumSize = new Vector2(200, 14);
		leftVBox.AddChild(_hpBar);

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

		// ── Painel de missão — logo abaixo do DNA / minimapa ────────
		_missionTitleLabel = new Label();
		_missionTitleLabel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		_missionTitleLabel.OffsetLeft   = -260f;
		_missionTitleLabel.OffsetTop    = 254f;
		_missionTitleLabel.OffsetRight  = -10f;
		_missionTitleLabel.OffsetBottom = 296f; // altura maior — cabe até 2 linhas
		_missionTitleLabel.HorizontalAlignment = HorizontalAlignment.Right;
		_missionTitleLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_missionTitleLabel.ClipText     = false;
		_missionTitleLabel.Text = "Missao: ...";
		_missionTitleLabel.AddThemeFontSizeOverride("font_size", 10);
		_missionTitleLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.4f));
		root.AddChild(_missionTitleLabel);

		_missionProgressLabel = new Label();
		_missionProgressLabel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		_missionProgressLabel.OffsetLeft   = -260f;
		_missionProgressLabel.OffsetTop    = 298f;
		_missionProgressLabel.OffsetRight  = -10f;
		_missionProgressLabel.OffsetBottom = 330f;
		_missionProgressLabel.HorizontalAlignment = HorizontalAlignment.Right;
		_missionProgressLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_missionProgressLabel.ClipText     = false;
		_missionProgressLabel.Text = "";
		_missionProgressLabel.AddThemeFontSizeOverride("font_size", 10);
		root.AddChild(_missionProgressLabel);

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

		if (MissionManager.Instance != null)
			MissionManager.Instance.MissionChanged += OnMissionChanged;
		else
			CallDeferred(MethodName.ConnectMissionSignal);
	}

	private void ConnectMissionSignal()
	{
		if (MissionManager.Instance == null) { CallDeferred(MethodName.ConnectMissionSignal); return; }
		MissionManager.Instance.MissionChanged += OnMissionChanged;
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

		// Barra do boss — só aparece se ele existir
		if (ChagasBoss.Instance != null && IsInstanceValid(ChagasBoss.Instance))
		{
			_bossBar.Visible   = true;
			_bossLabel.Visible = true;
			_bossBar.MaxValue  = ChagasBoss.Instance.MaxHp;
			_bossBar.Value     = ChagasBoss.Instance.Hp;
		}
		else
		{
			_bossBar.Visible   = false;
			_bossLabel.Visible = false;
		}
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

	private void OnMissionChanged(string title, string progressText)
	{
		_missionTitleLabel.Text    = title;
		_missionProgressLabel.Text = progressText;
	}

	private static Label MakeLabel(string text, Control parent, int fontSize = 12)
	{
		var lbl = new Label { Text = text };
		lbl.AddThemeFontSizeOverride("font_size", fontSize);
		parent.AddChild(lbl);
		return lbl;
	}
}
