using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUIAudio : MonoBehaviour
{
    // Same-scene "singleton" pattern 
    private static GUIAudio _instance;
    public static GUIAudio instance
    {
        get
        {
            if (!_instance)
                _instance = FindObjectOfType<GUIAudio>();
            return _instance;
        }
    }

    [SerializeField]
    AudioSource _UISpeaker;

    [Header("Audio Files")]
    [SerializeField]
    AudioClip _purchase;

    [SerializeField]
    AudioClip _purchaseFail;

    [SerializeField]
    AudioClip _achievementGet;

    void Awake()
    {
        OverrideClipIfFound(ref _purchase, "ui_purchase_confirm");
        OverrideClipIfFound(ref _purchaseFail, "ui_purchase_fail");
        OverrideClipIfFound(ref _achievementGet, "ui_achievement_unlock");
    }

    void OverrideClipIfFound(ref AudioClip slot, string clipName)
    {
        AudioClip clip = SoundCatalog.Get(clipName);
        if (clip)
            slot = clip;
    }

    public static AudioSource Speaker
    {
        get => instance._UISpeaker;
    }

    public static AudioClip PurchaseSound
    {
        get => instance._purchase;
    }

    public static AudioClip PurchaseFailSound
    {
        get => instance._purchaseFail;
    }

    public static AudioClip AchievementGetSound
    {
        get => instance._achievementGet;
    }
}
