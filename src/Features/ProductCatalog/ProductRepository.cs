using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using InventorySystem.Shared;

namespace InventorySystem.Features.ProductCatalog
{
    /// <summary>
    /// [LOGICA DE NEGOCIO Y DATOS]
    /// Gestiona el ciclo de vida de los productos en la base de datos.
    /// </summary>
    public class ProductRepository
    {
        /// <summary>
        /// [INFRAESTRUCTURA]
        /// Crea la tabla Products asegurando unicidad en el SKU.
        /// </summary>
        public void EnsureTableExists()
        {
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = 
                @"
                    CREATE TABLE IF NOT EXISTS Products (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Sku TEXT NOT NULL UNIQUE,
                        Category TEXT NOT NULL,
                        IsPerishable INTEGER NOT NULL,
                        
                        -- Campos de Auditoría (Heredados)
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
        /// Crea un nuevo producto validando que el SKU no exista.
        /// </summary>
        public void CreateProduct(Product product, string creatorName)
        {
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = 
                @"
                    INSERT INTO Products 
                    (Name, Sku, Category, IsPerishable, CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy, IsDeleted)
                    VALUES 
                    ($name, $sku, $cat, $perish, $date, $creator, $date, $creator, 0)
                ";

                command.Parameters.AddWithValue("$name", product.Name);
                command.Parameters.AddWithValue("$sku", product.Sku);
                // Guardamos el Enum como String para que sea legible en la BD
                command.Parameters.AddWithValue("$cat", product.Category.ToString()); 
                // SQLite no tiene Boolean, usa 0 (false) y 1 (true)
                command.Parameters.AddWithValue("$perish", product.IsPerishable ? 1 : 0);
                
                // Auditoría
                command.Parameters.AddWithValue("$date", DateTime.Now.ToString("o"));
                command.Parameters.AddWithValue("$creator", creatorName);

                try 
                {
                    command.ExecuteNonQuery();
                }
                catch(SqliteException ex) when (ex.SqliteErrorCode == 19) // Error 19 es restricción UNIQUE
                {
                    throw new Exception($"El SKU '{product.Sku}' ya existe en el sistema.");
                }
            }
        }

        /// <summary>
        /// [API del Módulo]
        /// Obtiene todos los productos activos (no borrados).
        /// </summary>
        public List<Product> GetAllProducts()
        {
            var list = new List<Product>();
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = 
                @"
                    SELECT Id, Name, Sku, Category, IsPerishable 
                    FROM Products 
                    WHERE IsDeleted = 0
                ";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Convertimos el string de la DB de vuelta al Enum
                        Enum.TryParse(reader.GetString(3), out ProductCategory categoryEnum);

                        list.Add(new Product
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Sku = reader.GetString(2),
                            Category = categoryEnum,
                            IsPerishable = reader.GetBoolean(4)
                        });
                    }
                }
            }
            return list;
        }
    }
}