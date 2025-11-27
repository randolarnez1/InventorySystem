
namespace InventorySystem.Domain
{
    /// <summary>
    /// [DOMINIO - ENTIDAD]
    /// Representa a un Cliente (Customer).
    /// Necesario para la salida de inventario (Ventas).
    /// </summary>
    public class Customer : AuditableEntity
    {
        // Nombre del cliente
        public string Name { get; set; } = string.Empty;

        // Documento de Identidad Fiscal (DNI, NIT, RUC, Cédula)
        public string TaxId { get; set; } = string.Empty;
    }
}