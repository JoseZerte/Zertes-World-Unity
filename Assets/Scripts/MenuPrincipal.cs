using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cargar escenas

public class MenuPrincipal : MonoBehaviour
{
    public void Jugar()
    {
        // Carga la siguiente escena en la lista (la del juego)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void Salir()
    {
        Debug.Log("Saliendo...");
        Application.Quit();
    }
}
