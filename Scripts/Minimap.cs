using Godot;

public partial class Minimap : CanvasLayer
{
	private const float MAP_W    = 200f;
	private const float MAP_H    = 200f;
	private const float MARGIN   = 10f;
	private const float BIG_W    = 700f;
	private const float BIG_H    = 700f;
	private const float REVEAL_R = 500f;

	private TileMapLayer _navLayer;
	private Vector2I[]   _navCells;
	private float[]      _revealAlpha;
	private Rect2I       _navBounds;

	private MinimapDrawer _smallDrawer;
	private MinimapDrawer _bigDrawer;

	public Vector2 SmoothedPlayerWorld { get; private set; }
	private bool _smoothInit = false;

	// Textura pré-renderizada do mapa grande
	private SubViewport       _bigViewport;
	private SubViewportContainer _bigViewportContainer;
	private bool              _bigDirty   = true;
	private float             _dirtyTimer = 0f;
	private const float       DIRTY_INTERVAL = 0.1f; // atualiza no máximo 10x/s

	public override void _Ready()
	{
		Layer       = 7;
		ProcessMode = ProcessModeEnum.Always;

		CallDeferred(MethodName.InitNavLayer);

		var root = new Control();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(root);

		_smallDrawer = new MinimapDrawer();
		_smallDrawer.Init(this, MAP_W, MAP_H, false);
		_smallDrawer.ClipContents = false;
		_smallDrawer.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		_smallDrawer.OffsetLeft   = -(MAP_W + MARGIN);
		_smallDrawer.OffsetTop    = MARGIN + 14f;
		_smallDrawer.OffsetRight  = -MARGIN;
		_smallDrawer.OffsetBottom = MAP_H + MARGIN + 14f;
		root.AddChild(_smallDrawer);

		_bigDrawer = new MinimapDrawer();
		_bigDrawer.Init(this, BIG_W, BIG_H, true);
		_bigDrawer.ClipContents = false;
		_bigDrawer.SetAnchorsPreset(Control.LayoutPreset.Center);
		_bigDrawer.OffsetLeft   = -BIG_W / 2f;
		_bigDrawer.OffsetTop    = -BIG_H / 2f;
		_bigDrawer.OffsetRight  =  BIG_W / 2f;
		_bigDrawer.OffsetBottom =  BIG_H / 2f;
		_bigDrawer.Visible = false;
		root.AddChild(_bigDrawer);
	}

	private void InitNavLayer()
	{
		_navLayer = GetTree().CurrentScene?.GetNodeOrNull<TileMapLayer>("World/NavLayer");
		if (_navLayer == null) return;

		var cells    = _navLayer.GetUsedCells();
		_navCells    = new Vector2I[cells.Count];
		_revealAlpha = new float[cells.Count];

		for (int i = 0; i < cells.Count; i++)
			_navCells[i] = cells[i];

		_navBounds = _navLayer.GetUsedRect();
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

			if (_navCells != null)
			{
				bool revealed = RevealNearPlayer(target, (float)delta);
				if (revealed) _bigDirty = true;
			}
		}

		bool tab = Input.IsKeyPressed(Key.Tab);

		// Minimap pequeno sempre atualiza (leve)
		_smallDrawer.QueueRedraw();

