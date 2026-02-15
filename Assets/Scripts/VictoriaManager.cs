using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoriaManager : MonoBehaviour
{
    // Función para el botón "MENÚ PRINCIPAL"
    public void VolverAlMenu()
    {
        // Carga la escena 0 (que es tu menú)
        SceneManager.LoadScene(0);
    }

    // Función para el botón "SALIR"
    public void SalirDelJuego()
    {
        Debug.Log("Cerrando juego...");
        Application.Quit();
    }
}