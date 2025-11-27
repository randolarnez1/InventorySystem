using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using InventorySystem.Domain; // Para ver 'Product'

namespace InventorySystem.Infraestructure
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

                // CORRECCIÓN: Ahora seleccionamos TAMBIÉN las columnas de auditoría
                command.CommandText = 
                @"
                    SELECT Id, Name, Sku, Category, IsPerishable, 
                           CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy
                    FROM Products 
                    WHERE IsDeleted = 0
                ";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Enum.TryParse(reader.GetString(3), out ProductCategory categoryEnum);

                        list.Add(new Product
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Sku = reader.GetString(2),
                            Category = categoryEnum,
                            IsPerishable = reader.GetBoolean(4),
                            
                            // Mapeamos los datos de auditoría para que se vean en consola
                            CreatedAt = DateTime.Parse(reader.GetString(5)),
                            CreatedBy = reader.GetString(6),
                            LastModifiedAt = DateTime.Parse(reader.GetString(7)),
                            LastModifiedBy = reader.GetString(8)
                        });
                    }
                }
            }
            return list;
        }
        /// <summary>
        /// [API] Actualiza un producto existente.
        /// Registra automáticamente quién lo modificó y cuándo.
        /// </summary>
        public void UpdateProduct(Product product, string editorName)
        {
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = 
                @"
                    UPDATE Products 
                    SET Name = $name, 
                        Category = $cat, 
                        IsPerishable = $perish,
                        LastModifiedAt = $date,
                        LastModifiedBy = $editor
                    WHERE Id = $id
                ";

                command.Parameters.AddWithValue("$name", product.Name);
                command.Parameters.AddWithValue("$cat", product.Category.ToString());
                command.Parameters.AddWithValue("$perish", product.IsPerishable ? 1 : 0);
                
                // Auditoría de edición
                command.Parameters.AddWithValue("$date", DateTime.Now.ToString("o"));
                command.Parameters.AddWithValue("$editor", editorName);
                command.Parameters.AddWithValue("$id", product.Id);

                command.ExecuteNonQuery();
            }
        }
        
        // Método auxiliar para buscar un solo producto por ID
        public Product? GetProductById(int id)
        {
            // Reutilizamos la lógica de GetAll pero filtrando por ID
            var all = GetAllProducts();
            return all.Find(p => p.Id == id);
        }

        /// <summary>
        /// [API] Marca un producto como eliminado (Soft Delete).
        /// </summary>
        public void DeleteProduct(int id, string userDeleting)
        {
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = 
                @"
                    UPDATE Products 
                    SET IsDeleted = 1,
                        DeletedAt = $date,
                        DeletedBy = $user
                    WHERE Id = $id
                ";

                command.Parameters.AddWithValue("$date", DateTime.Now.ToString("o"));
                command.Parameters.AddWithValue("$user", userDeleting);
                command.Parameters.AddWithValue("$id", id);

                command.ExecuteNonQuery();
            }
        }
    }
}