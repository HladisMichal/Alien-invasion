using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SFXManagerScript : MonoBehaviour
{
	public enum LoopSfxId
	{
		UfoSpawn,
		UfoFire
	}

	public enum SfxId
	{
		Jump,
		Dash,
		PlayerFire,
		EnemyFire,
		PlayerHit
	}

	public static SFXManagerScript Instance;

	[SerializeField] private AudioSource sfxSource;
	[SerializeField] private AudioMixerGroup sfxMixerGroup;
	[SerializeField] private bool pauseSfxWhenTimeScaleZero = true;
	[SerializeField] private bool blockGameplaySfxWithoutPlayer = true;
	[SerializeField] private bool blockGameplaySfxOutsideGameplayScenes = true;
	[SerializeField] private string[] gameplaySceneNames = { "Game", "Tutorial" };
	[Header("Central SFX Clips")]
	[SerializeField] private AudioClip jumpClip;
	[SerializeField] private AudioClip dashClip;
	[SerializeField] private AudioClip playerFireClip;
	[SerializeField] private AudioClip enemyFireClip;
	[SerializeField] private AudioClip playerHitClip;
	[SerializeField] private AudioClip ufoSpawnLoopClip;
	[SerializeField] private AudioClip ufoFireLoopClip;

	private AudioSource ufoSpawnLoopSource;
	private AudioSource ufoFireLoopSource;
	private int ufoSpawnLoopRequestCount = 0;
	private int ufoFireLoopRequestCount = 0;
	private bool isSfxPaused = false;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
			return;
		}

		ResolveSfxMixerGroup();
		EnsureAudioSource();
	}

	private void Update()
	{
		if (!ShouldAllowGameplaySfx())
		{
			StopAllLoopSourcesImmediate();
		}

		if (!pauseSfxWhenTimeScaleZero) return;

		bool shouldPause = Time.timeScale == 0f;
		if (shouldPause == isSfxPaused) return;

		SetSfxPaused(shouldPause);
	}

	public void PlaySFX(AudioClip clip, float volume = 1f)
	{
		if (clip == null) return;
		if (isSfxPaused) return;
		if (!ShouldAllowGameplaySfx()) return;
		EnsureAudioSource();
		sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
	}

	public void PlaySFX(SfxId id, float volume = 1f)
	{
		AudioClip clip = GetClip(id);
		if (clip == null) return;
		PlaySFX(clip, volume);
	}

	public void StartLoopSFX(LoopSfxId id, float volume = 1f)
	{
		if (!ShouldAllowGameplaySfx()) return;

		AudioSource source = GetLoopSource(id);
		AudioClip clip = GetLoopClip(id);
		if (source == null || clip == null) return;

		IncrementLoopRequest(id);
		source.volume = Mathf.Clamp01(volume);

		if (source.clip != clip)
		{
			source.clip = clip;
		}

		if (!source.isPlaying)
		{
			source.Play();
			if (isSfxPaused)
			{
				source.Pause();
			}
		}
	}

	public void StopLoopSFX(LoopSfxId id)
	{
		AudioSource source = GetLoopSource(id);
		if (source == null) return;

		DecrementLoopRequest(id);
		if (GetLoopRequestCount(id) > 0) return;

		if (source.isPlaying)
		{
			source.Stop();
		}
	}

	public void StopAllSfxImmediate()
	{
		EnsureAudioSource();
		isSfxPaused = false;

		if (sfxSource != null)
		{
			sfxSource.Stop();
		}

		StopAllLoopSourcesImmediate();
	}

	private AudioClip GetClip(SfxId id)
	{
		switch (id)
		{
			case SfxId.Jump:
				return jumpClip;
			case SfxId.Dash:
				return dashClip;
			case SfxId.PlayerFire:
				return playerFireClip;
			case SfxId.EnemyFire:
				return enemyFireClip;
			case SfxId.PlayerHit:
				return playerHitClip;
			default:
				return null;
		}
	}

	private AudioClip GetLoopClip(LoopSfxId id)
	{
		switch (id)
		{
			case LoopSfxId.UfoSpawn:
				return ufoSpawnLoopClip;
			case LoopSfxId.UfoFire:
				return ufoFireLoopClip;
			default:
				return null;
		}
	}

	private AudioSource GetLoopSource(LoopSfxId id)
	{
		switch (id)
		{
			case LoopSfxId.UfoSpawn:
				if (ufoSpawnLoopSource == null)
				{
					ufoSpawnLoopSource = CreateLoopSource("UfoSpawnLoopSource");
				}
				return ufoSpawnLoopSource;
			case LoopSfxId.UfoFire:
				if (ufoFireLoopSource == null)
				{
					ufoFireLoopSource = CreateLoopSource("UfoFireLoopSource");
				}
				return ufoFireLoopSource;
			default:
				return null;
		}
	}

	private AudioSource CreateLoopSource(string sourceName)
	{
		ResolveSfxMixerGroup();

		GameObject sourceObj = new GameObject(sourceName);
		sourceObj.transform.SetParent(transform, false);
		AudioSource source = sourceObj.AddComponent<AudioSource>();
		source.playOnAwake = false;
		source.loop = true;
		source.spatialBlend = 0f;
		if (sfxMixerGroup != null)
		{
			source.outputAudioMixerGroup = sfxMixerGroup;
		}
		return source;
	}

	private void IncrementLoopRequest(LoopSfxId id)
	{
		switch (id)
		{
			case LoopSfxId.UfoSpawn:
				ufoSpawnLoopRequestCount++;
				break;
			case LoopSfxId.UfoFire:
				ufoFireLoopRequestCount++;
				break;
		}
	}

	private void DecrementLoopRequest(LoopSfxId id)
	{
		switch (id)
		{
			case LoopSfxId.UfoSpawn:
				ufoSpawnLoopRequestCount = Mathf.Max(0, ufoSpawnLoopRequestCount - 1);
				break;
			case LoopSfxId.UfoFire:
				ufoFireLoopRequestCount = Mathf.Max(0, ufoFireLoopRequestCount - 1);
				break;
		}
	}

	private int GetLoopRequestCount(LoopSfxId id)
	{
		switch (id)
		{
			case LoopSfxId.UfoSpawn:
				return ufoSpawnLoopRequestCount;
			case LoopSfxId.UfoFire:
				return ufoFireLoopRequestCount;
			default:
				return 0;
		}
	}

	private void EnsureAudioSource()
	{
		ResolveSfxMixerGroup();

		if (sfxSource == null)
		{
			sfxSource = GetComponent<AudioSource>();
		}

		if (sfxSource == null)
		{
			sfxSource = gameObject.AddComponent<AudioSource>();
		}

		sfxSource.playOnAwake = false;
		sfxSource.loop = false;
		sfxSource.spatialBlend = 0f;

		if (sfxMixerGroup != null)
		{
			sfxSource.outputAudioMixerGroup = sfxMixerGroup;
			if (ufoSpawnLoopSource != null) ufoSpawnLoopSource.outputAudioMixerGroup = sfxMixerGroup;
			if (ufoFireLoopSource != null) ufoFireLoopSource.outputAudioMixerGroup = sfxMixerGroup;
		}
	}

	private void ResolveSfxMixerGroup()
	{
		if (sfxMixerGroup != null) return;
		if (SoundMixerManager.Instance == null || SoundMixerManager.Instance.AudioMixer == null) return;

		AudioMixerGroup[] groups = SoundMixerManager.Instance.AudioMixer.FindMatchingGroups("SFX");
		if (groups != null && groups.Length > 0)
		{
			sfxMixerGroup = groups[0];
			return;
		}

		AudioMixerGroup[] masterGroups = SoundMixerManager.Instance.AudioMixer.FindMatchingGroups("Master");
		if (masterGroups != null && masterGroups.Length > 0)
		{
			sfxMixerGroup = masterGroups[0];
		}
	}

	private void SetSfxPaused(bool paused)
	{
		isSfxPaused = paused;

		if (paused)
		{
			sfxSource.Pause();
			if (ufoSpawnLoopSource != null) ufoSpawnLoopSource.Pause();
			if (ufoFireLoopSource != null) ufoFireLoopSource.Pause();
		}
		else
		{
			sfxSource.UnPause();
			if (ufoSpawnLoopSource != null) ufoSpawnLoopSource.UnPause();
			if (ufoFireLoopSource != null) ufoFireLoopSource.UnPause();
		}
	}

	private bool HasPlayerInScene()
	{
		try
		{
			return GameObject.FindGameObjectWithTag("Player") != null;
		}
		catch
		{
			return false;
		}
	}

	private void StopAllLoopSourcesImmediate()
	{
		ufoSpawnLoopRequestCount = 0;
		ufoFireLoopRequestCount = 0;
		if (ufoSpawnLoopSource != null && ufoSpawnLoopSource.isPlaying) ufoSpawnLoopSource.Stop();
		if (ufoFireLoopSource != null && ufoFireLoopSource.isPlaying) ufoFireLoopSource.Stop();
	}

	private bool ShouldAllowGameplaySfx()
	{
		if (blockGameplaySfxOutsideGameplayScenes && !IsGameplayScene())
		{
			return false;
		}

		if (blockGameplaySfxWithoutPlayer && !HasPlayerInScene())
		{
			return false;
		}

		return true;
	}

	private bool IsGameplayScene()
	{
		if (gameplaySceneNames == null || gameplaySceneNames.Length == 0)
		{
			return true;
		}

		string currentSceneName = SceneManager.GetActiveScene().name;
		for (int i = 0; i < gameplaySceneNames.Length; i++)
		{
			if (!string.IsNullOrEmpty(gameplaySceneNames[i]) && gameplaySceneNames[i] == currentSceneName)
			{
				return true;
			}
		}

		return false;
	}
}
