using System;
using UnityEngine;
using TMPro;
using LootLocker.Requests; 
using UnityEngine.SceneManagement;

public class DeathUIManager : MonoBehaviour
{
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
            Debug.Log("[Leaderboard] Skóre se neukládá, protože už máš lepší výsledek.");
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
                Debug.LogError("[Leaderboard] Selhalo připojení ke statistikám (pravděpodobně chybí internet)." );
                ShowOnlyGroup(connectionErrorGroup);
            }
        });
    }

    public void OnClickConfirmSave()
    {
        string playerName = nameInputField.text;
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("[Leaderboard] Jméno je prázdné, ukládání bylo zrušeno.");
            return;
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
        // Načte rekord uložený v mobilu/PC
        int localBest = PlayerPrefs.GetInt("LocalHighScore", 0);

        Debug.Log($"[Leaderboard] Aktuální skóre: {currentScore}, Místní rekord: {localBest}");

        // KONTROLA: Na server jdeme jen tehdy, když jsme se zlepšili oproti rekordu v mobilu
        if (currentScore > localBest)
        {
            Debug.Log("[Leaderboard] NOVÝ REKORD! Ukládám lokálně i na server.");
            
            // 1. Uložíme nový rekord do paměti mobilu
            PlayerPrefs.SetInt("LocalHighScore", currentScore);
            PlayerPrefs.Save();

            // 2. Pošleme na server (teď je jedno, že tam je Always Overwrite, protože posíláme rekord)
            LootLockerSDKManager.SubmitScore("", currentScore, leaderboardKey, (sRes) =>
            {
                if (!sRes.success)
                {
                    Debug.LogWarning("[Leaderboard] Skóre se neuložilo na server. Pravděpodobně už máš v tabulce lepší výsledek, nebo došlo k chybě připojení.");
                    ShowOnlyGroup(connectionErrorGroup);
                    return;
                }

                LootLockerSDKManager.SetPlayerName(playerName, (nRes) =>
                {
                    if (!nRes.success)
                    {
                        Debug.LogError("[Leaderboard] Skóre bylo uloženo, ale nepodařilo se uložit jméno hráče.");
                    }

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

    public void OnClickGoToStats()
    {
        SceneManager.LoadScene("Statistic");
    }
}