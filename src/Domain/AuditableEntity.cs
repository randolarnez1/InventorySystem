//este archivo fue echo por manuel

using System;

namespace InventorySystem.Domain
{
    /// <summary>
    /// [DOMINIO - CORE]
    /// Clase Base Abstracta para todas las entidades del sistema (Ej: User, Product, Batch).
    /// Su propósito es centralizar las propiedades de auditoría para garantizar la trazabilidad
    /// y el cumplimiento de requisitos de seguridad en todos los modelos de datos.
    /// No se puede instanciar por sí sola (es 'abstract'), sino que debe ser heredada.
    /// </summary>
    public abstract class AuditableEntity
    {
        // --- IDENTIFICADOR ÚNICO ---
        // El identificador único y principal (Primary Key) del registro en la base de datos.
        // Es la referencia que usarán las otras entidades para establecer relaciones (Foreign Keys).
        public int Id { get; set; }

        // --- CAMPOS DE CREACIÓN (TRAZABILIDAD INICIAL) ---
        
        // Fecha exacta y hora (generalmente en formato UTC) en que este registro fue persistido
        // por primera vez en el sistema. Es inmutable una vez establecido.
        public DateTime CreatedAt { get; set; }
        
        // ID o Nombre del usuario que ejecutó la acción de creación de este registro.
        // Es fundamental para saber quién es responsable de la entrada inicial.
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
        
        // la Fecha en la que se "borró"
        // Es fecha y hora exacta en la que se aplicó la eliminación lógica.
        // Es de tipo 'DateTime?' (nullable) porque solo tiene valor si IsDeleted es TRUE.
        public DateTime? DeletedAt { get; set; }
        
        // ID o Nombre del usuario que ejecutó la acción del "borrado" (Soft Delete) del registro.
        // También es de tipo nullable.
        public string? DeletedBy { get; set; }
    }
}