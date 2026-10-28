
using System;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button disconnectButton;

    public event Action OnStartHost;
    public event Action OnStartClient;
    public event Action OnDiconnectClient;

    private void Start()
    {
        hostButton.onClick.AddListener(() => OnStartHost?.Invoke());
        clientButton.onClick.AddListener(() => OnStartClient?.Invoke());
        disconnectButton.onClick.AddListener(() => OnDiconnectClient?.Invoke());

        EnableButtons();
    }

    public void DisableButtons()
    {
        hostButton.interactable = false;
        clientButton.interactable = false;
        disconnectButton.interactable = true;
    }

    public void EnableButtons()
    {
        hostButton.interactable = true;
        clientButton.interactable = true;
        disconnectButton.interactable = false;
    }
}
