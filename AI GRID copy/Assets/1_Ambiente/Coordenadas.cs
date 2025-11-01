using System;                           // Importa el espacio de nombres System (necesario para [Serializable])

[Serializable]                          // Permite que la estructura se pueda serializar
                                        // se usa para indicar que una clase o estructura puede ser convertida 
                                        // en un formato que Unity pueda guardar y visualizar en el Inspector. 
                                        // Esto es útil para exponer datos en la interfaz de Unity y asegurarse 
                                        // de que se puedan almacenar correctamente en archivos de escenas o prefabs.
                
public struct Coordenadas
{
    public int x;                       // Variable pública que almacena la coordenada X
    public int y;                       // Variable pública que almacena la coordenada Y

    // Constructor que recibe dos valores enteros
    public Coordenadas(int x, int y)
    {
        this.x = x;                     // Asigna el valor del parámetro x a la variable de la estructura
        this.y = y;                     // Asigna el valor del parámetro y a la variable de la estructura
    }    
}
