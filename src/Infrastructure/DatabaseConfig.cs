using System;
using System.IO;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using InventorySystem.Domain; // <--- AGREGAR ESTO (Vital para ver Product, User, etc)

namespace InventorySystem.Infraestructure
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