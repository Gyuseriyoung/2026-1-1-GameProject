using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SoundSettingsUI : MonoBehaviour
{
    [Header("Master")]
    public Slider masterSlider;
    public TMP_InputField masterInputField;
    public TMP_Text masterValueText;
    public Toggle masterMuteToggle;

    [Header("BGM")]
    public Slider bgmSlider;
    public TMP_InputField bgmInputField;
    public TMP_Text bgmValueText;
    public Toggle bgmMuteToggle;

    [Header("SFX")]
    public Slider sfxSlider;
    public TMP_InputField sfxInputField;
    public TMP_Text sfxValueText;
    public Toggle sfxMuteToggle;

    [Header("Common")]
    public Button closeButton;
    public GameObject settingsPanel;

    private float _masterVol = 1f;
    private float _bgmVol = 0.5f;
    private float _sfxVol = 1f;
    private bool _muteMaster;
    private bool _muteBGM;
    private bool _muteSFX;
    private bool _isInit;

    private void Awake()
    {
        LoadSettings();
    }

    private void Start()
    {
        // Settings are applied by AudioManager on Start, 
        // but we can force it here just in case.
        ApplyAllSettings();
        RegisterListeners();
        ApplyToUI();
    }

    private void OnDestroy()
    {
        UnregisterListeners();
    }

    private void RegisterListeners()
    {
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterSlider);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBGMSlider);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXSlider);

        if (masterInputField != null) masterInputField.onEndEdit.AddListener(OnMasterInput);
        if (bgmInputField != null) bgmInputField.onEndEdit.AddListener(OnBGMInput);
        if (sfxInputField != null) sfxInputField.onEndEdit.AddListener(OnSFXInput);

        if (masterMuteToggle != null) masterMuteToggle.onValueChanged.AddListener(OnMasterMute);
        if (bgmMuteToggle != null) bgmMuteToggle.onValueChanged.AddListener(OnBGMMute);
        if (sfxMuteToggle != null) sfxMuteToggle.onValueChanged.AddListener(OnSFXMute);

        if (closeButton != null) closeButton.onClick.AddListener(CloseSettings);
    }

    private void UnregisterListeners()
    {
        if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(OnMasterSlider);
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(OnBGMSlider);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSFXSlider);

        if (masterInputField != null) masterInputField.onEndEdit.RemoveListener(OnMasterInput);
        if (bgmInputField != null) bgmInputField.onEndEdit.RemoveListener(OnBGMInput);
        if (sfxInputField != null) sfxInputField.onEndEdit.RemoveListener(OnSFXInput);

        if (masterMuteToggle != null) masterMuteToggle.onValueChanged.RemoveListener(OnMasterMute);
        if (bgmMuteToggle != null) bgmMuteToggle.onValueChanged.RemoveListener(OnBGMMute);
        if (sfxMuteToggle != null) sfxMuteToggle.onValueChanged.RemoveListener(OnSFXMute);

        if (closeButton != null) closeButton.onClick.RemoveListener(CloseSettings);
    }

    private void OnMasterSlider(float value)
    {
        if (_isInit) return;
        _masterVol = value;
        ApplyMasterVolume();
        SyncText(masterInputField, masterValueText, value);
        SaveSettings();
    }

    private void OnBGMSlider(float value)
    {
        if (_isInit) return;
        _bgmVol = value;
        ApplyBGMVolume();
        SyncText(bgmInputField, bgmValueText, value);
        SaveSettings();
    }

    private void OnSFXSlider(float value)
    {
        if (_isInit) return;
        _sfxVol = value;
        ApplySFXVolume();
        SyncText(sfxInputField, sfxValueText, value);
        SaveSettings();
    }

    private void OnMasterInput(string text)
    {
        if (!TryParsePercent(text, _masterVol, out _masterVol, masterInputField, masterValueText)) return;
        ApplyMasterVolume();
        SetSliderSilent(masterSlider, _masterVol);
        SaveSettings();
    }

    private void OnBGMInput(string text)
    {
        if (!TryParsePercent(text, _bgmVol, out _bgmVol, bgmInputField, bgmValueText)) return;
        ApplyBGMVolume();
        SetSliderSilent(bgmSlider, _bgmVol);
        SaveSettings();
    }

    private void OnSFXInput(string text)
    {
        if (!TryParsePercent(text, _sfxVol, out _sfxVol, sfxInputField, sfxValueText)) return;
        ApplySFXVolume();
        SetSliderSilent(sfxSlider, _sfxVol);
        SaveSettings();
    }

    private void OnMasterMute(bool muted)
    {
        if (_isInit) return;
        _muteMaster = muted;
        ApplyMasterVolume();
        SetInteractable(masterSlider, masterInputField, !muted);
        SaveSettings();
    }

    private void OnBGMMute(bool muted)
    {
        if (_isInit) return;
        _muteBGM = muted;
        ApplyBGMVolume();
        SetInteractable(bgmSlider, bgmInputField, !muted);
        SaveSettings();
    }

    private void OnSFXMute(bool muted)
    {
        if (_isInit) return;
        _muteSFX = muted;
        ApplySFXVolume();
        SetInteractable(sfxSlider, sfxInputField, !muted);
        SaveSettings();
    }

    private void ApplyAllSettings()
    {
        ApplyMasterVolume();
        ApplyBGMVolume();
        ApplySFXVolume();
    }

    private void ApplyMasterVolume()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVolume(AudioManager.Instance.masterParam, _muteMaster ? 0f : _masterVol);
        }
    }

    private void ApplyBGMVolume()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVolume(AudioManager.Instance.bgmParam, _muteBGM ? 0f : _bgmVol);
        }
    }

    private void ApplySFXVolume()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVolume(AudioManager.Instance.sfxParam, _muteSFX ? 0f : _sfxVol);
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void ApplyToUI()
    {
        _isInit = true;

        SetSliderSilent(masterSlider, _masterVol);
        SetSliderSilent(bgmSlider, _bgmVol);
        SetSliderSilent(sfxSlider, _sfxVol);

        SyncText(masterInputField, masterValueText, _masterVol);
        SyncText(bgmInputField, bgmValueText, _bgmVol);
        SyncText(sfxInputField, sfxValueText, _sfxVol);

        SetToggleSilent(masterMuteToggle, _muteMaster);
        SetToggleSilent(bgmMuteToggle, _muteBGM);
        SetToggleSilent(sfxMuteToggle, _muteSFX);

        SetInteractable(masterSlider, masterInputField, !_muteMaster);
        SetInteractable(bgmSlider, bgmInputField, !_muteBGM);
        SetInteractable(sfxSlider, sfxInputField, !_muteSFX);

        _isInit = false;
    }

    private void LoadSettings()
    {
        var sound = GameSettingsManager.Instance.Settings.sound;
        _masterVol = sound.masterVolume;
        _bgmVol = sound.bgmVolume;
        _sfxVol = sound.sfxVolume;
        _muteMaster = sound.muteMaster;
        _muteBGM = sound.muteBgm;
        _muteSFX = sound.muteSfx;
    }

    private void SaveSettings()
    {
        var sound = GameSettingsManager.Instance.Settings.sound;
        sound.masterVolume = _masterVol;
        sound.bgmVolume = _bgmVol;
        sound.sfxVolume = _sfxVol;
        sound.muteMaster = _muteMaster;
        sound.muteBgm = _muteBGM;
        sound.muteSfx = _muteSFX;
        GameSettingsManager.Instance.SaveSettings();
    }

    private bool TryParsePercent(string text, float fallback, out float value, TMP_InputField input, TMP_Text label)
    {
        if (!int.TryParse(text, out int percent))
        {
            value = fallback;
            SyncText(input, label, fallback);
            return false;
        }

        value = Mathf.Clamp01(percent / 100f);
        SyncText(input, label, value);
        return true;
    }

    private void SetSliderSilent(Slider slider, float value)
    {
        if (slider == null) return;
        _isInit = true;
        slider.value = value;
        _isInit = false;
    }

    private void SetToggleSilent(Toggle toggle, bool value)
    {
        if (toggle == null) return;
        _isInit = true;
        toggle.isOn = value;
        _isInit = false;
    }

    private void SyncText(TMP_InputField input, TMP_Text label, float value)
    {
        int pct = Mathf.RoundToInt(value * 100);
        if (input != null) input.text = pct.ToString();
        if (label != null) label.text = pct + "%";
    }

    private void SetInteractable(Slider slider, TMP_InputField input, bool interactable)
    {
        if (slider != null) slider.interactable = interactable;
        if (input != null) input.interactable = interactable;
    }
}
