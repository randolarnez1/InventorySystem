// Este archivo fue echo por Manuel (Arquitectura).
// CAPA: de la Infraestructura (Conexiones y Repositorios).

using System;
using System.IO;
using Microsoft.Data.Sqlite; // Librería oficial de Microsoft para interactuar con SQLite.
using System.Collections.Generic;
using InventorySystem.Domain; // este codigo importa el Dominio para posibles referencias a entidades (User, Product, etc.).

namespace InventorySystem.Infraestructure
{
    /// <summary>
    /// [INFRAESTRUCTURA - CONFIGURACIÓN ESTATICA]
    /// Clase de utilidad que centraliza toda la configuración y acceso inicial a la base de datos SQLite.
    ///
    /// PROPÓSITO:
    /// 1. se debe de garantizar que solo haya una forma de construir la cadena de conexión.
    /// 2. tambien se tiene que abstraer el nombre del archivo de la base de datos del resto de la aplicación.
    /// 3. Aquí se ejecutará la lógica inicial de creación de tablas (Migrations/Setup).
    /// </summary>
    // Es 'static' porque no necesita crear instancias (objetos) de sí misma;
    // sus métodos y propiedades son de acceso global y directo.
    public static class DatabaseConfig
    {
        // este es el campo privado y constante: se define el nombre del archivo físico que se creará en el disco.
        // Usar una constante asegura que el nombre del archivo NUNCA cambiará accidentalmente u otros motivos X.
        private const string DbName = "inventory.db";

        /// <summary>
        /// [API Interna]
        /// en esta Propiedad solo se hace la lectura que proporcionara la cadena de conexión completa
        /// requerida por el driver Sqlite para abrir o crear el archivo de la base de datos.
        /// </summary>
        public static string ConnectionString
        {
            get
            {
                // en la sintaxis "Data Source=..." es el formato estándar que espera SQLite.
                // ademas se construye la ruta para que el archivo se ubica en el mismo directorio
                // donde se ejecuta la aplicación, lo que simplifica la configuración.
                return $"Data Source={DbName}";
            }
        }
        
        // --- FUNCIÓN PENDIENTE (MIGRATIONS / SETUP) ---
        
        /// <summary>
        /// [FUNCIÓN PENDIENTE DE IMPLEMENTACIÓN]
        /// Este método que será invocado al inicio del programa (por UserService o Program.cs).
        /// Su misión es verificar si las tablas (User, Product, Supplier, Batch) existen.
        /// Si no existen, las crea ejecutando sentencias SQL DDL (Data Definition Language).
        /// </summary>
        public static void EnsureDatabaseAndTablesExist()
        {
             Console.WriteLine($"[DB_INFO] Verificando conexión a: {ConnectionString}");

            // Aquí se usaría el bloque 'using (var connection = new SqliteConnection(ConnectionString))'
            // para abrir la base de datos, ejecutar comandos como CREATE TABLE IF NOT EXISTS...
            // para las entidades AuditableEntity, User, Product, Supplier, Batch, etc.
            
            // Por ahora, solo mostramos un mensaje de confirmación.
        }
    }
}