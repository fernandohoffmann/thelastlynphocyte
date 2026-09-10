// Minimap.cs — completo, revelação em círculos + marcador de colônias
using Godot;
using System.Collections.Generic;

public partial class Minimap : CanvasLayer
{
	private const float MAP_W    = 200f;
	private const float MAP_H    = 200f;
	private const float MARGIN   = 10f;
	private const float BIG_W    = 700f;
	private const float BIG_H    = 700f;
	private const float REVEAL_R = 500f;
	private const float POINT_SPACING = 60f;
	private const int   MAX_POINTS    = 4000;

	private List<Vector2> _visitedPoints = new List<Vector2>();
	private Vector2 _lastRecordedPos;
	private bool    _hasLastRecorded = false;

	private Rect2 _playableArea;
	private bool  _areaInitialized = false;

	private MinimapDrawer _smallDrawer;
	private MinimapDrawer _bigDrawer;

	public Vector2 SmoothedPlayerWorld { get; private set; }
	private bool _smoothInit = false;

	private bool  _bigDirty   = true;
	private float _dirtyTimer = 0f;
	private const float DIRTY_INTERVAL = 0.1f;

	public override void _Ready()
	{
		Layer       = 7;
		ProcessMode = ProcessModeEnum.Always;

		CallDeferred(MethodName.SubscribeToGameManager);

		var root = new Control();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(root);

		_smallDrawer = new MinimapDrawer();
		_smallDrawer.Init(this, MAP_W, MAP_H, false);
		_smallDrawer.ClipContents = true;
		_smallDrawer.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		_smallDrawer.OffsetLeft   = -(MAP_W + MARGIN);
		_smallDrawer.OffsetTop    = MARGIN + 14f;
		_smallDrawer.OffsetRight  = -MARGIN;
		_smallDrawer.OffsetBottom = MAP_H + MARGIN + 14f;
		root.AddChild(_smallDrawer);

		_bigDrawer = new MinimapDrawer();
		_bigDrawer.Init(this, BIG_W, BIG_H, true);
		_bigDrawer.ClipContents = true;
		_bigDrawer.SetAnchorsPreset(Control.LayoutPreset.Center);
		_bigDrawer.OffsetLeft   = -BIG_W / 2f;
		_bigDrawer.OffsetTop    = -BIG_H / 2f;
		_bigDrawer.OffsetRight  =  BIG_W / 2f;
		_bigDrawer.OffsetBottom =  BIG_H / 2f;
		_bigDrawer.Visible = false;
		root.AddChild(_bigDrawer);
	}

	private void SubscribeToGameManager()
	{
		if (GameManager.Instance == null)
		{
			CallDeferred(MethodName.SubscribeToGameManager);
			return;
		}

		GameManager.Instance.PlayableAreaChanged += InitArea;
		InitArea();
	}

	public override void _ExitTree()
	{
		if (GameManager.Instance != null)
			GameManager.Instance.PlayableAreaChanged -= InitArea;
	}

	private void InitArea()
	{
		if (GameManager.Instance == null) return;

		_playableArea    = GameManager.Instance.PlayableArea;
		_areaInitialized = true;
	}

	public override void _Process(double delta)
	{
		if (Player.Instance != null)
		{
			Vector2 target = Player.Instance.GlobalPosition;

			if (!_smoothInit)
			{
				SmoothedPlayerWorld = target;
				_smoothInit = true;
			}
			else
			{
				SmoothedPlayerWorld = SmoothedPlayerWorld.Lerp(target, 20f * (float)delta);
			}

			if (_areaInitialized)
			{
				bool added = TryRecordPoint(target);
				if (added) _bigDirty = true;
			}
		}

		bool tab = Input.IsKeyPressed(Key.Tab);

		_smallDrawer.QueueRedraw();

		if (tab)
		{
			_bigDrawer.Visible = true;
			_dirtyTimer += (float)delta;

			if (_bigDirty && _dirtyTimer >= DIRTY_INTERVAL)
			{
				_bigDirty   = false;
				_dirtyTimer = 0f;
				_bigDrawer.QueueRedraw();
			}
			_bigDrawer.QueueRedraw();
		}
		else
		{
			_bigDrawer.Visible = false;
		}
	}

	private bool TryRecordPoint(Vector2 worldPos)
	{
		if (_visitedPoints.Count >= MAX_POINTS) return false;

		if (!_hasLastRecorded || worldPos.DistanceTo(_lastRecordedPos) >= POINT_SPACING)
		{
			_visitedPoints.Add(worldPos);
			_lastRecordedPos = worldPos;
			_hasLastRecorded = true;
			return true;
		}

		return false;
	}

	public Vector2 WorldToMapSmooth(float w, float h)
	{
		if (!_areaInitialized) return new Vector2(w / 2f, h / 2f);
		return WorldToMap(SmoothedPlayerWorld, w, h);
	}

	public Vector2 WorldToMap(Vector2 worldPos, float w, float h)
	{
		if (!_areaInitialized || _playableArea.Size.X <= 0f || _playableArea.Size.Y <= 0f)
			return Vector2.Zero;

		float nx = (worldPos.X - _playableArea.Position.X) / _playableArea.Size.X;
		float ny = (worldPos.Y - _playableArea.Position.Y) / _playableArea.Size.Y;
		return new Vector2(Mathf.Clamp(nx * w, 0, w), Mathf.Clamp(ny * h, 0, h));
	}

	public bool           AreaInitialized  => _areaInitialized;
	public Rect2           PlayableAreaRect => _playableArea;
	public List<Vector2>   VisitedPoints    => _visitedPoints;
	public float           RevealRadius     => REVEAL_R;
}

