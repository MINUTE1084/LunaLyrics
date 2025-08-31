using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LunaLyrics.Util
{
    public class TriggerHelper : MonoBehaviour
    {
        [SerializeField] private List<UnityEvent> eventList = new();

        public void RunEvent(int index)
        {
            eventList[index].Invoke();
        }
    }
}
