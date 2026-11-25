// TutorialOverlay.cs — tutorial da PRIMEIRA partida.
//
// Funciona como em jogos que "congelam" o tempo pra ensinar um botao:
// o jogo entra em camera lenta, aparece a tecla da vez (W, depois D, S, A),
// e quando o jogador aperta a tecla o tempo volta ao normal por um instante,
// pra ele ver o personagem andar, e entao vem a proxima tecla.
//
// So aparece uma vez: ao terminar (ou pular com BACKSPACE) grava o flag em
// SaveData.TutorialGameDone e nunca mais e criado.
using Godot;
using System.Collections.Generic;

public partial class TutorialOverlay : CanvasLayer
{
	// Quem cria (GameScene) entrega a tela de upgrade, pra ensinar a escolher.
	public UpgradeScreen UpgradeScreenRef;

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	private const float SLOW_SCALE    = 0.10f;  // 1 = normal, 0.10 = 10x mais lento
	private const float SLOW_IN_RATE  = 7f;     // velocidade de entrar na camera lenta
	private const float SLOW_OUT_RATE = 14f;    // velocidade de voltar ao normal
	private const float AUDIO_SLOW    = 0.65f;  // o som tambem desacelera (1 = nao mexe)
	private const float ARM_DELAY     = 0.45f;  // segundos reais antes de aceitar a tecla
	private const float MOVE_TIME     = 1.10f;  // segundos reais em velocidade normal apos acertar
	private const float TOAST_TIME    = 2.40f;  // "boa sorte" final
	private const float TINT_ALPHA    = 0.30f;  // escurecida azulada na camera lenta
	// ──────────────────────────────────────────────────────────────────────

	private enum StepKind { ChooseItem, Key, Info, Toast }

	private class Step
	{
		public StepKind Kind;
		public string   Action;   // acao do InputMap (Key)
		public string   Cap;      // texto desenhado na tecla (Key)
		public string   Title;
		public string   Body;
		public Vector2  Dir;      // seta apontando pra onde andar (Key)
	}

	private TutorialView _view;
	private ColorRect    _tint;
	private List<Step>   _steps;
	private int          _index = -1;

	private ulong _lastTicks;
	private float _stepTime;       // segundos reais desde que o passo comecou
	private float _armTimer;       // idem, mas so conta fora da pausa
	private float _successTimer;
	private bool  _success;
	private bool  _seenUpgrade;
	private bool  _prevConfirm = true;   // true: o clique que abriu o passo nao conta
	private float _slow;                 // 0 = normal, 1 = camera lenta total
	private float _slowTarget;
	private bool  _finished;

	public override void _Ready()
	{
		Layer       = 16;   // acima da tela de upgrade (15) e do HUD (5)
		ProcessMode = ProcessModeEnum.Always;

		// leve tom azulado durante a camera lenta
		_tint = new ColorRect { Color = new Color(0.02f, 0.06f, 0.16f, 0f), MouseFilter = Control.MouseFilterEnum.Ignore };
		_tint.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_tint);

