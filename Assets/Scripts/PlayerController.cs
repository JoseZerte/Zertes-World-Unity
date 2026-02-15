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

    [Header("Combate y Vidas")]
    public Collider2D sensorAtaque;
    public int vidasTotales = 3;
    public int saludMaxima = 3;
    private int saludActual;

    // --- AÑADIDO: SONIDOS ---
    [Header("Sonidos")]
    public AudioClip sonidoSalto;
    public AudioClip sonidoAtaque;
    public AudioClip sonidoHerida;
    public AudioClip sonidoMuerte;
    public AudioClip sonidoTrampa;
    private AudioSource audioSource;
    // ------------------------

    private Rigidbody2D rb;
    private Animator anim;
    private BoxCollider2D col;
    private SpriteRenderer sprite;
    
    private bool isGrounded;
    private bool isDead = false;
    private bool esInvulnerable = false;
    private float moveX;
    private bool mirandoDerecha = true;

    private void OnDrawGizmos()
    {
        if (techoCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(techoCheck.position, radioTecho);
        }
        if (col != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.size);
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponentInChildren<BoxCollider2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        
        // Inicializamos audio
        audioSource = GetComponent<AudioSource>();

        puntoRespawn = transform.position;
        if (sensorAtaque != null) sensorAtaque.enabled = false;
        
        saludActual = saludMaxima;
        Debug.Log("Vidas (Respawns) restantes: " + vidasTotales);
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
            
            // SONIDO SALTO
            if(audioSource && sonidoSalto) audioSource.PlayOneShot(sonidoSalto);
        }

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            anim.SetTrigger("Attack");
            // SONIDO ATAQUE
            if(audioSource && sonidoAtaque) audioSource.PlayOneShot(sonidoAtaque);
            StartCoroutine(ActivarAtaque());
        }

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

        DetectarEnemigosCercanos();
    }

    void DetectarEnemigosCercanos()
    {
        if (esInvulnerable || isDead) return;

        // TU LÓGICA ORIGINAL DE RADAR
        Collider2D[] contactos = Physics2D.OverlapBoxAll(transform.position + (Vector3)col.offset, col.size, 0f);

        foreach (Collider2D contacto in contactos)
        {
            if (contacto.CompareTag("Enemigo"))
            {
                RecibirGolpe(1);
                return;
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

    public void RecibirGolpe(int cantidad)
    {
        if (esInvulnerable || isDead) return;
        
        saludActual -= cantidad;
        
        // SONIDO HERIDA
        if (saludActual > 0 && audioSource && sonidoHerida) 
            audioSource.PlayOneShot(sonidoHerida);
        
        if (saludActual <= 0)
        {
            IniciarSecuenciaMuerte();
        }
        else
        {
            anim.SetTrigger("Hurt");
            StartCoroutine(Invulnerabilidad());
        }
    }

    public void MuerteInstantanea()
    {
        if (isDead) return;
        
        // SONIDO TRAMPA
        if(audioSource && sonidoTrampa) audioSource.PlayOneShot(sonidoTrampa);
        
        saludActual = 0;
        IniciarSecuenciaMuerte();
    }

    void IniciarSecuenciaMuerte()
    {
        isDead = true;
        
        // SONIDO MUERTE
        if(audioSource && sonidoMuerte) audioSource.PlayOneShot(sonidoMuerte);

        vidasTotales--;
        Debug.Log("Has muerto. Vidas restantes para respawn: " + vidasTotales);

        anim.SetTrigger("isDead");
        anim.SetFloat("Speed", 0);
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;
        if(col != null) col.enabled = false;

        Invoke("DecidirFuturo", 2f);
    }

    void DecidirFuturo()
    {
        if (vidasTotales > 0)
        {
            Respawn();
        }
        else
        {
            SceneManager.LoadScene(3);
        }
    }

    public void Respawn()
    {
        isDead = false;
        transform.position = puntoRespawn;
        saludActual = saludMaxima;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        if(col != null) col.enabled = true;
        if(anim != null)
        {
            anim.Rebind();
            anim.Play("Idle");
        }
        StartCoroutine(Invulnerabilidad());
    }

    IEnumerator Invulnerabilidad()
    {
        esInvulnerable = true;
        for (int i = 0; i < 5; i++)
        {
            if(sprite) sprite.color = new Color(1, 1, 1, 0.4f);
            yield return new WaitForSeconds(0.15f);
            if(sprite) sprite.color = Color.white;
            yield return new WaitForSeconds(0.15f);
        }
        esInvulnerable = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground")) isGrounded = true;
        
        // --- SEGURO ANTI-BUGS: SI EL RADAR FALLA, ESTO TE HACE DAÑO ---
        if (collision.gameObject.CompareTag("Enemigo")) RecibirGolpe(1);
    }

    // --- ESTAS LÍNEAS FALTABAN EN TU CÓDIGO Y POR ESO FALLABA EL SALTO ---
    private void OnCollisionStay2D(Collision2D collision) 
    { 
        if (collision.gameObject.CompareTag("Ground")) isGrounded = true; 
    }
    
    private void OnCollisionExit2D(Collision2D collision) 
    { 
        if (collision.gameObject.CompareTag("Ground")) isGrounded = false; 
    }
    // --------------------------------------------------------------------

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("trampa") && !isDead)
        {
            if (sensorAtaque != null && sensorAtaque.enabled && sensorAtaque.IsTouching(collision)) return;

            MuerteInstantanea();
        }

        if (collision.CompareTag("Meta")) SceneManager.LoadScene(2);
        if (collision.CompareTag("Checkpoint"))
        {
            puntoRespawn = collision.transform.position;
        }
    }
}