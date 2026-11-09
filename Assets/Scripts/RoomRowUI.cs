using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Button joinButton;

    public void Setup(RoomInfo info, Action<string> onJoin)
    {
        label.text = $"{info.name}   {info.players}/{RoomNetworkManager.maxPlayersPerRoom}";
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => onJoin(info.code));  
    }
}
