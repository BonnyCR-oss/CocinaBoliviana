namespace CocinaBoliviana.Data
{
    /// <summary>Cómo se cocina un ingrediente. La olla hierve, el sartén fríe.</summary>
    public enum MetodoCoccion
    {
        Hervir,
        Freir,

        /// <summary>Parrilla. En la cocina boliviana, el sonso cruceno.</summary>
        Asar
    }

    /// <summary>
    /// Punto de cocción de un ingrediente concreto. Es un estado sobre el mismo objeto,
    /// no un prefab distinto: cocinar solo cambia su color, a diferencia de cortar, que sí
    /// lo reemplaza porque la forma cambia de verdad.
    /// </summary>
    public enum EstadoCoccion
    {
        Crudo,
        Cocido,
        Quemado
    }
}
