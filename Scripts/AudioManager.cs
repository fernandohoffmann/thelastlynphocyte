using Godot;
using System.Collections.Generic;

public partial class AudioManager : Node
{
	public static AudioManager Instance { get; private set; }

	private AudioStreamPlayer _sfxPlayer1;
	private AudioStreamPlayer _sfxPlayer2;
	private AudioStreamPlayer _sfxPlayer3;
	private AudioStreamPlayer _musicPlayer;   // coração / rock
	private AudioStreamPlayer _fadePlayer;    // fade in do rock após intro

	private AudioStream _playerHit;
	private AudioStream _enemyHit;
	private AudioStream _pickup;
	private AudioStream _uiClick;
	private AudioStream _uiHover;
	private AudioStream _uiUnlock;
	private AudioStream _playerLevelup;
	private AudioStream _heartLoop;
	private AudioStream _rock1;
	private AudioStream _rock2;

	// Músicas de rock em ordem aleatória
	private List<AudioStream> _rockTracks = new();
	private int               _rockIndex  = 0;
	private bool              _playingRock = false;

	public float MusicVolume { get; private set; } = 0.8f;
	public float SfxVolume   { get; private set; } = 1.0f;

	// Fade in da música após intro
	private float _fadeTarget  = 0f;
	private float _fadeSpeed   = 0f;
	private bool  _fading      = false;

	public override void _Ready()
	{
		if (Instance != null) { QueueFree(); return; }
		Instance = this;

		_sfxPlayer1  = new AudioStreamPlayer { Bus = "Master" };
		_sfxPlayer2  = new AudioStreamPlayer { Bus = "Master" };
		_sfxPlayer3  = new AudioStreamPlayer { Bus = "Master" };
		_musicPlayer = new AudioStreamPlayer { Bus = "Master" };
		_fadePlayer  = new AudioStreamPlayer { Bus = "Master" };
		AddChild(_sfxPlayer1);
		AddChild(_sfxPlayer2);
		AddChild(_sfxPlayer3);
		AddChild(_musicPlayer);
		AddChild(_fadePlayer);

		_playerHit     = Load("res://Assets/sfx/sfx_player_hit.mp3");
		_enemyHit      = Load("res://Assets/sfx/sfx_enemy_hit.mp3");
		_pickup        = Load("res://Assets/sfx/sfx_pickup.wav");
		_uiClick       = Load("res://Assets/sfx/sfx_ui_click.wav");
		_uiHover       = Load("res://Assets/sfx/sfx_ui_hover.mp3");
		_uiUnlock      = Load("res://Assets/sfx/sfx_ui_unlock.mp3");
		_playerLevelup = Load("res://Assets/sfx/sfx_player_levelup.mp3");
		_heartLoop     = Load("res://Assets/sfx/sfx_heart.mp3");
		_rock1         = Load("res://Assets/sfx/rock1.mp3");
		_rock2         = Load("res://Assets/sfx/rock2.mp3");

		if (_rock1 != null) _rockTracks.Add(_rock1);
		if (_rock2 != null) _rockTracks.Add(_rock2);

		// Embaralha
		if (_rockTracks.Count > 1)
		{
			var tmp = _rockTracks[0];
			_rockTracks[0] = _rockTracks[(int)(GD.Randi() % (uint)_rockTracks.Count)];
			_rockTracks[_rockTracks.Count - 1] = tmp;
		}

		_musicPlayer.Finished += OnMusicFinished;
	}

	public override void _Process(double delta)
	{
		if (!_fading) return;
		float cur = Mathf.DbToLinear(_musicPlayer.VolumeDb);
		cur = Mathf.MoveToward(cur, _fadeTarget, _fadeSpeed * (float)delta);
		_musicPlayer.VolumeDb = ToDb(cur);
		if (Mathf.IsEqualApprox(cur, _fadeTarget, 0.01f))
			_fading = false;
	}

	private AudioStream Load(string path)
	{
		if (ResourceLoader.Exists(path))
			return GD.Load<AudioStream>(path);
		GD.PrintErr($"[AudioManager] Arquivo nao encontrado: {path}");
		return null;
	}

	private float ToDb(float linear) => linear <= 0.001f ? -80f : Mathf.LinearToDb(linear);

	public void SetMusicVolume(float v)
	{
		MusicVolume = Mathf.Clamp(v, 0f, 1f);
		if (!_fading)
			_musicPlayer.VolumeDb = ToDb(MusicVolume);
	}

