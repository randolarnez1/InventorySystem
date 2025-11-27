
namespace InventorySystem.Domain
{
    /// <summary>
    /// [DOMINIO - ENUMERACIÓN]
    /// Define las únicas categorías permitidas en el sistema.
    /// Al hacerlo así, evitamos errores de escritura como "groceries" vs "Groceries".
    /// </summary>
public enum ProductCategory
    {
        Groceries,   // Alimentos
        Electronics, // Electrónica
        General      // <--- NUEVA CATEGORÍA AGREGADA
    }

    /// <summary>
    /// [DOMINIO - ENTIDAD]
    /// Representa la DEFINICIÓN de un producto (no el stock físico).
    /// Ejemplo: "Leche 1L" (Producto) vs "El cartón de leche que tengo en la mano" (Lote).
    /// </summary>
    public class Product : AuditableEntity
    {
        // Nombre descriptivo del producto
        public string Name { get; set; } = string.Empty;

        // Stock Keeping Unit: Código único (código de barras) para identificar el producto
        public string Sku { get; set; } = string.Empty;

        // Categoría estricta (usando el Enum de arriba)
        public ProductCategory Category { get; set; }

        // Bandera que determina el comportamiento del inventario:
        // TRUE = Requiere fecha de caducidad obligatoria en los lotes.
        // FALSE = No caduca (como un cable HDMI).
        public bool IsPerishable { get; set; }
    }
}