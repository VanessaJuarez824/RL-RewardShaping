using UnityEngine;
using UnityEngine.SceneManagement;

public class CambiaEscenas : MonoBehaviour
{
    public void ChangeScene(string sceneName)
    {
        if (sceneName == "quit")
        {
            QuitGame();
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private void QuitGame()
    {
        Debug.Log("Saliendo del juego...");  // Se verá en el editor, no en la app compilada.
        Application.Quit();
    }
}

