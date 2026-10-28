using UnityEngine;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/SoundLibrary")]
public class SoundLibrary : ScriptableObject
{
    public SoundEntry[] sounds;
}

[System.Serializable]
public struct SoundEntry
{
    public SoundID id;
    public AudioClip clip;
    [Range(0, 1)] public float volume;
    public bool loop;
}

public enum SoundID
{
    sfx_bgm_gameplay,
    sfx_bgm_home,
    sfx_click,
    sfx_game_basin_block,
    sfx_game_basin_click,
    sfx_game_basin_disappear,
    sfx_game_basin_drop,
    sfx_game_basin_merge,
    sfx_game_booster_appear,
    sfx_game_booster_Cear,
    sfx_game_booster_magnet,
    sfx_game_booster_Shuffle,
    sfx_game_level_hard,
    sfx_game_loading,
    sfx_game_lose,
    sfx_game_sand_long,
    sfx_game_sand_short,
    sfx_game_win,
    sfx_ui_coin_claim,
    sfx_ui_coin_fly,
    sfx_ui_popup_disappear,
    sfx_ui_popup_show,
    sfx_ui_reward_claim,
    sfx_ui_spin,
    sfx_ui_spin_claim,
    sfx_stop_tunel,
    sfx_break_ice,
    sfx_unlock_bucket_lock
}
