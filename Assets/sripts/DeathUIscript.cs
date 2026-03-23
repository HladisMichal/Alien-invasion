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

    public void OnClickGoToStats()
    {
        SceneManager.LoadScene("Statistic");
    }
}