using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int deaths = 0;
    public float time = 0;
    [SerializeField] private Canvas GGCanvas;

    public static GameManager Instance; 

    private void Awake() 
    {
        if (Instance == null) 
        {
            Instance = this; 
            // Opcional: Si quieres que este objeto persista entre escenas
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
    }

    private float lastDeathTime = -1f;
    [SerializeField] private float deathCooldown = 1f;

    public void IncreaseDeaths()
    {
        // Solo incrementamos la muerte si ha pasado el tiempo de cooldown
        if (Time.time - lastDeathTime >= deathCooldown)
        {
            deaths++;
            lastDeathTime = Time.time;
        }
    }

    public void GG()
    {
        GGCanvas.gameObject.SetActive(true);
        GGCanvas.GetComponent<TextMesh>().text = "Muertes: " + deaths + "\nTiempo: " + time;
        MusicManager.Instance.StopMusic();
    }
}
