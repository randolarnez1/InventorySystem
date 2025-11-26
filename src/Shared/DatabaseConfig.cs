using System;
using System.IO;

namespace InventorySystem.Shared
{
    /// <summary>
    /// [INFRAESTRUCTURA]
    /// Configuración centralizada para la base de datos SQLite.
    /// </summary>
    public static class DatabaseConfig
    {
        // Nombre del archivo físico de la base de datos
        private const string DbName = "inventory.db";

        /// <summary>
        /// [API Interna]
        /// Devuelve la cadena de conexión necesaria para SQLite.
        /// </summary>
        public static string ConnectionString
        {
            get
            {
                // Construye la ruta para que funcione en cualquier sistema operativo
                return $"Data Source={DbName}";
            }
        }
    }
}