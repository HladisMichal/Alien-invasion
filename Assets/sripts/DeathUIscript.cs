using System;
using UnityEngine;
using TMPro;
using LootLocker.Requests; 
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

public class DeathUIManager : MonoBehaviour
{
    private const int MaxNameLength = 14;

    [Header("UI Elements")]
    [SerializeField] private GameObject saveStartButton;
    [SerializeField] private GameObject inputGroup;
    [SerializeField] private GameObject statusGroup;
    [SerializeField] private GameObject connectionErrorGroup;
    [SerializeField] private GameObject lowerScoreGroup;
    [SerializeField] private TMP_InputField nameInputField;
    
    [Header("Settings")]
    [SerializeField] private string leaderboardKey = "statskey";

    private bool isLeaderboardSessionReady;

    private void Start()
    {
        if (nameInputField != null)
        {
            nameInputField.characterLimit = MaxNameLength;
        }

        // Start session zůstává stejný, aby hráč mohl být v tabulce
        LootLockerSDKManager.StartGuestSession((response) =>
        {
            isLeaderboardSessionReady = response.success;
            if (response.success) Debug.Log("LootLocker: Relace spuštěna");
        });
    }

    public void OnClickInitialSave()
    {
        saveStartButton.SetActive(false);

        int currentScore = PlayerPrefs.GetInt("LastScore", 0);
        int localBest = PlayerPrefs.GetInt("LocalHighScore", 0);

        if (currentScore <= localBest)
        {
            ShowOnlyGroup(lowerScoreGroup);
            return;
        }

        if (isLeaderboardSessionReady)
        {
            ShowOnlyGroup(inputGroup);
            return;
        }

        LootLockerSDKManager.StartGuestSession((response) =>
        {
            isLeaderboardSessionReady = response.success;

            if (response.success)
            {
                ShowOnlyGroup(inputGroup);
            }
            else
            {
                ShowOnlyGroup(connectionErrorGroup);
            }
        });
    }

    public void OnClickConfirmSave()
    {
        string playerName = nameInputField != null ? SanitizePlayerName(nameInputField.text) : string.Empty;
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("[Leaderboard] Jméno může obsahovat jen písmena anglické abecedy, čísla a mezery (max 14 znaků).");
            return;
        }

        if (nameInputField != null)
        {
            nameInputField.text = playerName;
        }

        if (!isLeaderboardSessionReady)
        {
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                isLeaderboardSessionReady = response.success;

                if (!response.success)
                {
                    Debug.LogError("[Leaderboard] Ukládání selhalo: nepodařilo se připojit k leaderboardu při kliknutí na Uložit.");
                    ShowOnlyGroup(connectionErrorGroup);
                    return;
                }

                ProcessSave(playerName);
            });
            return;
        }

        ProcessSave(playerName);
    }

    private void ProcessSave(string playerName)
    {
        inputGroup.SetActive(false);
        int currentScore = PlayerPrefs.GetInt("LastScore", 0);
        int localBest = PlayerPrefs.GetInt("LocalHighScore", 0);
        if (currentScore > localBest)
        {
            PlayerPrefs.SetInt("LocalHighScore", currentScore);
            PlayerPrefs.Save();
            LootLockerSDKManager.SubmitScore("", currentScore, leaderboardKey, (sRes) =>
            {
                if (!sRes.success)
                {
                    ShowOnlyGroup(connectionErrorGroup);
                    return;
                }
                LootLockerSDKManager.SetPlayerName(playerName, (nRes) =>
                {
                    ShowOnlyGroup(statusGroup);
                });
            });
        }
    }

    private void ShowOnlyGroup(GameObject groupToShow)
    {
        inputGroup.SetActive(false);
        statusGroup.SetActive(false);

        if (connectionErrorGroup != null)
        {
            connectionErrorGroup.SetActive(false);
        }

        if (lowerScoreGroup != null)
        {
            lowerScoreGroup.SetActive(false);
        }

        if (groupToShow != null)
        {
            groupToShow.SetActive(true);
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

    public void OnClickGoToStats()
    {
        GameObject gameMusic = GameObject.Find("MusicManagerGame");
        GameObject menuMusic = GameObject.Find("MusicManagerMenu");

        if (gameMusic != null && menuMusic == null)
        {
            MusicManager.Instance = null;
            Destroy(gameMusic);
            Time.timeScale = 1f;
            SceneManagerscript.PendingSceneAfterMenu = "Statistic";
            SceneManager.LoadScene("Menu");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene("Statistic");
    }
}