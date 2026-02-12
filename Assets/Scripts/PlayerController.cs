using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerController : MonoBehaviour
{
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

    // --- ESTO ES LO NUEVO: EL CÍRCULO ROJO PARA VER EL DETECTOR ---
    private void OnDrawGizmos()
    {
        if (techoCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(techoCheck.position, radioTecho);
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponentInChildren<BoxCollider2D>(); 
        sprite = GetComponentInChildren<SpriteRenderer>(); 

        if (sensorAtaque != null) sensorAtaque.enabled = false;
        vidaActual = 3; 
    }

    void Update()
    {
        if (isDead) return; // Si estamos muertos, no leemos input.

        bool agachadoInput = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;
        
        moveX = 0;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1;
        else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = 1;

        // Comprobamos techo para actualizar el colisionador visualmente
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

        // Actualización de animaciones
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

        // Calculamos velocidad: reducida si está agachado, normal si no
        float vActual = modoAgachado ? (speed * crouchSpeedMultiplier) : speed;

        // Aplicamos velocidad al Rigidbody
        rb.linearVelocity = new Vector2(moveX * vActual, rb.linearVelocity.y);
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
        if (esInvulnerable || isDead) return;
        vidaActual--;
        
        if (vidaActual <= 0) 
        {
            Morir();
        }
        else 
        {
            anim.SetTrigger("Hurt");
            StartCoroutine(Invulnerabilidad());
        }
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

    public void Morir() 
    {
        if (isDead) return;
        isDead = true;

        // 1. Limpieza de Animator
        anim.SetFloat("Speed", 0);
        anim.SetBool("isGrounded", true);
        anim.SetBool("isCrouching", false);
        anim.SetTrigger("isDead"); 

        // 2. Bloqueo físico total (estatua)
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static; 
        
        if(col != null) col.enabled = false; // Opcional: para que el cadáver no moleste

        Invoke("ReiniciarEscena", 2f);
    }

    private void OnCollisionEnter2D(Collision2D collision) 
    { 
        if (collision.gameObject.CompareTag("Ground")) isGrounded = true; 
        if (collision.gameObject.CompareTag("Enemigo")) RecibirDaño();
    }

    private void OnCollisionStay2D(Collision2D collision) { if (collision.gameObject.CompareTag("Ground")) isGrounded = true; }
    private void OnCollisionExit2D(Collision2D collision) { if (collision.gameObject.CompareTag("Ground")) isGrounded = false; }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("trampa") && !isDead) Morir();
        if (collision.CompareTag("Meta")) SceneManager.LoadScene(0); 
    }

    void ReiniciarEscena() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}