using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Main Menu Buttons")]
    [SerializeField] private Button playGameButton;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;

    [Header("Pause Menu Buttons")]
    [SerializeField] private Button openPauseMenuButton;
    [SerializeField] private Button closePauseMenuButton;

    [Header("Panels")]
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject pauseMenuPanel;

    private GameObject lastActivePanel;

    void Start()
    {
        // Butonlara metodları bağla
        playGameButton.onClick.AddListener(PlayGame);
        howToPlayButton.onClick.AddListener(ShowHowToPlay);
        settingsButton.onClick.AddListener(ShowSettings);
        creditsButton.onClick.AddListener(ShowCredits);
        
        if (openPauseMenuButton != null) openPauseMenuButton.onClick.AddListener(OpenPauseMenu);
        if (closePauseMenuButton != null) closePauseMenuButton.onClick.AddListener(ClosePauseMenu);

        // Başlangıçta tüm panelleri kapat
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    // Play Game butonu - "Game" sahnesine geçiş
    private void PlayGame()
    {
        SceneManager.LoadScene("Game");
    }

    // How To Play panelini aç
    private void ShowHowToPlay()
    {
        CloseLastPanel();
        howToPlayPanel.SetActive(true);
        lastActivePanel = howToPlayPanel;
    }

    // Settings panelini aç
    private void ShowSettings()
    {
        CloseLastPanel();
        settingsPanel.SetActive(true);
        lastActivePanel = settingsPanel;
    }

    // Credits panelini aç
    private void ShowCredits()
    {
        CloseLastPanel();
        creditsPanel.SetActive(true);
        lastActivePanel = creditsPanel;
    }

    // Son açılan paneli kapat
    public void CloseLastPanel()
    {
        if (lastActivePanel != null)
        {
            lastActivePanel.SetActive(false);
            lastActivePanel = null;
        }
    }

    // Pause Menu aç
    public void OpenPauseMenu()
    {
        Debug.Log("OpenPauseMenu çağrıldı");
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            Time.timeScale = 0f; // Oyunu duraklat
            Debug.Log("Pause menu açıldı");
        }
        else
        {
            Debug.LogError("pauseMenuPanel atanmamış!");
        }
    }

    // Pause Menu kapat
    public void ClosePauseMenu()
    {
        Debug.Log("ClosePauseMenu çağrıldı");
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            Time.timeScale = 1f; // Oyunu devam ettir
            Debug.Log("Pause menu kapatıldı");
        }
        else
        {
            Debug.LogError("pauseMenuPanel atanmamış!");
        }
    }

    void Update()
    {
        
    }
}