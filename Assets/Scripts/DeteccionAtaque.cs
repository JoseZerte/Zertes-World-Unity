using UnityEngine;

public class DeteccionAtaque : MonoBehaviour
{
    public int daño = 1; // Cuánta vida quita el espadazo

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. Buscamos si lo que hemos tocado tiene el script "EnemigoBase"
        EnemigoBase enemigo = collision.GetComponent<EnemigoBase>();

        // 2. Si tiene el script, significa que es un enemigo vivo
        if (enemigo != null)
        {
            // Le hacemos daño
            enemigo.RecibirDaño(daño);
        }
    }
}