using UnityEngine;
using LootLocker.Requests;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class LeaderboardDisplay : MonoBehaviour
{
    private const int MaxNameLength = 14;

    [Header("LootLocker Settings")]
    public string leaderboardKey = "statskey";

    [Header("UI Elements")]
    public TextMeshProUGUI[] nameTexts;  // Přiřaď 10 textů pro jména
    public TextMeshProUGUI[] scoreTexts; // Přiřaď 10 textů pro skóre
    public GameObject noInternetGroup;   // Zobrazí se, když se leaderboard nepodaří načíst
    public GameObject placeGroup;        // Napr. objekt s nadpisem/sloupcem Place
    [SerializeField] private GameObject buttonsUI;

    [Header("Session Save UI")]
    [SerializeField] private Text sessionMaxScoreText;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private GameObject saveControlsGroup;
    [SerializeField] private GameObject saveSuccessGroup;
    [SerializeField] private GameObject saveErrorGroup;

    private string myPlayerID = "";
    private bool isLeaderboardSessionReady;
    private int sessionMaxScore;

    void Start()
    {
        if (nameInputField != null)
        {
            nameInputField.characterLimit = MaxNameLength;
        }

        sessionMaxScore = PlayerPrefs.GetInt("LastScore", 0);
        UpdateSessionScoreText();
        SetSaveUiState(false, false, false);
        if (buttonsUI != null)
        {
            buttonsUI.SetActive(false);
        }

        // 1. Přihlášení a získání ID, abychom věděli, který řádek v tabulce jsi ty
        LootLockerSDKManager.StartGuestSession((response) =>
        {
            if (response.success)
            {
                isLeaderboardSessionReady = true;
                ShowNoInternet(false);
                myPlayerID = response.player_id.ToString();
                if (buttonsUI != null)
                {
                    buttonsUI.SetActive(true);
                }
                if (saveControlsGroup != null)
                {
                    saveControlsGroup.SetActive(true);
                }
                RefreshLeaderboard();
            }
            else
            {
                isLeaderboardSessionReady = false;
                Debug.LogError("LootLocker: Selhalo přihlášení");
                ShowNoInternet(true);
                if (buttonsUI != null)
                {
                    buttonsUI.SetActive(false);
                }
                if (saveControlsGroup != null)
                {
                    saveControlsGroup.SetActive(false);
                }
            }
        });
    }

    // Metoda pro načtení a zobrazení tabulky
    public void RefreshLeaderboard()
    {
        // Zjistíme tvůj lokální rekord z mobilu
        int localBest = PlayerPrefs.GetInt("LocalHighScore", 0);

        LootLockerSDKManager.GetScoreList(leaderboardKey, 10, 0, (response) =>
        {
            if (response.success && response.items != null)
            {
                ShowNoInternet(false);

                // Nejdřív všechna textová pole vyčistíme (příprava na posun)
                for (int i = 0; i < nameTexts.Length; i++)
                {
                    nameTexts[i].text = "---";
                    scoreTexts[i].text = "";
                }

                int uiIndex = 0; // Index pro řádky v UI (0 až 9)
                
                for (int i = 0; i < response.items.Length; i++)
                {
                    var item = response.items[i];
                    bool isMe = item.player.id.ToString() == myPlayerID;

                    // FILTR: Pokud jsi to ty a máš v mobilu smazáno (0), nebo má kdokoli jiný 0, přeskočíme ho
                    if ((isMe && localBest <= 0) || item.score <= 0)
                    {
                        continue; // Přeskočí hráče a NEZVÝŠÍ uiIndex (tím vznikne posun)
                    }

                    // Pokud hráč prošel filtrem, zapíšeme ho do dalšího volného řádku v UI
                    if (uiIndex < nameTexts.Length)
                    {
                        string playerName = (item.player != null && !string.IsNullOrEmpty(item.player.name)) 
                                            ? item.player.name : "Neznámý";

                        nameTexts[uiIndex].text = playerName;
                        scoreTexts[uiIndex].text = item.score.ToString();
                        uiIndex++; // Posuneme se na další řádek v UI
                    }
                }
                Debug.Log("[Leaderboard] Tabulka aktualizována a posunuta.");
            }
            else
            {
                Debug.LogError("[Leaderboard] Nepodařilo se načíst statistiky (pravděpodobně není připojení k internetu).");
                ShowNoInternet(true);
            }
        });
    }

    public void SaveSessionMaxScore()
    {
        if (!isLeaderboardSessionReady)
        {
            ShowSaveError();
            Debug.LogWarning("[Leaderboard] Ukládání zablokováno: není aktivní LootLocker session.");
            return;
        }

        if (sessionMaxScore <= 0)
        {
            ShowSaveError();
            Debug.LogWarning("[Leaderboard] Session max skóre je 0, není co ukládat.");
            return;
        }

        if (nameInputField == null || string.IsNullOrWhiteSpace(nameInputField.text))
        {
            ShowSaveError();
            Debug.LogWarning("[Leaderboard] Vyplň jméno před uložením.");
            return;
        }

        string sanitizedName = SanitizePlayerName(nameInputField.text);
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            ShowSaveError();
            Debug.LogWarning("[Leaderboard] Jméno může obsahovat jen písmena anglické abecedy, čísla a mezery.");
            return;
        }

        int localBest = PlayerPrefs.GetInt("LocalHighScore", 0);
        if (sessionMaxScore <= localBest)
        {
            ShowSaveError();
            Debug.LogWarning("[Leaderboard] Session skóre není vyšší než lokální maximum (" + localBest + "), není co uploadovat.");
            return;
        }

        string playerName = sanitizedName;
        nameInputField.text = sanitizedName;
        SetSaveUiState(true, false, false);

        LootLockerSDKManager.SubmitScore("", sessionMaxScore, leaderboardKey, (sRes) =>
        {
            if (!sRes.success)
            {
                ShowSaveError();
                Debug.LogError("[Leaderboard] Nepodařilo se odeslat skóre.");
                return;
            }

            LootLockerSDKManager.SetPlayerName(playerName, (nRes) =>
            {
                if (!nRes.success)
                {
                    ShowSaveError();
                    Debug.LogError("[Leaderboard] Skóre se uložilo, ale nepodařilo se uložit jméno.");
                    return;
                }

                int localBest = PlayerPrefs.GetInt("LocalHighScore", 0);
                if (sessionMaxScore > localBest)
                {
                    PlayerPrefs.SetInt("LocalHighScore", sessionMaxScore);
                    PlayerPrefs.Save();
                }

                SetSaveUiState(false, true, false);
                RefreshLeaderboard();
            });
        });
    }

    private void ShowNoInternet(bool show)
    {
        if (noInternetGroup != null)
        {
            noInternetGroup.SetActive(show);
        }

        if (placeGroup != null)
        {
            placeGroup.SetActive(!show);
        }
    }

    private void UpdateSessionScoreText()
    {
        if (sessionMaxScoreText != null)
        {
            sessionMaxScoreText.text = "Max skóre session: " + sessionMaxScore;
        }
    }

    private void SetSaveUiState(bool showControls, bool showSuccess, bool showError)
    {
        if (saveControlsGroup != null)
        {
            saveControlsGroup.SetActive(showControls);
        }

        if (saveSuccessGroup != null)
        {
            saveSuccessGroup.SetActive(showSuccess);
        }

        if (saveErrorGroup != null)
        {
            saveErrorGroup.SetActive(showError);
        }
    }

    private void ShowSaveError()
    {
        if (saveErrorGroup != null)
        {
            saveErrorGroup.SetActive(true);
        }
    }

    private string SanitizePlayerName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
        {
            return string.Empty;
        }

        string lettersNumbersAndSpacesOnly = Regex.Replace(rawName, "[^A-Za-z0-9 ]", string.Empty);
        string normalizedSpaces = Regex.Replace(lettersNumbersAndSpacesOnly, "\\s+", " ").Trim();

        if (normalizedSpaces.Length > MaxNameLength)
        {
            normalizedSpaces = normalizedSpaces.Substring(0, MaxNameLength).TrimEnd();
        }

        return normalizedSpaces;
    }

    public void LoadMenuFromStatistics()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    // Metoda pro tlačítko "Smazat"
    public void DeleteMyScore()
    {
        Debug.Log("[Leaderboard] Mažu rekord...");

        // 1. Okamžité smazání v mobilu (tohle aktivuje filtr v RefreshLeaderboard)
        // Pozn: LastScore NEMAZAT - to je session max, který zůstává, dokud se hra nerestaruje
        PlayerPrefs.DeleteKey("LocalHighScore");
        PlayerPrefs.Save();

        // 2. Okamžité překreslení UI - díky filtru zmizíš hned a tabulka se posune
        RefreshLeaderboard();

        // 3. Úklid na serveru na pozadí (aby to tam časem nezavazelo)
        LootLockerSDKManager.SubmitScore("", 0, leaderboardKey, (sRes) =>
        {
            if (sRes.success)
            {
                LootLockerSDKManager.SetPlayerName("---", (nRes) => 
                {
                    Debug.Log("[Leaderboard] Server vyčištěn na pozadí.");
                });
            }
        });
    }
}