using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerData", menuName = "Player Data", order = 1)]
public class PlayerData : ScriptableObject
{
    // Add player-related properties here
    public string playerName;
    public int playerLevel;
    public float playerHealth;
    // Add more as needed
    [SerializeField]
    public CursorController _cursor;
}