using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VideoAndPauseSettings : MonoBehaviour
{
    [SerializeField]
    GameObject PausePanel;

    [SerializeField]
    Button UnpauseButton;

    #if UNITY_EDITOR
        [SerializeField]
        bool StopAutoPause = false;
    #endif
    void Awake()
    {
        if (UnpauseButton != null)
            UnpauseButton.onClick.AddListener(Unpause);

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    public void ChangeVolume(Slider volume) =>
        AudioListener.volume = volume.value;

    public void Update()
    {
        if (PausePanel != null && PausePanel.activeInHierarchy)
        {
            if (UnpauseButton != null && UnpauseButton.isActiveAndEnabled)
            {
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem != null && eventSystem.currentSelectedGameObject != UnpauseButton.gameObject)
                    eventSystem.SetSelectedGameObject(UnpauseButton.gameObject);
            }

            if (InputAdapter.MenuSubmitPressedThisFrame() ||
                InputAdapter.MenuBackPressedThisFrame() ||
                InputAdapter.MenuDailyDropPressedThisFrame())
            {
                Unpause();
            }

            return;
        }

        if (InputAdapter.PausePressedThisFrame()) Pause();
    }

    public void Pause()
    {
        if (PausePanel == null) return;
        PausePanel.SetActive(true);
        Time.timeScale = 0;
    }

    void Unpause()
    {
        if (PausePanel == null) return;
        PausePanel.SetActive(false);
        Time.timeScale = 1;
    }

    void OnApplicationFocus(bool hasFocus)
    {   
        #if UNITY_EDITOR
            if(!hasFocus && !StopAutoPause) Pause();
        #else
            if(!hasFocus) Pause();
        #endif
    }
}