		// Mapa grande: só atualiza se dirty e em intervalo limitado
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
			// Sempre pede redraw para o ponto do player (leve)
			_bigDrawer.QueueRedraw();
		}
		else
		{
			_bigDrawer.Visible = false;
		}
	}

	// Retorna true se algum tile foi revelado
	private bool RevealNearPlayer(Vector2 worldPos, float delta)
	{
		if (_navLayer == null) return false;

		Vector2I playerTile = _navLayer.LocalToMap(_navLayer.ToLocal(worldPos));
		float    tileSize   = _navLayer.TileSet.TileSize.X;
		bool     changed    = false;

		for (int i = 0; i < _navCells.Length; i++)
		{
			int   dx   = _navCells[i].X - playerTile.X;
			int   dy   = _navCells[i].Y - playerTile.Y;
			float dist = Mathf.Sqrt(dx * dx + dy * dy) * tileSize;

			if (dist > REVEAL_R) continue;

			float prev = _revealAlpha[i];
			float t    = 1f - Mathf.Clamp(dist / REVEAL_R, 0f, 1f);
			_revealAlpha[i] = Mathf.Min(1f, _revealAlpha[i] + Mathf.Pow(t, 0.4f) * delta * 5f);

			if (_revealAlpha[i] != prev) changed = true;
		}

		return changed;
	}

	public Vector2 WorldToMapSmooth(float w, float h)
	{
		if (_navLayer == null) return new Vector2(w / 2f, h / 2f);
		return WorldToMap(SmoothedPlayerWorld, w, h);
	}

	public Vector2 TileToMap(Vector2I tile, float w, float h)
	{
		if (_navBounds.Size == Vector2I.Zero) return Vector2.Zero;
		float nx = (float)(tile.X - _navBounds.Position.X) / _navBounds.Size.X;
		float ny = (float)(tile.Y - _navBounds.Position.Y) / _navBounds.Size.Y;
		return new Vector2(Mathf.Clamp(nx * w, 0, w), Mathf.Clamp(ny * h, 0, h));
	}

	public Vector2 WorldToMap(Vector2 worldPos, float w, float h)
	{
		if (_navLayer == null) return Vector2.Zero;
		var  local  = _navLayer.ToLocal(worldPos);
		var  bounds = _navBounds;
		if (bounds.Size == Vector2I.Zero) return Vector2.Zero;

		float tileSize = _navLayer.TileSet.TileSize.X;
		float nx = (local.X / tileSize - bounds.Position.X) / bounds.Size.X;
		float ny = (local.Y / tileSize - bounds.Position.Y) / bounds.Size.Y;
		return new Vector2(Mathf.Clamp(nx * w, 0, w), Mathf.Clamp(ny * h, 0, h));
	}

	public Vector2I[]   NavCells    => _navCells;
	public float[]      RevealAlpha => _revealAlpha;
	public TileMapLayer NavLayer    => _navLayer;
	public Rect2I       NavBounds   => _navBounds;
}

public partial class MinimapDrawer : Control
{
	private static readonly Color BG_COLOR      = new Color(0.28f, 0.05f, 0.08f);
	private static readonly Color TILE_HIDDEN   = new Color(0.28f, 0.05f, 0.08f);
	private static readonly Color TILE_REVEALED = new Color(0.75f, 0.25f, 0.30f);
	private static readonly Color BORDER_COLOR  = new Color(0.6f,  0.6f,  0.6f, 0.8f);
	private static readonly Color PLAYER_COLOR  = new Color(0.3f,  0.7f,  1f);

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

		var cells  = _minimap.NavCells;
		var alpha  = _minimap.RevealAlpha;
		var bounds = _minimap.NavBounds;

		if (cells != null && bounds.Size != Vector2I.Zero)
		{
			if (_isBig)
			{
				DrawAllCellsBase(cells, bounds);
				DrawAllCells(cells, alpha, bounds);
			}
			else
			{
				DrawZoomedCellsBase(cells);
				DrawZoomedCells(cells, alpha);
			}
		}

		if (Player.Instance != null)
		{
			Vector2 p    = _isBig ? _minimap.WorldToMapSmooth(_w, _h) : new Vector2(_w / 2f, _h / 2f);
			float   size = _isBig ? 5f : 4f;
			DrawCircle(p, size * 2.5f, new Color(PLAYER_COLOR, 0.2f));
			DrawCircle(p, size, PLAYER_COLOR);
		}

		// Borda sólida na cor do fundo — cobre pixels que vazem
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

