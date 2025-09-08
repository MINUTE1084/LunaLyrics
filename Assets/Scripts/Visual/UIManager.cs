using System.Runtime.InteropServices;
using LunaLyrics.Data;
using TMPro;
using UnityEngine;
using Zenject;

public class UIManager : MonoBehaviour
{
    [Inject] private readonly SignalBus signalBus;

    [Header("Title")]
    [SerializeField] private Animator titleAnimator;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI artistText;

    private void Start()
    {
        signalBus.Subscribe<UpdateMediaSignal>(SetTitle); // 미디어 업데이트 이벤트 구독
    }

    private void SetTitle(UpdateMediaSignal signal)
    {
        titleText.text = signal.title;
        artistText.text = signal.artist;

        titleAnimator.Play("Show");
    }
}
