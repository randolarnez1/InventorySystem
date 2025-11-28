// Este archivo creado y comentado por Manuel (Arquitectura y Seguridad).
// CAPA: Domain (Entidades clave del sistema).

namespace InventorySystem.Domain 
{
    /// <summary>
    /// [DOMINIO - ENTIDAD CLAVE DE SEGURIDAD]
    /// Representa a un usuario registrado y autenticado del sistema de inventario.
    ///
    /// HERENCIA Y TRAZABILIDAD:
    /// Hereda de AuditableEntity para obtener automáticamente los campos de auditoría
    /// (Id, CreatedAt, CreatedBy, IsDeleted, etc.), lo cual es un requisito de seguridad.
    /// </summary>
    public class User : AuditableEntity
    {
        // Nombre de usuario único (Username) que se utiliza para el inicio de sesión (Login).
        // Es un requisito de unicidad y no debe ser sensible a mayúsculas/minúsculas en la búsqueda.
        public string Username { get; set; } = string.Empty;

        // ** ENCRIPTACIÓN PARA LA SEGURIDAD **
        // Almacena el HASH de la contraseña, que es la versión ENCRIPTADA de la clave de acceso.
        // NUNCA, bajo ninguna circunstancia, se debe guardar la contraseña en texto plano.
        // El 'UserService.cs' de Manuel es responsable de generar este hash.
        public string PasswordHash { get; set; } = string.Empty;

        // Rol del usuario: "Admin" (Acceso total) o "Employee" (Acceso limitado)
        // Define el nivel de permisos que el usuario tiene dentro del sistema.
        // Valores posibles (ejemplo): "Admin" (acceso total a configuración e inventario) o
        // "Employee" (acceso limitado a registro de entradas y salidas).
        public string Role { get; set; } = "Employee";
    }
}