		_view = new TutorialView { MouseFilter = Control.MouseFilterEnum.Ignore };
		_view.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_view);

		_steps = new List<Step>
		{
			new Step { Kind = StepKind.ChooseItem,
				Title = "ESCOLHA SUA PRIMEIRA ARMA",
				Body  = "Clique em uma carta para equipar. A cada nivel que voce subir, escolhe uma nova melhoria." },

			new Step { Kind = StepKind.Key, Action = "move_up",    Cap = "W", Dir = Vector2.Up,
				Title = "ANDAR PARA CIMA",     Body = "Aperte W (ou a seta para cima)." },
			new Step { Kind = StepKind.Key, Action = "move_right", Cap = "D", Dir = Vector2.Right,
				Title = "ANDAR PARA A DIREITA", Body = "Aperte D (ou a seta para a direita)." },
			new Step { Kind = StepKind.Key, Action = "move_down",  Cap = "S", Dir = Vector2.Down,
				Title = "ANDAR PARA BAIXO",    Body = "Aperte S (ou a seta para baixo)." },
			new Step { Kind = StepKind.Key, Action = "move_left",  Cap = "A", Dir = Vector2.Left,
				Title = "ANDAR PARA A ESQUERDA", Body = "Aperte A (ou a seta para a esquerda)." },

			new Step { Kind = StepKind.Info,
				Title = "ATAQUE AUTOMATICO",
				Body  = "Suas armas atacam sozinhas. Seu trabalho e se mover e desviar dos inimigos." },
			new Step { Kind = StepKind.Info,
				Title = "SUBA DE NIVEL",
				Body  = "Derrote inimigos e colete o XP que eles deixam. Ao subir de nivel, clique em uma carta para escolher uma melhoria." },
			new Step { Kind = StepKind.Info,
				Title = "MISSOES",
				Body  = "A missao atual fica no canto direito, abaixo do minimapa. Segure TAB para ver o mapa grande. Cumpra todas para vencer a fase." },

			new Step { Kind = StepKind.Toast, Title = "BOA SORTE, LINFOCITO!" },
		};

		_lastTicks = Time.GetTicksUsec();
		NextStep();
	}

	public override void _ExitTree()
	{
		// nunca deixa o jogo preso em camera lenta (menu, game over, troca de cena)
		RestoreTime();
	}

	private static void RestoreTime()
	{
		Engine.TimeScale = 1.0;
		AudioServer.PlaybackSpeedScale = 1.0f;
	}

	// ── passos ────────────────────────────────────────────────────────────
	private void NextStep()
	{
		_index++;
		if (_index >= _steps.Count) { Finish(true); return; }

		_stepTime     = 0f;
		_armTimer     = 0f;
		_successTimer = 0f;
		_success      = false;
		_seenUpgrade  = false;
		_prevConfirm  = true;

		var s = _steps[_index];
		_view.BeginStep(s.Kind.ToString(), s.Title, s.Body, s.Cap, s.Dir);
	}

	private void Finish(bool markDone)
	{
		if (_finished) return;
		_finished = true;

		if (markDone) SaveData.SetTutorialGameDone();
		RestoreTime();
		QueueFree();
	}

	// ── loop ──────────────────────────────────────────────────────────────
	public override void _Process(double delta)
	{
		if (_finished) return;

		// Engine.TimeScale encolhe o 'delta', entao o tempo do tutorial vem
		// do relogio real. Senao a propria camera lenta o deixaria lento.
		ulong now = Time.GetTicksUsec();
		float dt  = Mathf.Min((now - _lastTicks) / 1_000_000f, 0.1f);
		_lastTicks = now;

		var gm = GameManager.Instance;
		if (gm == null || !gm.IsGameRunning)
		{
			Finish(false);   // morreu / saiu: o tutorial volta na proxima vez
			return;
		}

		// pular
		if (Input.IsKeyPressed(Key.Backspace))
		{
			Finish(true);
			return;
		}

		var  step   = _steps[_index];
		bool paused = GetTree().Paused;
		_stepTime  += dt;

		bool confirm = Input.IsKeyPressed(Key.Space) || Input.IsKeyPressed(Key.Enter)
					|| Input.IsMouseButtonPressed(MouseButton.Left);
		bool confirmEdge = confirm && !_prevConfirm;
		_prevConfirm = confirm;

		_slowTarget = 0f;
		bool visible = true;

		switch (step.Kind)
		{
			case StepKind.ChooseItem:
			{
				bool up = UpgradeScreenRef != null && IsInstanceValid(UpgradeScreenRef) && UpgradeScreenRef.Visible;
				if (up) _seenUpgrade = true;

				visible = up;
				if (_seenUpgrade && !up)        NextStep();   // escolheu
				else if (!_seenUpgrade && _stepTime > 1.0f) NextStep();   // nem apareceu: pula
				break;
			}

			case StepKind.Key:
			{
				// level-up no meio do tutorial: esconde e espera voltar
				if (paused) { visible = false; break; }

				_armTimer += dt;
				if (!_success)
				{
					_slowTarget = 1f;
					if (_armTimer >= ARM_DELAY && Input.IsActionPressed(step.Action))
					{
						_success = true;
						_successTimer = 0f;
						AudioManager.Instance?.PlayUiClick();
					}
				}
				else
				{
					_successTimer += dt;       // tempo normal pra ver o personagem andar
					if (_successTimer >= MOVE_TIME) NextStep();
				}
				break;
			}

			case StepKind.Info:
			{
				if (paused) { visible = false; break; }

				_armTimer  += dt;
				_slowTarget = 1f;
				if (_armTimer > 0.35f && confirmEdge)
				{
					AudioManager.Instance?.PlayUiClick();
					NextStep();
				}
				break;
			}

			case StepKind.Toast:
			{
				if (_stepTime >= TOAST_TIME) Finish(true);
				break;
			}
		}

		if (_finished) return;

		// camera lenta suave (entra e sai com rampa, sem tranco)
		float rate = _slowTarget > _slow ? SLOW_IN_RATE : SLOW_OUT_RATE;
		_slow = Mathf.MoveToward(_slow, _slowTarget, rate * dt);
		Engine.TimeScale = Mathf.Lerp(1.0f, SLOW_SCALE, _slow);
		AudioServer.PlaybackSpeedScale = Mathf.Lerp(1.0f, AUDIO_SLOW, _slow);

		_tint.Color = new Color(0.02f, 0.06f, 0.16f, TINT_ALPHA * _slow);
		_view.Tick(dt, visible, _slow, _success ? Mathf.Clamp(_successTimer / MOVE_TIME, 0f, 1f) : -1f);
	}
}

