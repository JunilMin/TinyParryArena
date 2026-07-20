using UnityEngine;

/// <summary>
/// Lightweight global audio service. Clips are loaded from
/// Assets/_Project/Resources/Audio so the generated arena scene needs no
/// manual Inspector wiring.
/// </summary>
public sealed class GameAudioManager : MonoBehaviour
{
    private static GameAudioManager instance;

    private AudioSource sfxSource;
    private AudioSource uiSource;

    private AudioClip[] swings;
    private AudioClip[] parries;
    private AudioClip weaponHit;
    private AudioClip hurt;
    private AudioClip death;
    private AudioClip potion;
    private AudioClip roundStart;
    private AudioClip roundClear;
    private AudioClip victory;
    private AudioClip defeat;
    private AudioClip uiClick;

    public static GameAudioManager Instance
    {
        get
        {
            EnsureInstance();
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => EnsureInstance();

    private static void EnsureInstance()
    {
        if (instance != null) return;
        instance = FindFirstObjectByType<GameAudioManager>();
        if (instance != null) return;

        GameObject go = new GameObject("--- AUDIO MANAGER ---");
        instance = go.AddComponent<GameAudioManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = 0.82f;

        uiSource = gameObject.AddComponent<AudioSource>();
        uiSource.playOnAwake = false;
        uiSource.spatialBlend = 0f;
        uiSource.volume = 0.78f;

        swings = new[] { Load("Swing_01"), Load("Swing_02"), Load("Swing_03") };
        parries = new[] { Load("Parry_01"), Load("Parry_02") };
        weaponHit = Load("WeaponHit");
        hurt = Load("Hurt_01");
        death = Load("Death_01");
        potion = Load("Potion");
        roundStart = Load("RoundStart");
        roundClear = Load("RoundClear");
        victory = Load("Victory");
        defeat = Load("Defeat");
        uiClick = Load("UIClick");
    }

    private static AudioClip Load(string name) => Resources.Load<AudioClip>("Audio/" + name);

    private void Play(AudioSource source, AudioClip clip, float volume = 1f, float pitchMin = 1f, float pitchMax = 1f)
    {
        if (source == null || clip == null) return;
        source.pitch = Random.Range(pitchMin, pitchMax);
        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private AudioClip RandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    // Player swings stay quick and bright; combo finishers are slightly lower and heavier.
    public void PlayPlayerSwing(int comboIndex)
    {
        AudioClip clip = swings != null && swings.Length > 0
            ? swings[Mathf.Abs(comboIndex) % swings.Length]
            : null;
        float volume = comboIndex % 3 == 2 ? 0.88f : 0.78f;
        float pitchMin = comboIndex % 3 == 2 ? 0.88f : 0.97f;
        float pitchMax = comboIndex % 3 == 2 ? 0.96f : 1.07f;
        Play(sfxSource, clip, volume, pitchMin, pitchMax);
    }

    // Boss attacks use the same blade recordings at a lower pitch for more weight.
    public void PlayBossSwing(int motion)
    {
        AudioClip clip = swings != null && swings.Length > 0
            ? swings[Mathf.Abs(motion + 1) % swings.Length]
            : null;
        Play(sfxSource, clip, 0.92f, 0.78f, 0.91f);
    }

    // Compatibility for any older script that still calls PlaySwing().
    public void PlaySwing() => PlayPlayerSwing(Random.Range(0, 3));
    public void PlayWeaponHit() => Play(sfxSource, weaponHit, 0.82f, 0.95f, 1.05f);
    public void PlayParry() => Play(sfxSource, RandomClip(parries), 1f, 0.92f, 1.02f);
    public void PlayHurt() => Play(sfxSource, hurt, 0.62f, 0.90f, 1.08f);
    public void PlayDeath() => Play(sfxSource, death, 0.92f, 0.84f, 0.96f);
    public void PlayPotion() => Play(sfxSource, potion, 0.72f, 0.98f, 1.04f);
    public void PlayRoundStart() => Play(uiSource, roundStart, 0.72f);
    public void PlayRoundClear() => Play(uiSource, roundClear, 0.72f);
    public void PlayVictory() => Play(uiSource, victory, 0.92f);
    public void PlayDefeat() => Play(uiSource, defeat, 0.82f, 0.90f, 0.96f);
    public void PlayUIClick() => Play(uiSource, uiClick, 0.55f);
}
