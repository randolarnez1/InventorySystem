
namespace InventorySystem.Domain
{
    /// <summary>
    /// [DOMINIO - ENTIDAD]
    /// Representa a un Proveedor (Supplier).
    /// Un Lote (Batch) NO puede existir sin un proveedor asociado.
    /// </summary>
    public class Supplier : AuditableEntity
    {
        // Nombre de la empresa o persona
        public string Name { get; set; } = string.Empty;

        // Contacto principal (Email o Teléfono)
        public string ContactEmail { get; set; } = string.Empty;
    }
}