public partial class MinimapDrawer : Control
{
	private static readonly Color BG_COLOR           = new Color(0.28f, 0.05f, 0.08f);
	private static readonly Color TILE_REVEALED      = new Color(0.75f, 0.25f, 0.30f);
	private static readonly Color BORDER_COLOR       = new Color(0.6f,  0.6f,  0.6f, 0.8f);
	private static readonly Color PLAYER_COLOR       = new Color(0.3f,  0.7f,  1f);
	private static readonly Color COLONY_MARKER_COLOR = new Color(1f,   0.15f, 0.15f);

	private Minimap _minimap;
	private float   _w, _h;
	private bool    _isBig;

	public void Init(Minimap minimap, float w, float h, bool isBig)
	{
		_minimap    = minimap;
		_w = w; _h  = h;
		_isBig      = isBig;
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Draw()
	{
		if (_minimap == null) return;

		DrawRect(new Rect2(0, 0, _w, _h), BG_COLOR);

		if (_minimap.AreaInitialized)
		{
			if (_isBig)
				DrawFogBig();
			else
				DrawFogZoomed();
		}

		DrawColonyMarkers();

		if (Player.Instance != null)
		{
			Vector2 p    = _isBig ? _minimap.WorldToMapSmooth(_w, _h) : new Vector2(_w / 2f, _h / 2f);
			float   size = _isBig ? 5f : 4f;
			DrawCircle(p, size * 2.5f, new Color(PLAYER_COLOR, 0.2f));
			DrawCircle(p, size, PLAYER_COLOR);
		}

		float b = 3f;
		DrawRect(new Rect2(0,    0,    _w, b),  BG_COLOR);
		DrawRect(new Rect2(0,    _h-b, _w, b),  BG_COLOR);
		DrawRect(new Rect2(0,    0,    b,  _h), BG_COLOR);
		DrawRect(new Rect2(_w-b, 0,    b,  _h), BG_COLOR);

		DrawRect(new Rect2(0, 0, _w, _h), BORDER_COLOR, false, 2f);

		if (!_isBig)
			DrawString(ThemeDB.FallbackFont,
				new Vector2(4, _h - 4), "[TAB] Mapa",
				HorizontalAlignment.Left, -1, 9,
				new Color(0.7f, 0.7f, 0.7f, 0.7f));
	}

	// Mapa grande: cada ponto visitado vira um círculo na posição normalizada
	private void DrawFogBig()
	{
		var area = _minimap.PlayableAreaRect;
		if (area.Size.X <= 0f || area.Size.Y <= 0f) return;

		float scaleX = _w / area.Size.X;
		float scaleY = _h / area.Size.Y;
		float radius = _minimap.RevealRadius * ((scaleX + scaleY) / 2f);

		foreach (var point in _minimap.VisitedPoints)
		{
			Vector2 p = _minimap.WorldToMap(point, _w, _h);
			DrawCircle(p, radius, TILE_REVEALED, true, -1f, true);
		}
	}

	// Minimap pequeno: zoom centrado no player, só desenha pontos próximos
	private void DrawFogZoomed()
	{
		if (Player.Instance == null) return;

		Vector2 playerWorld   = Player.Instance.GlobalPosition;
		float   zoomWorldSize = _minimap.RevealRadius * 3.2f;
		float   scale         = (_w / 2f) / zoomWorldSize;
		float   radius        = _minimap.RevealRadius * scale;

		Vector2 center = new Vector2(_w / 2f, _h / 2f);

		foreach (var point in _minimap.VisitedPoints)
		{
			Vector2 diff = point - playerWorld;
			if (diff.LengthSquared() > (zoomWorldSize * 1.6f) * (zoomWorldSize * 1.6f)) continue;

			Vector2 p = center + diff * scale;
			DrawCircle(p, radius, TILE_REVEALED, true, -1f, true);
		}
	}

	// Marca a posição de cada colônia viva — sempre visível, mesmo em área não revelada.
	private void DrawColonyMarkers()
	{
		var colonies = GetTree().GetNodesInGroup("Colony");
		if (colonies.Count == 0) return;

		float pulse = 0.6f + 0.4f * (float)Mathf.Sin(Time.GetTicksMsec() / 200.0);

		foreach (Node n in colonies)
		{
			if (n is not Node2D colony || !IsInstanceValid(colony)) continue;

			Vector2 p;

			if (_isBig)
			{
				p = _minimap.WorldToMap(colony.GlobalPosition, _w, _h);
			}
			else
			{
				if (Player.Instance == null) continue;
				Vector2 diff = colony.GlobalPosition - Player.Instance.GlobalPosition;
				float zoomWorldSize = _minimap.RevealRadius * 3.2f;
				if (diff.Length() > zoomWorldSize * 1.6f) continue; // fora do alcance do minimapa pequeno
				float scale = (_w / 2f) / zoomWorldSize;
				p = new Vector2(_w / 2f, _h / 2f) + diff * scale;
			}

			float size = _isBig ? 7f : 5f;

			DrawCircle(p, size + 3f, new Color(COLONY_MARKER_COLOR, 0.20f + 0.15f * pulse));
			DrawCircle(p, size * pulse, COLONY_MARKER_COLOR);

			// "X" branco por cima, reforça que é um alvo de missão
			float h2 = size * 0.5f;
			DrawLine(p + new Vector2(-h2, -h2), p + new Vector2(h2, h2), Colors.White, 1.5f);
			DrawLine(p + new Vector2(-h2,  h2), p + new Vector2(h2, -h2), Colors.White, 1.5f);
		}
	}
}