// ─────────────────────────────────────────────────────────────────────────
// Desenho do tutorial. Tudo por _Draw (sem containers): posicao e tamanho
// ficam previsiveis em qualquer resolucao.
// ─────────────────────────────────────────────────────────────────────────
public partial class TutorialView : Control
{
	private string  _kind = "";
	private string  _title = "", _body = "", _cap = "";
	private Vector2 _dir;

	private float _alpha;        // fade do painel
	private float _slow;
	private float _success = -1f; // -1 = ainda nao acertou; 0..1 = progresso do "tempo normal"
	private float _time;         // relogio real (pulsos)
	private bool  _visible = true;

	private StyleBoxFlat _panel;
	private StyleBoxFlat _capNormal;
	private StyleBoxFlat _capOk;

	public override void _Ready()
	{
		_panel     = MenuStyle.PanelFrame(24);
		_capNormal = MenuStyle.Frame(MenuStyle.FILL_ON, MenuStyle.LINE_ON, 3, 16);
		_capOk     = MenuStyle.Frame(new Color(0.06f, 0.30f, 0.14f, 0.95f), new Color(0.40f, 1.00f, 0.55f), 3, 16);
	}

	public void BeginStep(string kind, string title, string body, string cap, Vector2 dir)
	{
		_kind = kind; _title = title; _body = body ?? ""; _cap = cap ?? ""; _dir = dir;
		_alpha   = 0f;
		_success = -1f;
	}

	public void Tick(float dt, bool visible, float slow, float success)
	{
		_time   += dt;
		_slow    = slow;
		_visible = visible;
		_success = success;
		_alpha   = Mathf.MoveToward(_alpha, visible ? 1f : 0f, dt * 5f);
		Modulate = new Color(1, 1, 1, _alpha);
		QueueRedraw();
	}

	private Font GetFont() => GetThemeDefaultFont() ?? ThemeDB.FallbackFont;

	// quebra por palavras; a fonte e monoespacada, entao largura = chars * tamanho
	private static List<string> Wrap(string text, int maxChars)
	{
		var lines = new List<string>();
		var cur   = "";
		foreach (var word in text.Split(' '))
		{
			if (cur.Length == 0)                       cur = word;
			else if (cur.Length + 1 + word.Length <= maxChars) cur += " " + word;
			else { lines.Add(cur); cur = word; }
		}
		if (cur.Length > 0) lines.Add(cur);
		return lines;
	}

	private void Txt(string text, Vector2 pos, int size, Color col, float width = -1f,
					  HorizontalAlignment align = HorizontalAlignment.Left)
	{
		DrawString(GetFont(), pos, text, align, width, size, col);
	}

	public override void _Draw()
	{
		var vp = Size;

		if (_alpha <= 0.01f) return;

		switch (_kind)
		{
			case "ChooseItem": DrawChoose(vp); break;
			case "Key":        DrawKey(vp);    break;
			case "Info":       DrawInfo(vp);   break;
			case "Toast":      DrawToast(vp);  break;
		}

		// aviso de pular, discreto, embaixo
		if (_kind != "Toast")
			Txt("[BACKSPACE] PULAR TUTORIAL", new Vector2(0, vp.Y - 26), 8,
				new Color(MenuStyle.TEXT_DIM.R, MenuStyle.TEXT_DIM.G, MenuStyle.TEXT_DIM.B, 0.85f),
				vp.X, HorizontalAlignment.Center);
	}

	// ── passo: escolher item (fica abaixo das cartas) ────────────────────
	private void DrawChoose(Vector2 vp)
	{
		float w = 780f, h = 170f;
		var   r = new Rect2((vp.X - w) * 0.5f, vp.Y * 0.5f + 190f, w, h);
		DrawStyleBox(_panel, r);

		// seta balancando, apontando pras cartas
		float bob = Mathf.Sin(_time * 5f) * 8f;
		DrawArrow(new Vector2(vp.X * 0.5f, r.Position.Y - 18f + bob), Vector2.Up, 22f, MenuStyle.ACCENT);

		Txt(_title, new Vector2(r.Position.X, r.Position.Y + 56f), MenuStyle.FONT_TITLE,
			MenuStyle.ACCENT, w, HorizontalAlignment.Center);

		DrawBody(_body, r.Position.X + 28f, r.Position.Y + 96f, w - 56f, 16, MenuStyle.TEXT);
	}

