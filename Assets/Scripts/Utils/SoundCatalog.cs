using System.Collections.Generic;
using UnityEngine;

public static class SoundCatalog
{
    const string ResourcePath = "Sounds/V2";

    static readonly string[] VariantPrefixes = new string[]
    {
        "flipper_click_",
        "ball_hit_",
        "bumper_pop_",
        "slingshot_snap_"
    };

    static bool loaded;
    static Dictionary<string, AudioClip> clipsByName = new Dictionary<string, AudioClip>();
    static Dictionary<string, List<AudioClip>> clipsByPrefix = new Dictionary<string, List<AudioClip>>();

    static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;

        AudioClip[] clips = Resources.LoadAll<AudioClip>(ResourcePath);
        foreach (AudioClip clip in clips)
        {
            if (!clip || string.IsNullOrEmpty(clip.name))
                continue;

            clipsByName[clip.name] = clip;

            foreach (string prefix in VariantPrefixes)
            {
                if (!clip.name.StartsWith(prefix))
                    continue;

                if (!clipsByPrefix.TryGetValue(prefix, out List<AudioClip> list))
                {
                    list = new List<AudioClip>();
                    clipsByPrefix[prefix] = list;
                }

                list.Add(clip);
            }
        }
    }

    public static AudioClip Get(string clipName)
    {
        EnsureLoaded();
        if (clipsByName.TryGetValue(clipName, out AudioClip clip))
            return clip;
        return null;
    }

    public static AudioClip GetRandom(string prefix)
    {
        EnsureLoaded();
        if (!clipsByPrefix.TryGetValue(prefix, out List<AudioClip> list) || list.Count == 0)
            return null;
        return list[Random.Range(0, list.Count)];
    }

    public static bool PlayNamed(AudioSource source, string clipName)
    {
        if (!source) return false;

        AudioClip clip = Get(clipName);
        if (!clip) return false;

        source.PlayOneShot(clip);
        return true;
    }

    public static bool PlayRandom(AudioSource source, string prefix)
    {
        if (!source) return false;

        AudioClip clip = GetRandom(prefix);
        if (!clip) return false;

        source.PlayOneShot(clip);
        return true;
    }
}
