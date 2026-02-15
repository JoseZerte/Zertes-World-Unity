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
    private Animator anim;
    private bool mirandoDerecha = true;
    private bool estaMuerto = false;
    private bool puedeGirar = true;
    
    // Variable para controlar si está atacando
    private bool estaAtacando = false; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        vidaActual = vidaMaxima;

        if(rb != null) rb.freezeRotation = true;
        Physics2D.queriesStartInColliders = false;
    }

    void Update()
    {
        // Si el script está desactivado o muerto, no hacemos nada (Seguridad extra)
        if (estaMuerto) return;

        // Si está atacando, paramos el movimiento y salimos
        if (estaAtacando) 
        {
            rb.linearVelocity = Vector2.zero; 
            return; 
        }

        // Movimiento de Patrulla
        rb.linearVelocity = new Vector2(velocidad * (mirandoDerecha ? 1 : -1), rb.linearVelocity.y);

        if (anim != null)
        {
            anim.SetBool("isWalking", true);
        }

        // Detección de bordes y paredes para girar
        RaycastHit2D hitSuelo = Physics2D.Raycast(detectorSuelo.position, Vector2.down, distanciaAbajo, capaSuelo);
        Vector2 direccion = mirandoDerecha ? Vector2.right : Vector2.left;
        RaycastHit2D hitPared = Physics2D.Raycast(detectorSuelo.position, direccion, distanciaFrente, capaSuelo);

        if (puedeGirar && (hitSuelo.collider == null || hitPared.collider != null))
        {
            StartCoroutine(GirarCooldown());
        }
    }

    // --- AQUÍ ESTÁ EL ARREGLO DE LA MUERTE RÁPIDA ---
    public void RecibirDaño(int cantidad)
    {
        if (estaMuerto) return; // Si ya murió, ignoramos golpes extra

        vidaActual -= cantidad;

        if (vidaActual > 0)
        {
            // Sigue vivo: Animación de dolor
            if (anim != null) 
            {
                // Usamos Play para que sea instantáneo y no se lie con triggers
                anim.Play("enemigo_hiteado", -1, 0f); 
            }
        }
        else
        {
            // Ha muerto: Ejecutamos muerte definitiva
            Morir();
        }
    }

    void Morir()
    {
        if (estaMuerto) return;
        estaMuerto = true;
        estaAtacando = false; // Cancelar ataques

        // 1. IMPORTANTE: Quitar la hitbox para que no puedas pegarle más veces
        GetComponent<Collider2D>().enabled = false;

        // 2. Parar físicas para que no deslice
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic; // Lo convertimos en fantasma físico

        // 3. Forzar animación de muerte (Fuerza bruta)
        if (anim != null)
        {
            anim.Play("enemigo_muere");
        }

        // 4. EL TRUCO FINAL: Desactivar este script
        // Esto hace que el Update deje de funcionar, así que el enemigo
        // no intentará volver a andar ni patrullar nunca más.
        this.enabled = false; 

        // 5. Borrar el objeto a los 2 segundos
        Destroy(gameObject, 2f);
    }

    // --- Lógica de Ataque ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (estaMuerto) return; // Si está muerto, no ataca

        if (collision.gameObject.CompareTag("Player") && !estaAtacando)
        {
            StartCoroutine(RealizarAtaque(collision.gameObject));
        }
    }

    IEnumerator RealizarAtaque(GameObject jugador)
    {
        estaAtacando = true; 
        
        // Intentamos forzar la animación de ataque
        if (anim != null)
        {
            anim.SetBool("isWalking", false);
            // Usamos Play en vez de Trigger por si las flechas fallan
            anim.Play("enemigo_ataca");        
        }

        // Empujón al jugador
        Rigidbody2D rbPlayer = jugador.GetComponent<Rigidbody2D>();
        if (rbPlayer != null)
        {
            Vector2 direccionEmpuje = (jugador.transform.position - transform.position).normalized;
            rbPlayer.AddForce(direccionEmpuje * 5f, ForceMode2D.Impulse);
        }
        
        // Daño al jugador
        var vida = jugador.GetComponent<PlayerController>();
        if (vida != null)
        {
             // vida.RecibirDaño(); // Descomenta esto cuando quieras daño real
        }

        yield return new WaitForSeconds(0.6f); // Tiempo del ataque

        estaAtacando = false;
        
        // Si sigue vivo, volvemos a andar
        if (!estaMuerto && anim != null)
        {
             anim.Play("enemigo_andando");
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

    private void OnDrawGizmos()
    {
        if (detectorSuelo == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(detectorSuelo.position, detectorSuelo.position + Vector3.down * distanciaAbajo);
    }
}