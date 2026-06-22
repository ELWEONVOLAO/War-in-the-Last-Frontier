using UnityEngine;
using UnityEngine.UI;

public class CrosshairSelector : MonoBehaviour
{
    [Header("Referencias UI")]
    public Transform contenedorLista;   // El panel con el Grid Layout Group
    public GameObject botonMiraPrefab;  // El prefab del botón que creaste
    public Image imagenPreview;         // La imagen que muestra la selección actual

    [Header("Tus Miras")]
    public Sprite[] spritesMiras;       // Arrastra aquí todas tus imágenes de miras

    void Start()
    {
        GenerarLista();

        // Carga la mira que el jugador tenía guardada de antes (por defecto la 0)
        int indexGuardado = PlayerPrefs.GetInt("CrosshairIndex", 0);
        ActualizarPreview(indexGuardado);
    }

    void GenerarLista()
    {
        // 1. Limpiamos el contenedor por si había botones viejos
        foreach (Transform child in contenedorLista)
        {
            Destroy(child.gameObject);
        }

        // 2. Creamos un botón por cada sprite en tu array
        for (int i = 0; i < spritesMiras.Length; i++)
        {
            int index = i; // Guardamos una copia local del índice para el botón

            GameObject nuevoBoton = Instantiate(botonMiraPrefab, contenedorLista);

            // 3. Le asignamos la imagen correspondiente al botón
            Image imgBoton = nuevoBoton.GetComponent<Image>();
            if (imgBoton != null)
            {
                imgBoton.sprite = spritesMiras[index];
            }

            // 4. Le agregamos el evento OnClick por código
            Button btn = nuevoBoton.GetComponent<Button>();
            if (btn != null)
            {
                // Cuando se haga clic, llamará a SeleccionarMira con su índice
                btn.onClick.AddListener(() => SeleccionarMira(index));
            }
        }
    }

    public void SeleccionarMira(int indice)
    {
        // Guardamos la elección en la memoria de la PC
        // La clave "CrosshairIndex" DEBE ser idéntica a la que usas en el UIManager
        PlayerPrefs.SetInt("CrosshairIndex", indice);
        PlayerPrefs.Save();

        // Actualizamos la previsualización visual
        ActualizarPreview(indice);
    }

    void ActualizarPreview(int indice)
    {
        if (spritesMiras.Length > 0 && indice < spritesMiras.Length)
        {
            if (imagenPreview != null)
            {
                imagenPreview.sprite = spritesMiras[indice];
            }
        }
    }
}