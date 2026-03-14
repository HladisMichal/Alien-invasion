using UnityEngine;
using LootLocker.Requests;
using TMPro;

public class LeaderboardDisplay : MonoBehaviour
{
    [Header("LootLocker Settings")]
    public string leaderboardKey = "statskey";

    [Header("UI Elements")]
    public TextMeshProUGUI[] nameTexts;  // Přiřaď 10 textů pro jména
    public TextMeshProUGUI[] scoreTexts; // Přiřaď 10 textů pro skóre
    public GameObject noInternetGroup;   // Zobrazí se, když se leaderboard nepodaří načíst
    public GameObject placeGroup;        // Napr. objekt s nadpisem/sloupcem Place

    private string myPlayerID = "";

    void Start()
    {
        // 1. Přihlášení a získání ID, abychom věděli, který řádek v tabulce jsi ty
        LootLockerSDKManager.StartGuestSession((response) =>
        {
            if (response.success)
            {
                ShowNoInternet(false);
                myPlayerID = response.player_id.ToString();
                RefreshLeaderboard();
            }
            else
            {
                Debug.LogError("LootLocker: Selhalo přihlášení");
                ShowNoInternet(true);
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

    // Metoda pro tlačítko "Smazat"
    public void DeleteMyScore()
    {
        Debug.Log("[Leaderboard] Mažu rekord...");

        // 1. Okamžité smazání v mobilu (tohle aktivuje filtr v RefreshLeaderboard)
        PlayerPrefs.DeleteKey("LocalHighScore");
        PlayerPrefs.DeleteKey("LastScore");
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