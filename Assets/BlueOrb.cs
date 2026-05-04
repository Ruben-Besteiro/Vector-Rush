using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlueOrb : MonoBehaviour
{
    [Header("Configuración de Detección")]
    [SerializeField] private float detectionRadius = 1.5f;
    
    private bool playerIsInside = false;

    void Update()
    {
        DetectPlayer();
    }

    private void DetectPlayer()
    {
        // Detectar colliders en el radio especificado
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        
        bool foundPlayerThisFrame = false;
        PlayerController playerController = null;

        foreach (var hitCollider in hitColliders)
        {
            print(hitCollider.name);
            if (hitCollider.CompareTag("Player"))
            {
                foundPlayerThisFrame = true;
                playerController = hitCollider.GetComponent<PlayerController>();
                break;
            }
        }

        if (foundPlayerThisFrame)
        {
            // Solo invertimos la gravedad si el jugador acaba de entrar en el radio
            if (!playerIsInside)
            {
                if (playerController != null)
                {
                    playerController.InvertGravity();
                }
                playerIsInside = true;
            }
        }
        else
        {
            // Cuando el jugador sale del radio, reseteamos el flag
            playerIsInside = false;
        }
    }

    // Visualización en el Editor
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 0.5f, 1, 0.3f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
