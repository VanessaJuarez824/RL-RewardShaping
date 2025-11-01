using UnityEngine;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    public float startTime = 86400f; // Tiempo inicial en segundos
    private float timeRemaining;
    public TextMeshProUGUI timerText; // Referencia al TextMeshPro en UI

    private bool isRunning = true; // Control para iniciar/detener el timer

    // Factor de aceleración del tiempo
    public float timeScale = 10f; // 10 significa que el tiempo pasará 10 veces más rápido

    void Start()
    {
        timeRemaining = startTime;
        UpdateTimerText();
    }

    void Update()
    {
        if (isRunning && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime * timeScale; // Acelera el paso del tiempo
            if (timeRemaining < 0) 
                timeRemaining = 0; // Evita valores negativos
            UpdateTimerText();
        }
        else if (timeRemaining <= 0 && isRunning)
        {
            timeRemaining = 0;
            isRunning = false;
            TimerFinished();
            UpdateTimerText(); // Asegurar que muestre 00:00:00
        }
    }

    void UpdateTimerText()
    {
        int hours = Mathf.Max(0, Mathf.FloorToInt(timeRemaining / 3600)); // Evita valores negativos
        int minutes = Mathf.Max(0, Mathf.FloorToInt((timeRemaining % 3600) / 60));
        int seconds = Mathf.Max(0, Mathf.FloorToInt(timeRemaining % 60));

        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
    }

    void TimerFinished()
    {
        Debug.Log("¡Tiempo agotado!");
        // lógica adicional cuando el tiempo llegue a 0.
    }

    public void ResetTimer()
    {
        timeRemaining = startTime;
        isRunning = true;
        UpdateTimerText(); // Asegurar que se reinicia correctamente
    }
}
