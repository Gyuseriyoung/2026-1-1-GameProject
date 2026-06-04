using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoundSettingsUI : MonoBehaviour
{
    private const string KEY_MASTER = "Vol_Master";
    private const string KEY_BGM = "Vol_BGM";
    private const string KEY_SFX = "Vol_SFX";
    private const string KEY_MUTE_MASTER = "Mute_Master";
    private const string KEY_MUTE_BGM = "Mute_BGM";
    private const string KEY_MUTE_SFX = "Mute_SFX";

    [Header("── 전체(Master)")]
    public Slider masterSlider;
    public TMP_InputField masterInputField;
    public TMP_Text masterValueText;
    public Toggle masterMuteToggle;

    [Header("── 배경음(BGM)")]
    public Slider bgmSlider;
    public TMP_InputField bgmInputField;
    public TMP_Text bgmValueText;
    public Toggle bgmMuteToggle;

    [Header("── 효과음(SFX)")]
    public Slider sfxSlider;
    public TMP_InputField sfxInputField;
    public TMP_Text sfxValueText;
    public Toggle sfxMuteToggle;

    [Header("── 공통")]
    public Button closeButton;
    public GameObject settingsPanel;

    private float _masterVol = 1f;
    private float _bgmVol = 0.5f;
    private float _sfxVol = 1f;
    private bool _muteMaster, _muteBGM, _muteSFX;
    private bool _isInit;

    // ------------------------------------------------

    private void Awake()
    {
        // PlayerPrefs 읽기 + AudioListener만 — BGMPlayer/SFXManager는 Start에서
        _masterVol = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        _bgmVol = PlayerPrefs.GetFloat(KEY_BGM, 0.5f);
        _sfxVol = PlayerPrefs.GetFloat(KEY_SFX, 1f);
        _muteMaster = PlayerPrefs.GetInt(KEY_MUTE_MASTER, 0) == 1;
        _muteBGM = PlayerPrefs.GetInt(KEY_MUTE_BGM, 0) == 1;
        _muteSFX = PlayerPrefs.GetInt(KEY_MUTE_SFX, 0) == 1;

        AudioListener.volume = _muteMaster ? 0f : _masterVol;
    }

    private void Start()
    {
        ApplyBGMVolume();
        ApplySFXVolume();
        RegisterListeners();
        ApplyToUI();
    }

    private void OnDestroy()
    {
        // RemoveAllListeners를 쓰지 않으므로 이 한 번만 호출해도 완전히 해제됨
        UnregisterListeners();
    }

    // ── 리스너 등록 / 해제 ──────────────────────────

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

    // ── 슬라이더 콜백 ───────────────────────────────

    private void OnMasterSlider(float value)
    {
        if (_isInit) return;
        _masterVol = value;
        ApplyMasterVolume();
        SyncText(masterInputField, masterValueText, value);
        PlayerPrefs.SetFloat(KEY_MASTER, value);
    }

    private void OnBGMSlider(float value)
    {
        if (_isInit) return;
        _bgmVol = value;
        ApplyBGMVolume();
        SyncText(bgmInputField, bgmValueText, value);
        PlayerPrefs.SetFloat(KEY_BGM, value);
    }

    private void OnSFXSlider(float value)
    {
        if (_isInit) return;
        _sfxVol = value;
        ApplySFXVolume();
        SyncText(sfxInputField, sfxValueText, value);
        PlayerPrefs.SetFloat(KEY_SFX, value);
    }

    // ── InputField 콜백 ─────────────────────────────

    private void OnMasterInput(string text)
    {
        if (!int.TryParse(text, out int val)) { SyncText(masterInputField, masterValueText, _masterVol); return; }
        _masterVol = Mathf.Clamp01(val / 100f);
        ApplyMasterVolume();
        SetSliderSilent(masterSlider, _masterVol);
        SyncText(masterInputField, masterValueText, _masterVol);
        PlayerPrefs.SetFloat(KEY_MASTER, _masterVol);
    }

    private void OnBGMInput(string text)
    {
        if (!int.TryParse(text, out int val)) { SyncText(bgmInputField, bgmValueText, _bgmVol); return; }
        _bgmVol = Mathf.Clamp01(val / 100f);
        ApplyBGMVolume();
        SetSliderSilent(bgmSlider, _bgmVol);
        SyncText(bgmInputField, bgmValueText, _bgmVol);
        PlayerPrefs.SetFloat(KEY_BGM, _bgmVol);
    }

    private void OnSFXInput(string text)
    {
        if (!int.TryParse(text, out int val)) { SyncText(sfxInputField, sfxValueText, _sfxVol); return; }
        _sfxVol = Mathf.Clamp01(val / 100f);
        ApplySFXVolume();
        SetSliderSilent(sfxSlider, _sfxVol);
        SyncText(sfxInputField, sfxValueText, _sfxVol);
        PlayerPrefs.SetFloat(KEY_SFX, _sfxVol);
    }

    // ── 음소거 토글 콜백 ────────────────────────────

    private void OnMasterMute(bool muted)
    {
        if (_isInit) return;
        _muteMaster = muted;
        ApplyMasterVolume();
        SetInteractable(masterSlider, masterInputField, !muted);
        PlayerPrefs.SetInt(KEY_MUTE_MASTER, muted ? 1 : 0);
    }

    private void OnBGMMute(bool muted)
    {
        if (_isInit) return;
        _muteBGM = muted;
        ApplyBGMVolume();
        SetInteractable(bgmSlider, bgmInputField, !muted);
        PlayerPrefs.SetInt(KEY_MUTE_BGM, muted ? 1 : 0);
    }

    private void OnSFXMute(bool muted)
    {
        if (_isInit) return;
        _muteSFX = muted;
        ApplySFXVolume();
        SetInteractable(sfxSlider, sfxInputField, !muted);
        PlayerPrefs.SetInt(KEY_MUTE_SFX, muted ? 1 : 0);
    }

    // ── 볼륨 적용 ───────────────────────────────────

    private void ApplyMasterVolume()
    {
        AudioListener.volume = _muteMaster ? 0f : _masterVol;
    }

    private void ApplyBGMVolume()
    {
        var bgm = FindFirstObjectByType<BGMPlayer>();
        if (bgm != null) bgm.SetVolume(_muteBGM ? 0f : _bgmVol);
    }

    private void ApplySFXVolume()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.SetMasterVolume(_muteSFX ? 0f : _sfxVol);
    }

    // ── 설정창 열기 / 닫기 ──────────────────────────

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        PlayerPrefs.Save();
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // ── UI 초기화 ───────────────────────────────────

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

    // ── 유틸 ────────────────────────────────────────

    // ★ 핵심 수정: RemoveAllListeners() 제거 → _isInit 플래그만 사용
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