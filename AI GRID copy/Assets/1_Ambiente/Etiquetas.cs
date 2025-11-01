using UnityEngine;                                              // Importa la librería principal de Unity
using TMPro;                                                    // Importa la librería de TextMeshPro para trabajar con texto en la UI

public class Etiquetas : MonoBehaviour
{
    [SerializeField]  
    private TextMeshPro label;                                  // Referencia a un objeto de texto TextMeshPro en la escena.
                                                                // Se mantiene privada para encapsulación, pero con [SerializeField] 
                                                                // se puede asignar desde el Inspector de Unity

    public Coordenadas coordenadas;                             // Variable pública que almacena las coordenadas en una estructura.
                                                                // Esto permite visualizar y modificar las coordenadas en el Inspector

    public void SetCoordinates(int x, int y)                    // Método público que actualiza las coordenadas y el texto en pantalla
    {
        coordenadas = new Coordenadas(x, y);                    // Crea una nueva instancia de la estructura Coordenadas con los valores dados
        UpdateCordsLabel();                                     // Llama al método que actualiza el texto en pantalla
    }

    
    private void UpdateCordsLabel()                             // Método privado que actualiza el texto del TextMeshPro con las coordenadas actuales
    {
    //  label.text = coordenadas.x + ", " + coordenadas.y;      //Así se hacía antes, ahora se utiliza interpolación de cadenas en la línea
        label.text = $"{coordenadas.x}, {coordenadas.y}";       // Usa interpolación de cadenas ($"{variable}") para mostrar las coordenadas como "x, y"
    }
}
