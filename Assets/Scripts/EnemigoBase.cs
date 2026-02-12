using UnityEngine;
using System.Collections;

public class EnemigoBase : MonoBehaviour
{
    [Header("Vida")]
    public int vidaActual = 3;
    public int vidaMaxima = 3;

    [Header("Patrulla")]
    public float velocidad = 2f;
    public Transform detectorSuelo; 
    public float distanciaAbajo = 1.5f; 
    public float distanciaFrente = 0.2f;
    public LayerMask capaSuelo; 

    private Rigidbody2D rb;
    private Animator anim; // <--- AQUÍ ESTÁ LA CLAVE
    private bool mirandoDerecha = true;
    private bool estaMuerto = false;
    private bool puedeGirar = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>(); // <--- CONECTAMOS EL ANIMATOR
        vidaActual = vidaMaxima;
        
        if(rb != null) rb.freezeRotation = true;
        Physics2D.queriesStartInColliders = false;
    }

    void Update()
    {
        if (estaMuerto) return;

        // Movimiento
        rb.linearVelocity = new Vector2(velocidad * (mirandoDerecha ? 1 : -1), rb.linearVelocity.y);

        // Solo mandamos datos si el animator está activo y funcionando
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.SetBool("isWalking", true);
        }

        // Detección de suelo y paredes
        RaycastHit2D hitSuelo = Physics2D.Raycast(detectorSuelo.position, Vector2.down, distanciaAbajo, capaSuelo);
        Vector2 direccion = mirandoDerecha ? Vector2.right : Vector2.left;
        RaycastHit2D hitPared = Physics2D.Raycast(detectorSuelo.position, direccion, distanciaFrente, capaSuelo);

        if (puedeGirar && (hitSuelo.collider == null || hitPared.collider != null))
        {
            StartCoroutine(GirarCooldown());
        }
    }

    IEnumerator GirarCooldown()
    {
        puedeGirar = false;
        mirandoDerecha = !mirandoDerecha;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        yield return new WaitForSeconds(0.5f); 
        puedeGirar = true;
    }

    public void RecibirDaño(int cantidad)
    {
        if (estaMuerto) return;

        vidaActual -= cantidad;

        // SI SIGUE VIVO, lanza la animación de golpe
        if (vidaActual > 0)
        {
            if (anim != null)
            {
                anim.SetTrigger("golpe"); // Esto activa la animación de Hit
            }
            Debug.Log("¡Ay! Al enemigo le quedan " + vidaActual + " puntos de vida.");
        }
        else
        {
            Morir();
        }
    }

    void Morir()
    {
        if (estaMuerto) return;
        estaMuerto = true;

        // Lanzamos la animación de muerte
        if (anim != null)
        {
            anim.SetBool("isWalking", false);
            anim.SetTrigger("Muerte");
        }

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;
        GetComponent<Collider2D>().enabled = false;

        // Tiempo suficiente para que se vea la animación antes de borrar el objeto
        Destroy(gameObject, 1.2f); 
    }

    private void OnDrawGizmos()
    {
        if (detectorSuelo == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(detectorSuelo.position, detectorSuelo.position + Vector3.down * distanciaAbajo);
    }
    
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 1. Lanzamos la animación de ataque
            if (anim != null)
            {
                anim.SetTrigger("ataque");
            }

            // 2. Le quitamos vida al jugador (lo que ya tenías)
            var vida = collision.gameObject.GetComponent<PlayerController>();
            if (vida != null)
            {
                // vida.RecibirDaño(1);
                Debug.Log("¡El esqueleto te ha golpeado con animación!");
            }
        
            // OPCIONAL: Un pequeño empujón al caballero para que no se queden pegados
            Rigidbody2D rbPlayer = collision.gameObject.GetComponent<Rigidbody2D>();
            if (rbPlayer != null)
            {
                Vector2 direccionEmpuje = (collision.transform.position - transform.position).normalized;
                rbPlayer.AddForce(direccionEmpuje * 5f, ForceMode2D.Impulse);
            }
        }
    }
    
}