using UnityEngine;

public class Tile : MonoBehaviour
{
    private Renderer tileRenderer;                          // Referencia al Renderer de la celda para cambiar su color
    private bool isObstacle = false;                        // Indica si esta celda es un obstáculo.
    private Coordenadas coordenadas;                        // Coordenadas de la celda en la cuadrícula.

    private GameObject obstacleCollider;                    // Referencia a la caja de colisión en el Tile Obstáculo



    private void Start()
    {
        tileRenderer = GetComponent<Renderer>();            // Obtiene el Renderer de la celda para modificar el color del Tile
    }

    // Método para asignar coordenadas a la celda
    public void SetCoordinates(int x, int y)
    {
        coordenadas = new Coordenadas(x, y);
    }
    
    // Método para comparar si las coordenadas de esta celda coinciden con otra
    public bool CoordenadasEquals(Coordenadas other)
    {
        return coordenadas.x == other.x && coordenadas.y == other.y;
    }


    // Método que convierte esta celda en un obstáculo de manera forzada
    public void ForceObstacle()
    {
        isObstacle = true;                                  // Marca la celda como obstáculo
        tileRenderer.material.color = Color.black;          // Cambia el color a negro para indicar obstáculo
        gameObject.tag = "Obstaculo";                       // Asigna la etiqueta "Obstaculo" para detección


        // Si aún no tiene un collider, se crea
        if (obstacleCollider == null)
        {
            obstacleCollider = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacleCollider.transform.position = transform.position + new Vector3(0, 0.5f, 0);                 // Un poco más alto que el tile
            obstacleCollider.transform.localScale = new Vector3(1, 1, 1) * GridManager.Instance.TileSize;       // Ajustar tamaño
            obstacleCollider.GetComponent<Renderer>().enabled = false;                                          // Hacer invisible.
            obstacleCollider.GetComponent<Collider>().isTrigger = true;                                        // Activar colisiones físicas
            obstacleCollider.tag = "Obstaculo";                                                                // Asegurar que la caja tenga el tag correcto
        }
    }

    // Detecta cuando se hace clic sobre la celda
    private void OnMouseDown()
    {
        if (!GridManager.Instance) return;              // Verifica que haya un GridManager activo
        if (!GridManager.Instance.EditMode) return;     // Solo se permite modificar si está en modo edición                                                         // Solo cambiar si estamos en modo edición
        
        ToggleObstacle();                               // Alterna el estado de la celda entre normal y obstáculo                                                                   // Si no se presiona "S" o "G", marcar como obstáculo normal.
            
    }
        

    // Método para alternar entre celda normal y obstáculo.
    private void ToggleObstacle()
    {
        isObstacle = !isObstacle;                                             // Invierte el estado actual de la celda
        tileRenderer.material.color = isObstacle ? Color.black : Color.white; // Cambia color según estado

        if (isObstacle)
        {
            GridManager.Instance.AddObstacle(coordenadas);     // Agrega la celda a la lista de obstáculos                                                 // Agregar la celda a la lista de obstáculos
            gameObject.tag = "Obstaculo";                       // Se marca como obstáculo                                                                    // Asigna la etiqueta "Obstaculo" para detección de colisiones
            
            // Si no tiene un collider de obstáculo, se crea
            if (obstacleCollider == null)
            {
                obstacleCollider = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacleCollider.transform.position = transform.position + new Vector3(0, 0.5f, 0);             // Un poco más alto que el tile
                obstacleCollider.transform.localScale = new Vector3(1, 1, 1) * GridManager.Instance.TileSize;   // Ajustar tamaño
                obstacleCollider.GetComponent<Renderer>().enabled = false;                                      // Hacer invisible
                obstacleCollider.GetComponent<Collider>().isTrigger = false;                                    // Activar colisiones físicas
                obstacleCollider.tag = "Obstaculo";                                                             // Asegurar que la caja también tenga la etiqueta
            }
        }
        else
        {
            gameObject.tag = "Untagged";                // Si deja de ser obstáculo, elimina la etiqueta                                                           // Quita la etiqueta de obstáculo si se vuelve transitable
            
            // Si existe la caja de colisión se elimina
            if (obstacleCollider != null)
            {
                Destroy(obstacleCollider);
                obstacleCollider = null;
            }
        }
    }
       
}
