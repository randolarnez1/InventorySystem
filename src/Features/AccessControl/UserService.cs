using System;
using System.Security.Cryptography; // Necesario para encriptar contraseñas
using System.Text;
using Microsoft.Data.Sqlite;        // Librería de SQLite
using InventorySystem.Shared;

namespace InventorySystem.Features.AccessControl
{
    /// <summary>
    /// [LOGICA DE NEGOCIO Y ACCESO A DATOS]
    /// Servicio encargado de gestionar todo lo relacionado con Usuarios.
    /// Actúa como el Repositorio y el Servicio a la vez para simplificar la estructura.
    /// </summary>
    public class UserService
    {
        /// <summary>
        /// [INFRAESTRUCTURA]
        /// Crea la tabla de Usuarios si no existe en la base de datos.
        /// </summary>
        public void EnsureTableExists()
        {
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                
                // SQL para crear la tabla. Fíjate que incluimos todos los campos de auditoría.
                command.CommandText = 
                @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL UNIQUE,
                        PasswordHash TEXT NOT NULL,
                        Role TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        CreatedBy TEXT NOT NULL,
                        LastModifiedAt TEXT NOT NULL,
                        LastModifiedBy TEXT NOT NULL,
                        IsDeleted INTEGER DEFAULT 0,
                        DeletedAt TEXT,
                        DeletedBy TEXT
                    );
                ";
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// [API del Módulo]
        /// Registra un nuevo usuario en el sistema con auditoría.
        /// Retorna true si fue exitoso.
        /// </summary>
        /// <param name="username">Nombre de usuario</param>
        /// <param name="password">Contraseña en texto plano (se encriptará aquí)</param>
        /// <param name="role">Rol del usuario</param>
        /// <param name="creatorName">Nombre del usuario ADMIN que está creando a este usuario</param>
        public bool RegisterUser(string username, string password, string role, string creatorName)
        {
            try
            {
                using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();

                    // Query de inserción. Nota que llenamos CreatedAt y LastModifiedAt con la fecha actual.
                    command.CommandText = 
                    @"
                        INSERT INTO Users 
                        (Username, PasswordHash, Role, CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy, IsDeleted)
                        VALUES 
                        ($name, $hash, $role, $date, $creator, $date, $creator, 0)
                    ";

                    // Asignación de parámetros para prevenir Inyección SQL
                    command.Parameters.AddWithValue("$name", username);
                    command.Parameters.AddWithValue("$hash", HashPassword(password)); // Encriptamos antes de guardar
                    command.Parameters.AddWithValue("$role", role);
                    command.Parameters.AddWithValue("$date", DateTime.Now.ToString("o")); // Formato ISO 8601 para fechas
                    command.Parameters.AddWithValue("$creator", creatorName);

                    command.ExecuteNonQuery();
                    return true;
                }
            }
            catch (SqliteException)
            {
                // Probablemente el usuario ya existe (Unique constraint)
                Console.WriteLine("Error: El usuario ya existe o hubo un problema con la base de datos.");
                return false;
            }
        }

        /// <summary>
        /// [API del Módulo]
        /// Verifica las credenciales para iniciar sesión.
        /// Retorna el objeto User si es correcto, o null si falla.
        /// </summary>
        public User? Login(string username, string password)
        {
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                // Buscamos usuario activo (IsDeleted = 0)
                command.CommandText = 
                @"
                    SELECT Id, Username, PasswordHash, Role 
                    FROM Users 
                    WHERE Username = $name AND IsDeleted = 0
                ";
                command.Parameters.AddWithValue("$name", username);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var storedHash = reader.GetString(2);
                        // Verificamos si la contraseña ingresada coincide con el hash guardado
                        if (storedHash == HashPassword(password))
                        {
                            return new User
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Role = reader.GetString(3)
                            };
                        }
                    }
                }
            }
            return null; // Login fallido
        }

        /// <summary>
        /// [UTILIDAD - SEGURIDAD]
        /// Función privada para encriptar contraseñas usando SHA256.
        /// Convierte "12345" en una cadena ilegible de caracteres.
        /// </summary>
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}