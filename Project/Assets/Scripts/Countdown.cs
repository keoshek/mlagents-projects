using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Countdown : MonoBehaviour
{
    [Tooltip("In Seconds")] [SerializeField] private float timeLeft = 0;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private UnityEvent onEnd;


    private float countdown;


    public void ResetTimer()
    {
        countdown = timeLeft;
    }


    void Update()
    {
        if (countdown <= 0)
            return;

        TimeSpan time = new(0, 0, (int)countdown);

        if (countdown > 86400) // 86400 = 1D
            text.text = time.Days.ToString("0") + "D " + time.Hours.ToString("0") + "H";
        else if (countdown > 3600) // 3600 = 1h
            text.text = time.Hours.ToString("0") + "H " + time.Minutes.ToString("0") + "m";//countdown.Hours.ToString("00") + ':' + countdown.Minutes.ToString("00") + ':' + countdown.Seconds.ToString("00");
        else
            text.text = time.Minutes.ToString("00") + ':' + time.Seconds.ToString("00");

        countdown -= Time.deltaTime;
        if (countdown <= 0)
            onEnd?.Invoke();
    }
}
