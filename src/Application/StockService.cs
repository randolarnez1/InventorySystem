using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using InventorySystem.Domain;
using InventorySystem.Infraestructure; // <--- AHORA SÍ FUNCIONARÁ

namespace InventorySystem.Application
{
    /// <summary>
    /// [LOGICA DE NEGOCIO AVANZADA]
    /// Gestiona las entradas (compras) y salidas (ventas) aplicando reglas estrictas.
    /// Aquí reside el algoritmo FIFO.
    /// </summary>
    public class StockService
    {
        /// <summary>
        /// [INFRAESTRUCTURA]
        /// Crea la tabla Batches.
        /// </summary>
        public void EnsureTableExists()
        {
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = 
                @"
                    CREATE TABLE IF NOT EXISTS Batches (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ProductId INTEGER NOT NULL,
                        SupplierId INTEGER NOT NULL,
                        Quantity INTEGER NOT NULL,
                        CostPrice REAL NOT NULL,
                        EntryDate TEXT NOT NULL,
                        ExpirationDate TEXT,
                        
                        -- Auditoría
                        CreatedAt TEXT NOT NULL, CreatedBy TEXT NOT NULL,
                        LastModifiedAt TEXT NOT NULL, LastModifiedBy TEXT NOT NULL,
                        IsDeleted INTEGER DEFAULT 0, DeletedAt TEXT, DeletedBy TEXT
                    );
                ";
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// [API - ENTRADA DE STOCK]
        /// Registra una compra creando un NUEVO lote.
        /// No modificamos productos existentes, siempre agregamos historia nueva.
        /// </summary>
        public void RegisterEntry(Batch newBatch, string creatorName)
        {
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = 
                @"
                    INSERT INTO Batches 
                    (ProductId, SupplierId, Quantity, CostPrice, EntryDate, ExpirationDate, 
                     CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy, IsDeleted)
                    VALUES 
                    ($prodId, $supId, $qty, $cost, $entry, $exp, 
                     $date, $creator, $date, $creator, 0)
                ";

                command.Parameters.AddWithValue("$prodId", newBatch.ProductId);
                command.Parameters.AddWithValue("$supId", newBatch.SupplierId);
                command.Parameters.AddWithValue("$qty", newBatch.Quantity);
                command.Parameters.AddWithValue("$cost", newBatch.CostPrice);
                command.Parameters.AddWithValue("$entry", newBatch.EntryDate.ToString("o"));
                
                // Manejo de nulos para fecha de expiración
                if (newBatch.ExpirationDate.HasValue)
                    command.Parameters.AddWithValue("$exp", newBatch.ExpirationDate.Value.ToString("o"));
                else
                    command.Parameters.AddWithValue("$exp", DBNull.Value);

                command.Parameters.AddWithValue("$date", DateTime.Now.ToString("o"));
                command.Parameters.AddWithValue("$creator", creatorName);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// [API - SALIDA DE STOCK (FIFO)]
        /// Esta función es el corazón del sistema.
        /// Descuenta la cantidad solicitada de los lotes más viejos primero.
        /// </summary>
        /// <param name="productId">ID del producto a vender</param>
        /// <param name="quantityRequired">Cantidad que el cliente quiere comprar</param>
        /// <param name="userAuditor">Usuario que realiza la venta</param>
        public void RegisterExit(int productId, int quantityRequired, string userAuditor)
        {
            // Paso 1: Obtener todos los lotes con stock, ORDENADOS por fecha de entrada (Más viejos primero)
            var batches = GetBatchesForProduct(productId);
            int quantityToDeduct = quantityRequired;

            // Verificamos si hay stock suficiente en total antes de empezar
            int totalStock = 0;
            foreach(var b in batches) totalStock += b.Quantity;

            if (totalStock < quantityRequired)
            {
                throw new Exception($"Stock insuficiente. Disponible: {totalStock}, Solicitado: {quantityRequired}");
            }

            // Paso 2: Iterar y descontar
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();

                foreach (var batch in batches)
                {
                    if (quantityToDeduct <= 0) break; // Ya satisficimos la demanda

                    int deduction = 0;

                    if (batch.Quantity >= quantityToDeduct)
                    {
                        // CASO A: El lote tiene suficiente para cubrir todo lo que falta
                        deduction = quantityToDeduct;
                        batch.Quantity -= deduction;
                        quantityToDeduct = 0;
                    }
                    else
                    {
                        // CASO B: El lote no alcanza, tomamos todo lo que tiene y pasamos al siguiente
                        deduction = batch.Quantity;
                        quantityToDeduct -= deduction;
                        batch.Quantity = 0; // Lote agotado
                    }

                    // Paso 3: Actualizar el lote en la base de datos
                    var command = connection.CreateCommand();
                    command.CommandText = 
                    @"
                        UPDATE Batches 
                        SET Quantity = $qty, 
                            LastModifiedAt = $date, 
                            LastModifiedBy = $user 
                        WHERE Id = $id
                    ";
                    
                    command.Parameters.AddWithValue("$qty", batch.Quantity);
                    command.Parameters.AddWithValue("$date", DateTime.Now.ToString("o"));
                    command.Parameters.AddWithValue("$user", userAuditor);
                    command.Parameters.AddWithValue("$id", batch.Id);
                    
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// [UTILIDAD INTERNA]
        /// Busca lotes activos de un producto ordenados por antigüedad (FIFO).
        /// </summary>
        private List<Batch> GetBatchesForProduct(int productId)
        {
            var list = new List<Batch>();
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                // WHERE Quantity > 0: Solo nos interesan lotes con mercadería
                // ORDER BY EntryDate ASC: Esto asegura el FIFO (Primero lo viejo)
                command.CommandText = 
                @"
                    SELECT Id, Quantity, EntryDate 
                    FROM Batches 
                    WHERE ProductId = $pid AND Quantity > 0 AND IsDeleted = 0
                    ORDER BY EntryDate ASC
                ";
                command.Parameters.AddWithValue("$pid", productId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Batch
                        {
                            Id = reader.GetInt32(0),
                            Quantity = reader.GetInt32(1),
                            EntryDate = DateTime.Parse(reader.GetString(2))
                        });
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// [API - CONSULTA]
        /// Obtiene la suma total de stock de un producto (sumando todos sus lotes).
        /// </summary>
        public int GetTotalStock(int productId)
        {
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT SUM(Quantity) FROM Batches WHERE ProductId = $pid AND IsDeleted = 0";
                command.Parameters.AddWithValue("$pid", productId);

                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
                return 0;
            }
        }

        /// <summary>
        /// [API - CONSULTA]
        /// Obtiene todos los lotes con stock positivo para reportes detallados.
        /// </summary>
        public List<Batch> GetAllActiveBatches()
        {
            var list = new List<Batch>();
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                
                // Traemos todos los lotes que tienen cantidad > 0 y no están borrados
                command.CommandText = 
                @"
                    SELECT Id, ProductId, SupplierId, Quantity, CostPrice, EntryDate, ExpirationDate 
                    FROM Batches 
                    WHERE Quantity > 0 AND IsDeleted = 0
                    ORDER BY ProductId ASC, EntryDate ASC
                ";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime? expDate = null;
                        if (!reader.IsDBNull(6)) expDate = DateTime.Parse(reader.GetString(6));

                        list.Add(new Batch
                        {
                            Id = reader.GetInt32(0),
                            ProductId = reader.GetInt32(1),
                            SupplierId = reader.GetInt32(2),
                            Quantity = reader.GetInt32(3),
                            CostPrice = reader.GetDecimal(4),
                            EntryDate = DateTime.Parse(reader.GetString(5)),
                            ExpirationDate = expDate
                        });
                    }
                }
            }
            return list;
        }
    }
}