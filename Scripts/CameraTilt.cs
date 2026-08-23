using Godot;

public partial class CameraTilt : Node
{
	private Camera2D _camera;
	private Vector2  _baseZoom   = new Vector2(1f, 1f);
	private float    _pulseTimer = 0f;

	private const float PULSE_SPEED     = 1.2f;
	private const float PULSE_INTENSITY = 0.008f;

	private float   _shakeTimer     = 0f;
	private float   _shakeIntensity = 0f;

	public override void _Ready()
	{
		CallDeferred(MethodName.FindCamera);
		CallDeferred(MethodName.ConnectPlayer);
	}

	private void FindCamera()
	{
		var player = GetTree().GetFirstNodeInGroup("Player") as Node;
		_camera    = player?.GetNodeOrNull<Camera2D>("Camera2D");
		if (_camera != null)
			_baseZoom = _camera.Zoom;
	}

	private void ConnectPlayer()
	{
		var player = GetTree().GetFirstNodeInGroup("Player") as Player;
		if (player != null)
			player.HpChanged += OnHpChanged;
	}
private int _lastHp = -1;

private void OnHpChanged(int cur, int max)
{
	// Só shake se perdeu HP (não ao curar)
	if (_lastHp >= 0 && cur < _lastHp)
	{
		_shakeTimer     = 0.3f;
		_shakeIntensity = 6f;
	}
	_lastHp = cur;
}

	public override void _Process(double delta)
	{
		if (_camera == null) return;

		_pulseTimer += (float)delta;

		// Pulsação suave tipo batimento cardíaco
		float pulse = Mathf.Sin(_pulseTimer * Mathf.Tau * PULSE_SPEED) * PULSE_INTENSITY;
		_camera.Zoom = _baseZoom + new Vector2(pulse, pulse);

		// Shake ao tomar dano
		if (_shakeTimer > 0f)
		{
			_shakeTimer -= (float)delta;
			float s = _shakeIntensity * (_shakeTimer / 0.3f);
			_camera.Offset = new Vector2(
				(float)GD.RandRange(-s, s),
				(float)GD.RandRange(-s, s)
			);
		}
		else
		{
			// Volta o offset para zero suavemente
			float ox = Mathf.Lerp(_camera.Offset.X, 0f, 8f * (float)delta);
			float oy = Mathf.Lerp(_camera.Offset.Y, 0f, 8f * (float)delta);
			_camera.Offset = new Vector2(ox, oy);
		}
	}
}
