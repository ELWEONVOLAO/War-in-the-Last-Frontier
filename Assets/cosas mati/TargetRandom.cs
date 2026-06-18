using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class TargetRandom : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Movement Toggles")]
    [Tooltip("Activa o desactiva por completo todo el movimiento")]
    [SerializeField] private bool enableMovement = true;
    [Tooltip("Permite al objeto moverse de izquierda a derecha de forma aleatoria")]
    [SerializeField] private bool moveHorizontally = true;
    [Tooltip("Permite al objeto moverse de arriba a abajo de forma aleatoria")]
    [SerializeField] private bool moveVertically = false;

    [Header("Speed Settings")]
    [SerializeField] private float horizontalSpeed = 3f;
    [SerializeField] private float verticalSpeed = 2f;

    [Tooltip("Cada cuántos segundos cambia de dirección el objeto")]
    [SerializeField] private float changeDirectionInterval = 1.5f;

    [Header("Boundary Settings (Radio de Movimiento)")]
    [Tooltip("Distancia máxima en metros que el objeto puede alejarse de su punto de inicio")]
    [SerializeField] private float movementRadius = 5f;

    private Vector3 startPosition;
    private Vector3 targetDirection;
    private float nextDirectionChangeTime;

    private void Awake()
    {
        currentHealth = maxHealth;
        // Guardamos el punto exacto donde colocaste el objeto en la escena como el centro de su radio
        startPosition = transform.position;
        ChooseNewRandomDirection();
    }

    private void Update()
    {
        if (enableMovement)
        {
            MoveObject();
        }
    }

    // --- ESTE MÉTODO SIGUE SIENDO LLAMADO POR TU WEAPON SYSTEM ---
    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log($"{gameObject.name} recibió {damageAmount} de daño. Vida restante: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void MoveObject()
    {
        // Cambiar de dirección cuando pase el intervalo de tiempo configurado
        if (Time.time >= nextDirectionChangeTime)
        {
            ChooseNewRandomDirection();
        }

        // Creamos el vector de movimiento base aplicando las velocidades si los booleanos están activos
        float moveX = moveHorizontally ? targetDirection.x * horizontalSpeed : 0f;
        float moveY = moveVertically ? targetDirection.y * verticalSpeed : 0f;

        Vector3 movement = new Vector3(moveX, moveY, 0f);

        // Mover el objeto en el espacio global
        transform.Translate(movement * Time.deltaTime, Space.World);

        // --- SISTEMA DE CONTROL DE RADIO (LÍMITE ESTRICTO) ---
        // Calculamos la distancia actual respecto al punto de inicio original
        float distanceFromStart = Vector3.Distance(startPosition, transform.position);

        if (distanceFromStart > movementRadius)
        {
            // Si se pasa del radio, calculamos la dirección de regreso al centro
            Vector3 fromStartToTarget = transform.position - startPosition;

            // Forzamos al objeto a quedarse exactamente en el límite exterior de la esfera/círculo
            transform.position = startPosition + fromStartToTarget.normalized * movementRadius;

            // Invertimos la dirección de forma inmediata para que empiece a regresar y no se quede pegado en el borde
            targetDirection = -targetDirection;
        }
    }

    private void ChooseNewRandomDirection()
    {
        // Genera valores aleatorios independientes entre -1 y 1
        float randomX = Random.Range(-1f, 1f);
        float randomY = Random.Range(-1f, 1f);

        // Normalizamos el vector para que la velocidad total resultante sea uniforme
        targetDirection = new Vector3(randomX, randomY, 0f).normalized;

        // Asignamos el próximo frame de tiempo en el que cambiará de rumbo
        nextDirectionChangeTime = Time.time + changeDirectionInterval;
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} ha muerto.");
        Destroy(gameObject);
    }

    // Dibuja una línea de guía en la pestaña 'Scene' para que puedas ver el tamaño real del radio en Unity
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? startPosition : transform.position;
        Gizmos.DrawWireSphere(center, movementRadius);
    }
}