	// ── passo: apertar uma tecla ─────────────────────────────────────────
	private void DrawKey(Vector2 vp)
	{
		float w = 760f, h = 210f;
		var   r = new Rect2((vp.X - w) * 0.5f, 96f, w, h);
		DrawStyleBox(_panel, r);

		bool ok = _success >= 0f;

		// tecla
		float pulse = ok ? 1.10f : 1f + Mathf.Sin(_time * 6f) * 0.05f;
		float cs    = 124f * pulse;
		var   cc    = new Vector2(r.Position.X + 40f + 62f, r.Position.Y + h * 0.5f);
		var   capR  = new Rect2(cc - new Vector2(cs, cs) * 0.5f, new Vector2(cs, cs));

		DrawStyleBox(ok ? _capOk : _capNormal, capR);
		Txt(ok ? "OK" : _cap, new Vector2(capR.Position.X, cc.Y + (ok ? 12f : 19f)), ok ? 32 : 56,
			ok ? new Color(0.55f, 1f, 0.65f) : Colors.White, cs, HorizontalAlignment.Center);

		// texto
		float tx = r.Position.X + 40f + 124f + 36f;
		float tw = r.End.X - 30f - tx;

		Txt(ok ? "MUITO BEM!" : _title, new Vector2(tx, r.Position.Y + 92f), MenuStyle.FONT_TITLE,
			ok ? new Color(0.55f, 1f, 0.65f) : MenuStyle.ACCENT);

		if (!ok)
			DrawBody(_body, tx, r.Position.Y + 134f, tw, 16, MenuStyle.TEXT);
		else
			DrawBody("Continue andando!", tx, r.Position.Y + 134f, tw, 16, MenuStyle.TEXT);

		// setas em volta do personagem (centro da tela), so enquanto espera
		if (!ok && _dir != Vector2.Zero)
		{
			var center = vp * 0.5f;
			for (int i = 0; i < 3; i++)
			{
				float ph = Mathf.PosMod(_time * 1.6f - i * 0.28f, 1f);
				float a  = Mathf.Sin(ph * Mathf.Pi);
				DrawArrow(center + _dir * (70f + ph * 90f), _dir, 20f,
					new Color(MenuStyle.ACCENT.R, MenuStyle.ACCENT.G, MenuStyle.ACCENT.B, a));
			}
		}
	}

	// ── passo: informacao + "continuar" ──────────────────────────────────
	private void DrawInfo(Vector2 vp)
	{
		float w = 900f, h = 290f;
		var   r = new Rect2((vp.X - w) * 0.5f, 96f, w, h);
		DrawStyleBox(_panel, r);

		Txt(_title, new Vector2(r.Position.X, r.Position.Y + 66f), MenuStyle.FONT_TITLE,
			MenuStyle.ACCENT, w, HorizontalAlignment.Center);

		DrawBody(_body, r.Position.X + 40f, r.Position.Y + 112f, w - 80f, 16, MenuStyle.TEXT);

		// "continuar" piscando
		float blink = 0.55f + 0.45f * Mathf.Sin(_time * 5f);
		Txt("[ESPACO] CONTINUAR", new Vector2(r.Position.X, r.End.Y - 36f), 16,
			new Color(1f, 0.92f, 0.55f, blink), w, HorizontalAlignment.Center);
	}

	private void DrawToast(Vector2 vp)
	{
		float w = 700f, h = 110f;
		var   r = new Rect2((vp.X - w) * 0.5f, 120f, w, h);
		DrawStyleBox(_panel, r);
		Txt(_title, new Vector2(r.Position.X, r.Position.Y + 68f), MenuStyle.FONT_TITLE,
			new Color(0.55f, 1f, 0.65f), w, HorizontalAlignment.Center);
	}

	// ── pecas ─────────────────────────────────────────────────────────────
	private void DrawBody(string text, float x, float y, float width, int size, Color col)
	{
		int maxChars = Mathf.Max(8, (int)(width / size));
		var lines    = Wrap(text, maxChars);
		for (int i = 0; i < lines.Count; i++)
			Txt(lines[i], new Vector2(x, y + i * (size + 10)), size, col);
	}

	private void DrawArrow(Vector2 tip, Vector2 dir, float size, Color col)
	{
		var d = dir.Normalized();
		var n = new Vector2(-d.Y, d.X);
		var pts = new[]
		{
			tip,
			tip - d * size + n * size * 0.75f,
			tip - d * size * 0.45f,
			tip - d * size - n * size * 0.75f,
		};
		DrawColoredPolygon(pts, col);
	}
}
