
namespace InventorySystem.Domain
{
    /// <summary>
    /// [DOMINIO - ENTIDAD]
    /// Representa a un usuario del sistema.
    /// Hereda de AuditableEntity para tener historial de creación y edición automáticamente.
    /// </summary>
    public class User : AuditableEntity
    {
        // Nombre de usuario único para login
        public string Username { get; set; } = string.Empty;

        // Contraseña ENCRIPTADA (Nunca guardaremos texto plano por seguridad)
        public string PasswordHash { get; set; } = string.Empty;

        // Rol del usuario: "Admin" (Acceso total) o "Employee" (Acceso limitado)
        public string Role { get; set; } = "Employee";
    }
}