using System;

namespace InventorySystem.Domain
{
    /// <summary>
    /// [DOMINIO - CORE]
    /// Clase Base para todas las entidades del sistema.
    /// Define la estructura obligatoria de auditoría para cumplir con los requisitos de seguridad.
    /// No se puede instanciar por sí sola (abstract), solo sirve para ser heredada.
    /// </summary>
    public abstract class AuditableEntity
    {
        // Identificador único del registro en la base de datos
        public int Id { get; set; }

        // --- CAMPOS DE CREACIÓN ---
        
        // Fecha exacta en que se creó el registro
        public DateTime CreatedAt { get; set; }
        
        // ID o Nombre del usuario que creó este registro (Trazabilidad)
        public string CreatedBy { get; set; } = string.Empty;

        // --- CAMPOS DE MODIFICACIÓN ---

        // Fecha de la última vez que se editó algo en este registro
        public DateTime LastModifiedAt { get; set; }
        
        // Quién fue el último en tocar este registro
        public string LastModifiedBy { get; set; } = string.Empty;

        // --- CAMPOS DE ELIMINACIÓN LÓGICA (SOFT DELETE) ---

        // Bandera principal: Si es TRUE, el sistema ignora este registro (parece borrado)
        // pero sigue existiendo físicamente en la base de datos para auditoría.
        public bool IsDeleted { get; set; }
        
        // Fecha en que se "borró"
        public DateTime? DeletedAt { get; set; }
        
        // Quién realizó la acción de borrar
        public string? DeletedBy { get; set; }
    }
}