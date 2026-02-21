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
    [SerializeField] private TMP_InputField nameInputField;
    
    [Header("Settings")]
    [SerializeField] private string leaderboardKey = "statskey";

    private void Start()
    {
        // Start session zůstává stejný, aby hráč mohl být v tabulce
        LootLockerSDKManager.StartGuestSession((response) =>
        {
            if (response.success) Debug.Log("LootLocker: Relace spuštěna");
        });
    }

    public void OnClickInitialSave()
    {
        saveStartButton.SetActive(false);
        inputGroup.SetActive(true);
    }

    public void OnClickConfirmSave()
    {
        string playerName = nameInputField.text;
        if (string.IsNullOrEmpty(playerName)) return;

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
                if (sRes.success)
                {
                    LootLockerSDKManager.SetPlayerName(playerName, (nRes) =>
                    {
                        statusGroup.SetActive(true);
                    });
                }
            });
        }
        else
        {
            // Pokud jsi dal míň než svůj rekord, serveru se ani nedotkneme
            Debug.Log("[Leaderboard] Skóre je horší než tvůj rekord. Nic neposílám.");
            statusGroup.SetActive(true);
        }
    }

    public void OnClickGoToStats()
    {
        SceneManager.LoadScene("Statistic");
    }
}