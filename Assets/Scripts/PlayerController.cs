using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private Vector2 puntoRespawn;
    
    [Header("Ajustes de Movimiento")]
    public float speed = 8f;
    public float jumpForce = 12f;
    [Range(0, 1)] public float crouchSpeedMultiplier = 0.4f; 
    
    [Header("Ajustes de Colisión")]
    public Vector2 sizeNormal = new Vector2(1f, 2f);
    public Vector2 offsetNormal = new Vector2(0f, 1f);
    public Vector2 sizeAgachado = new Vector2(1f, 1f);
    public Vector2 offsetAgachado = new Vector2(0f, 0.5f);

    [Header("Detector de Techo")]
    public Transform techoCheck;       
    public float radioTecho = 0.2f;    
    public LayerMask capaSuelo;        

    [Header("Combate y Salud")]
    public Collider2D sensorAtaque; 
    public int vidaActual = 3;
    public float tiempoInvulnerable = 1.5f;

    private Rigidbody2D rb;
    private Animator anim;
    private BoxCollider2D col; 
    private SpriteRenderer sprite; 
    
    private bool isGrounded;
    private bool isDead = false;
    private bool esInvulnerable = false;
    private float moveX;
    private bool mirandoDerecha = true;

    // --- VISUALIZADOR DEL DETECTOR EN EL EDITOR ---
    private void OnDrawGizmos()
    {
        if (techoCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(techoCheck.position, radioTecho);
        }
        
        // DIBUJAMOS EL RADAR DE DAÑO (Para que veas el área de choque)
        if (col != null)
        {
            Gizmos.color = Color.yellow;
            // Dibujamos un cubo donde el jugador detecta daño
            Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.size);
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponentInChildren<BoxCollider2D>(); 
        sprite = GetComponentInChildren<SpriteRenderer>(); 
        
        puntoRespawn = transform.position;
        
        if (sensorAtaque != null) sensorAtaque.enabled = false;
        vidaActual = 3; 
    }

    void Update()
    {
        if (isDead) return; 

        bool agachadoInput = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;
        
        moveX = 0;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1;
        else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = 1;

        bool hayTecho = (techoCheck != null) && Physics2D.OverlapCircle(techoCheck.position, radioTecho, capaSuelo);
        bool debeEstarAgachado = agachadoInput || hayTecho;

        if (col != null)
        {
            col.size = debeEstarAgachado ? sizeAgachado : sizeNormal;
            col.offset = debeEstarAgachado ? offsetAgachado : offsetNormal;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded && !debeEstarAgachado)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); 
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        if (Keyboard.current.kKey.wasPressedThisFrame) 
        {
            anim.SetTrigger("Attack");
            StartCoroutine(ActivarAtaque());
        }

        if (Keyboard.current.mKey.wasPressedThisFrame) Morir();

        if (moveX > 0 && !mirandoDerecha) Girar();
        else if (moveX < 0 && mirandoDerecha) Girar();

        anim.SetFloat("Speed", Mathf.Abs(moveX));
        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        anim.SetBool("isCrouching", debeEstarAgachado); 
    }

    void Girar()
    {
        mirandoDerecha = !mirandoDerecha;
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }

    void FixedUpdate()
    {
        if (isDead) return;

        bool agachadoInput = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;
        bool hayTecho = (techoCheck != null) && Physics2D.OverlapCircle(techoCheck.position, radioTecho, capaSuelo);
        bool modoAgachado = agachadoInput || hayTecho;

        float vActual = modoAgachado ? (speed * crouchSpeedMultiplier) : speed;

        rb.linearVelocity = new Vector2(moveX * vActual, rb.linearVelocity.y);

        // --- ¡AQUÍ ESTÁ LA MAGIA! ---
        // Llamamos a la función "Radar" en cada ciclo de físicas
        DetectarEnemigosCercanos(); 
    }

    // --- NUEVA FUNCIÓN: RADAR DE DAÑO ---
    // Esto soluciona el problema de "quedarse pegado y no recibir daño"
    void DetectarEnemigosCercanos()
    {
        // 1. Si ya somos invulnerables o estamos muertos, no comprobamos nada
        if (esInvulnerable || isDead) return;

        // 2. Creamos una caja invisible del mismo tamaño que el jugador
        // OverlapBoxAll nos devuelve TODO lo que estamos tocando
        Collider2D[] contactos = Physics2D.OverlapBoxAll(transform.position + (Vector3)col.offset, col.size, 0f);

        foreach (Collider2D contacto in contactos)
        {
            // 3. Si tocamos algo con el Tag "Enemigo"...
            if (contacto.CompareTag("Enemigo"))
            {
                Debug.Log("¡Contacto continuo con enemigo detectado!");
                RecibirDaño(); // Nos hacemos daño a nosotros mismos
                return; // Salimos para no recibir daño 20 veces en el mismo frame
            }
        }
    }

    IEnumerator ActivarAtaque()
    {
        yield return new WaitForSeconds(0.1f);
        if (sensorAtaque != null) sensorAtaque.enabled = true;
        yield return new WaitForSeconds(0.2f);
        if (sensorAtaque != null) sensorAtaque.enabled = false;
    }

    public void RecibirDaño()
    {
        // El sistema de invulnerabilidad evita que mueras al instante
        if (esInvulnerable || isDead) return;
        
        vidaActual--;
        Debug.Log("Vida actual: " + vidaActual);
        
        if (vidaActual <= 0) 
        {
            Morir();
        }
        else 
        {
            anim.SetTrigger("Hurt");
            StartCoroutine(Invulnerabilidad());
            
            // OPCIONAL: Pequeño empujón hacia atrás al recibir daño
            // rb.AddForce(new Vector2(mirandoDerecha ? -5 : 5, 5), ForceMode2D.Impulse);
        }
    }

    IEnumerator Invulnerabilidad()
    {
        esInvulnerable = true;
        for (int i = 0; i < 5; i++)
        {
            if(sprite) sprite.color = new Color(1, 1, 1, 0.4f);
            yield return new WaitForSeconds(0.15f); // Parpadeo
            if(sprite) sprite.color = Color.white;
            yield return new WaitForSeconds(0.15f);
        }
        esInvulnerable = false; // Aquí termina la invulnerabilidad y te pueden volver a pegar
    }

    public void Morir() 
    {
        if (isDead) return;
        isDead = true;

        anim.SetFloat("Speed", 0);
        anim.SetBool("isGrounded", true);
        anim.SetBool("isCrouching", false);
        anim.SetTrigger("isDead"); 

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static; 
        
        if(col != null) col.enabled = false; 

        Invoke("Respawn", 2f);
    }

    // Mantenemos el collision enter por si acaso, pero el Radar hace el trabajo sucio
    private void OnCollisionEnter2D(Collision2D collision) 
    { 
        if (collision.gameObject.CompareTag("Ground")) isGrounded = true; 
        // Ya no es estrictamente necesario el de Enemigo aquí porque el Radar lo detectará,
        // pero lo dejamos por seguridad.
        if (collision.gameObject.CompareTag("Enemigo")) RecibirDaño();
    }

    private void OnCollisionStay2D(Collision2D collision) { if (collision.gameObject.CompareTag("Ground")) isGrounded = true; }
    private void OnCollisionExit2D(Collision2D collision) { if (collision.gameObject.CompareTag("Ground")) isGrounded = false; }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // --- FILTRO DE SEGURIDAD PARA LA ESPADA ---
        // Si chocamos con una trampa...
        if (collision.CompareTag("trampa") && !isDead)
        {
            // Verificamos: ¿Ha sido la espada la que ha tocado el pincho?
            if (sensorAtaque != null && sensorAtaque.enabled && sensorAtaque.IsTouching(collision))
            {
                // ¡SÍ! Ha sido la espada. No hacemos nada.
                return; 
            }

            // Si NO ha sido la espada, entonces ha sido el cuerpo -> MUERTE
            Morir();
        }
        // ------------------------------------------

        if (collision.CompareTag("Meta")) SceneManager.LoadScene(2); 
        
        if (collision.CompareTag("Checkpoint"))
        {
            puntoRespawn = collision.transform.position;
            Debug.Log("¡Checkpoint guardado!");
        }
    }
    
    public void Respawn()
    {
        transform.position = puntoRespawn;
        vidaActual = 3; 
        isDead = false;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
    
        if(col != null) col.enabled = true;

        if(anim != null) 
        {
            anim.Rebind(); 
            anim.Play("Idle"); 
        }
    }

    void ReiniciarEscena() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}