	public void SetSfxVolume(float v) { SfxVolume = Mathf.Clamp(v, 0f, 1f); }

	private void Play1(AudioStream stream, float volumeDb = 0f, float pitch = 1f)
	{
		if (stream == null) return;
		_sfxPlayer1.Stream = stream; _sfxPlayer1.VolumeDb = volumeDb + ToDb(SfxVolume);
		_sfxPlayer1.PitchScale = pitch; _sfxPlayer1.Play();
	}
	private void Play2(AudioStream stream, float volumeDb = 0f, float pitch = 1f)
	{
		if (stream == null) return;
		_sfxPlayer2.Stream = stream; _sfxPlayer2.VolumeDb = volumeDb + ToDb(SfxVolume);
		_sfxPlayer2.PitchScale = pitch; _sfxPlayer2.Play();
	}
	private void Play3(AudioStream stream, float volumeDb = 0f, float pitch = 1f)
	{
		if (stream == null) return;
		_sfxPlayer3.Stream = stream; _sfxPlayer3.VolumeDb = volumeDb + ToDb(SfxVolume);
		_sfxPlayer3.PitchScale = pitch; _sfxPlayer3.Play();
	}

	public void PlayShoot()     { }
	public void PlayEnemyHit()  => Play1(_enemyHit,     -10f, 1.0f);
	public void PlayPlayerHit() => Play2(_playerHit,      -8f, 1.0f);
	public void PlayPickup()    => Play2(_pickup,          -2f, 1.1f);
	public void PlayUiClick()   => Play3(_uiClick,          0f, 1.0f);
	public void PlayUiHover()   => Play3(_uiHover,         -8f, 1.0f);
	public void PlayLevelUp()   => Play3(_playerLevelup,    0f, 1.0f);

	public void PlayUiUnlock()
	{
		if (_uiUnlock == null) return;
		_sfxPlayer3.Stream = _uiUnlock;
		_sfxPlayer3.VolumeDb = ToDb(SfxVolume);
		_sfxPlayer3.PitchScale = 1.0f;
		_sfxPlayer3.Play(0.4f);
	}

	// ── Músicas de Rock ───────────────────────────────────────
	public void StartRockMusic(bool fadeIn = false, float fadeDuration = 3f)
	{
		if (_rockTracks.Count == 0) return;
		StopHeartLoop();
		_playingRock = true;
		_rockIndex   = (int)(GD.Randi() % (uint)_rockTracks.Count);

		_musicPlayer.Stream   = _rockTracks[_rockIndex];
		_musicPlayer.VolumeDb = fadeIn ? -80f : ToDb(MusicVolume);
		_musicPlayer.Play();

		if (fadeIn && fadeDuration > 0f)
		{
			_fadeTarget = MusicVolume;
			_fadeSpeed  = MusicVolume / fadeDuration;
			_fading     = true;
		}
	}

	public void StopRockMusic()
	{
		_playingRock = false;
		_musicPlayer.Stop();
	}

	private void OnMusicFinished()
	{
		if (!_playingRock) return;
		// Próxima música em ordem aleatória
		_rockIndex = (_rockIndex + 1 + (int)(GD.Randi() % (uint)_rockTracks.Count)) % _rockTracks.Count;
		_musicPlayer.Stream   = _rockTracks[_rockIndex];
		_musicPlayer.VolumeDb = ToDb(MusicVolume);
		_musicPlayer.Play();
	}

	// ── Loop de coração ───────────────────────────────────────
	public void PlayHeartLoop()
	{
		if (_heartLoop == null || _musicPlayer.Playing) return;
		_playingRock          = false;
		_musicPlayer.Stream   = _heartLoop;
		_musicPlayer.VolumeDb = ToDb(MusicVolume);
		_musicPlayer.PitchScale = 4.0f;
		_musicPlayer.Finished -= OnMusicFinished;
		_musicPlayer.Finished += LoopHeart;
		_musicPlayer.Play();
	}

	public void StopHeartLoop()
	{
		_musicPlayer.Stop();
		_musicPlayer.Finished -= LoopHeart;
	}

	public void PauseHeartLoop(bool paused)
	{
		_musicPlayer.StreamPaused = paused;
	}

	public void UpdateHeartPitch(float progress01)
	{
		_musicPlayer.PitchScale = Mathf.Lerp(1.0f, 2.2f, progress01);
	}

	private void LoopHeart() => _musicPlayer.Play();
}
