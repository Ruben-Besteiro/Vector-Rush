using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Death : MonoBehaviour
{
    public void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            print(collision.gameObject + " te ha matado");
            Destroy(collision.gameObject);
        }
    }
}