	private void DrawAllCellsBase(Vector2I[] cells, Rect2I bounds)
	{
		float tileW = Mathf.Max(_w / bounds.Size.X, 1.5f);
		float tileH = Mathf.Max(_h / bounds.Size.Y, 1.5f);

		for (int i = 0; i < cells.Length; i++)
		{
			Vector2 pos = _minimap.TileToMap(cells[i], _w, _h);
			if (pos.X + tileW < 0 || pos.Y + tileH < 0 || pos.X > _w || pos.Y > _h) continue;
			DrawRect(new Rect2(pos.X, pos.Y, tileW + 0.5f, tileH + 0.5f), TILE_HIDDEN);
		}
	}

	private void DrawAllCells(Vector2I[] cells, float[] alpha, Rect2I bounds)
	{
		float tileW = Mathf.Max(_w / bounds.Size.X, 1.5f);
		float tileH = Mathf.Max(_h / bounds.Size.Y, 1.5f);

		for (int i = 0; i < cells.Length; i++)
		{
			if (alpha[i] <= 0f) continue;
			Vector2 pos = _minimap.TileToMap(cells[i], _w, _h);
			if (pos.X + tileW < 0 || pos.Y + tileH < 0 || pos.X > _w || pos.Y > _h) continue;
			DrawRect(new Rect2(pos.X, pos.Y, tileW + 0.5f, tileH + 0.5f),
				new Color(TILE_REVEALED, alpha[i]));
		}
	}

	private void DrawZoomedCellsBase(Vector2I[] cells)
	{
		var navLayer = _minimap.NavLayer;
		if (navLayer == null || Player.Instance == null) return;

		const int VIEW_TILES  = 50;
		Vector2   realWorld   = Player.Instance.GlobalPosition;
		Vector2   local       = navLayer.ToLocal(realWorld);
		float     tileSize    = navLayer.TileSet.TileSize.X;
		float     playerTileX = local.X / tileSize;
		float     playerTileY = local.Y / tileSize;
		float     tileW       = _w / VIEW_TILES;
		float     tileH       = _h / VIEW_TILES;
		int       half        = VIEW_TILES / 2;

		for (int i = 0; i < cells.Length; i++)
		{
			float dx = cells[i].X - playerTileX;
			float dy = cells[i].Y - playerTileY;
			if (dx < -half || dx > half || dy < -half || dy > half) continue;

			float px = _w / 2f + dx * tileW;
			float py = _h / 2f + dy * tileH;

			float rx = Mathf.Max(0, px);
			float ry = Mathf.Max(0, py);
			float rw = Mathf.Min(px + tileW, _w) - rx;
			float rh = Mathf.Min(py + tileH, _h) - ry;
			if (rw <= 0 || rh <= 0) continue;

			DrawRect(new Rect2(rx, ry, rw, rh), TILE_HIDDEN);
		}
	}

	private void DrawZoomedCells(Vector2I[] cells, float[] alpha)
	{
		var navLayer = _minimap.NavLayer;
		if (navLayer == null || Player.Instance == null) return;

		const int VIEW_TILES  = 50;
		Vector2   realWorld   = Player.Instance.GlobalPosition;
		Vector2   local       = navLayer.ToLocal(realWorld);
		float     tileSize    = navLayer.TileSet.TileSize.X;
		float     playerTileX = local.X / tileSize;
		float     playerTileY = local.Y / tileSize;
		float     tileW       = _w / VIEW_TILES;
		float     tileH       = _h / VIEW_TILES;
		int       half        = VIEW_TILES / 2;

		for (int i = 0; i < cells.Length; i++)
		{
			if (alpha[i] <= 0f) continue;

			float dx = cells[i].X - playerTileX;
			float dy = cells[i].Y - playerTileY;
			if (dx < -half || dx > half || dy < -half || dy > half) continue;

			float px = _w / 2f + dx * tileW;
			float py = _h / 2f + dy * tileH;

			float rx = Mathf.Max(0, px);
			float ry = Mathf.Max(0, py);
			float rw = Mathf.Min(px + tileW, _w) - rx;
			float rh = Mathf.Min(py + tileH, _h) - ry;
			if (rw <= 0 || rh <= 0) continue;

			DrawRect(new Rect2(rx, ry, rw, rh), new Color(TILE_REVEALED, alpha[i]));
		}
	}
}
