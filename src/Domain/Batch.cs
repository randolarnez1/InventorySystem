namespace InventorySystem.Domain
{
    /// <summary>
    /// [DOMINIO - ENTIDAD CORE]
    /// Representa un LOTE de mercadería que ingresó al almacén.
    /// El stock total de un producto es la suma de las cantidades de todos sus lotes activos.
    /// </summary>
    public class Batch : AuditableEntity
    {
        // ID del producto que contiene este lote (Relación FK con Product)
        public int ProductId { get; set; }

        // ID del proveedor que nos vendió este lote (Relación FK con Supplier)
        public int SupplierId { get; set; }

        // Cantidad ACTUAL disponible en este lote específico.
        // Cuando vendemos, este número baja. Si llega a 0, el lote se considera agotado.
        public int Quantity { get; set; }

        // Costo de compra unitario para este lote.
        // Vital para calcular márgenes de ganancia reales (Costo Promedio o FIFO).
        public decimal CostPrice { get; set; }

        // Fecha de ingreso al almacén.
        // VITAL PARA FIFO: Los lotes con EntryDate más antigua salen primero.
        public DateTime EntryDate { get; set; }

        // Fecha de caducidad.
        // Obligatorio si el Producto es 'Perishable', opcional si no.
        public DateTime? ExpirationDate { get; set; }
    }
}