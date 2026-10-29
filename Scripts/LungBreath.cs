// LungBreath.cs — o ciclo respiratorio da fase 2.
//
// Uma fonte unica: o arrasto do jogador, a pulsacao dos alveolos e o brilho
// dos bronquios leem todos o mesmo valor, entao nunca saem de fase.
using Godot;

public partial class LungBreath : Node
{
	public static LungBreath Instance { get; private set; }

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	private const float CYCLE      = 7.0f;  // segundos de um ciclo completo
	private const float IN_END     = 0.38f; // fim da inspiracao (fracao do ciclo)
	private const float HOLD_END   = 0.46f; // fim da pausa
	private const float DRIFT      = 190f;  // forca do arrasto em px/s no pico
	private const float DRIFT_RANGE= 4200f; // distancia em que o arrasto ja e pleno
	// ──────────────────────────────────────────────────────────────────────

	/// 0 = pulmao vazio, 1 = cheio. Usado pra inflar os alveolos.
	public float Volume { get; private set; }

	/// Negativo puxando pra dentro (inspirando), positivo empurrando pra fora.
	public float Flow { get; private set; }

	private float   _t = 0f;
	private float   _lastVolume = 0f;
	private Vector2 _center = Vector2.Zero;

	public override void _Ready()
	{
		Instance = this;

		if (GameManager.Instance != null)
			_center = GameManager.Instance.PlayableArea.GetCenter();
	}

	public override void _ExitTree()
	{
		if (Instance == this) Instance = null;
	}

	public override void _Process(double delta)
	{
		float d = (float)delta;

		_t = Mathf.PosMod(_t + d / CYCLE, 1f);
		_lastVolume = Volume;
		Volume = VolumeAt(_t);

		// fluxo = variacao do volume. Normaliza pelo delta pra nao depender de FPS.
		float raw = d > 0.0001f ? (Volume - _lastVolume) / d : 0f;
		// -1 inspirando, +1 expirando
		Flow = Mathf.Clamp(-raw * CYCLE * 0.8f, -1f, 1f);
	}

	// Sobe, segura no topo, desce mais devagar — como respiracao de verdade.
	private static float VolumeAt(float p)
	{
		if (p < IN_END)   return Mathf.SmoothStep(0f, 1f, p / IN_END);
		if (p < HOLD_END) return 1f;
		return Mathf.SmoothStep(1f, 0f, (p - HOLD_END) / (1f - HOLD_END));
	}

	/// Arrasto do ar naquele ponto, em px/s. Inspirando puxa pra traqueia,
	/// expirando empurra pra fora.
	public Vector2 GetDrift(Vector2 worldPos)
	{
		Vector2 toCenter = _center - worldPos;
		float   dist     = toCenter.Length();
		if (dist < 1f) return Vector2.Zero;

		Vector2 dir = toCenter / dist;

		// perto da traqueia o ar quase nao empurra de lado; longe, empurra pleno
		float reach = Mathf.Clamp(dist / DRIFT_RANGE, 0f, 1f);

		// Flow < 0 = inspirando = puxa PRA traqueia (dir positiva)
		return dir * (-Flow) * DRIFT * reach;
	}
}
