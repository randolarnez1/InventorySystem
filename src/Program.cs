//Este archivo lo trabajo MAURICIO nuevo comentario

using System;
using System.Collections.Generic;
using InventorySystem.Domain;
using InventorySystem.Infraestructure;
using InventorySystem.Application;

namespace InventorySystem
{
    class Program
    {
        // Servicios
        static UserService _userService = new UserService();
        static ProductRepository _productRepo = new ProductRepository();
        static StakeholderService _stakeholderService = new StakeholderService();
        static StockService _stockService = new StockService();
        
        // Usuario actual (necesario para guardar internamente el registro, aunque no lo mostremos)
        static User? _currentUser;

        static void Main(string[] args)
        {
            InitializeDatabase();
            _userService.RegisterUser("admin", "admin123", "Admin", "SYSTEM");

            while (true)
            {
                _currentUser = null;
                while (_currentUser == null) ShowLoginScreen();

                bool logout = false;
                while (!logout)
                {
                    Console.Clear();
                    // Cabecera simple
                    Console.WriteLine("=== SISTEMA DE INVENTARIO ===");
                    Console.WriteLine("1. PRODUCTOS (Catálogo)");
                    Console.WriteLine("2. PROVEEDORES");
                    Console.WriteLine("3. MOVIMIENTOS (Compras y Ventas)"); // Aquí es donde se manejan los batches
                    
                    if (_currentUser.Role == "Admin") Console.WriteLine("4. USUARIOS");

                    Console.WriteLine("5. Salir");
                    Console.Write("\nOpción: ");

                    switch (Console.ReadLine())
                    {
                        case "1": ManageCatalog(); break;
                        case "2": ManageStakeholders(); break;
                        case "3": ManageInventory(); break; // <-- AQUÍ AGREGAS LOS BATCHES
                        case "4": 
                            if (_currentUser.Role == "Admin") ManageUsers(); 
                            break;
                        case "5": logout = true; break;
                    }
                }
            }
        }

        static void InitializeDatabase()
        {
            _userService.EnsureTableExists();
            _productRepo.EnsureTableExists();
            _stakeholderService.EnsureTablesExist();
            _stockService.EnsureTableExists();
        }

        static void ShowLoginScreen()
        {
            Console.Clear();
            Console.WriteLine("=== LOGIN ===");
            Console.Write("Usuario: "); string user = Console.ReadLine() ?? "";
            Console.Write("Password: "); string pass = Console.ReadLine() ?? "";
            _currentUser = _userService.Login(user, pass);
            if (_currentUser == null) { Console.WriteLine("Error."); Console.ReadKey(); }
        }

        // ==========================================
        //         1. PRODUCTOS (Simplificado)
        // ==========================================
        static void ManageCatalog()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                Console.WriteLine("--- LISTA DE PRODUCTOS ---");
                var list = _productRepo.GetAllProducts();
                
                Console.WriteLine("{0,-4} | {1,-20} | {2,-10} | {3,-10}", "ID", "NOMBRE", "SKU", "TIPO");
                Console.WriteLine(new string('-', 60));
                foreach (var p in list)
                    Console.WriteLine("{0,-4} | {1,-20} | {2,-10} | {3,-10}", p.Id, Truncate(p.Name, 20), p.Sku, p.Category);

                Console.WriteLine("\n[1] Nuevo Producto");
                Console.WriteLine("[2] Editar Nombre");
                Console.WriteLine("[3] Eliminar Producto");
                Console.WriteLine("[4] Volver");
                Console.Write("Opción: ");

                switch (Console.ReadLine())
                {
                    case "1": 
                        Console.Write("Nombre: "); string n = Console.ReadLine();
                        Console.Write("SKU: "); string sku = Console.ReadLine();
                        Console.WriteLine("Categoría: [1] Alimentos [2] Electrónica");
                        var cat = (Console.ReadLine() == "2") ? ProductCategory.Electronics : ProductCategory.Groceries;
                        Console.Write("¿Perecedero? (s/n): "); bool per = Console.ReadLine() == "s";
                        _productRepo.CreateProduct(new Product{Name=n, Sku=sku, Category=cat, IsPerishable=per}, _currentUser.Username);
                        break;
                    case "2": 
                        Console.Write("ID a Editar: "); if(int.TryParse(Console.ReadLine(), out int ide)) {
                            var p = _productRepo.GetProductById(ide);
                            if(p!=null){
                                Console.Write("Nuevo Nombre: "); p.Name = Console.ReadLine();
                                _productRepo.UpdateProduct(p, _currentUser.Username);
                            }
                        }
                        break;
                    case "3": 
                        Console.Write("ID a Borrar: "); if(int.TryParse(Console.ReadLine(), out int idd)) 
                            _productRepo.DeleteProduct(idd, _currentUser.Username);
                        break;
                    case "4": back = true; break;
                }
            }
        }

        // ==========================================
        //         2. PROVEEDORES (Simplificado)
        // ==========================================
        static void ManageStakeholders()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                Console.WriteLine("--- PROVEEDORES ---");
                var list = _stakeholderService.GetAllSuppliers();
                foreach(var s in list) Console.WriteLine($"{s.Id} | {s.Name} | {s.ContactEmail}");

                Console.WriteLine("\n[1] Nuevo Proveedor");
                Console.WriteLine("[2] Editar");
                Console.WriteLine("[3] Eliminar");
                Console.WriteLine("[4] Volver");
                
                var op = Console.ReadLine();
                if(op == "4") back = true;
                else if(op == "1") {
                    Console.Write("Nombre: "); string n = Console.ReadLine();
                    Console.Write("Email: "); string e = Console.ReadLine();
                    _stakeholderService.CreateSupplier(new Supplier{Name=n, ContactEmail=e}, _currentUser.Username);
                }
                else if(op == "2") {
                    Console.Write("ID: "); int.TryParse(Console.ReadLine(), out int id);
                    var s = _stakeholderService.GetSupplierById(id);
                    if(s!=null) {
                        Console.Write("Nombre: "); string nn = Console.ReadLine(); if(nn!="") s.Name=nn;
                        Console.Write("Email: "); string ee = Console.ReadLine(); if(ee!="") s.ContactEmail=ee;
                        _stakeholderService.UpdateSupplier(s, _currentUser.Username);
                    }
                }
                else if(op == "3") {
                    Console.Write("ID: "); int.TryParse(Console.ReadLine(), out int id);
                    _stakeholderService.DeleteSupplier(id, _currentUser.Username);
                }
            }
        }

// ==========================================
        //   3. MOVIMIENTOS (Inventario - Lógica Completa)
        // ==========================================
        static void ManageInventory()
        {
            bool back = false;
            while(!back)
            {
                Console.Clear();
                Console.WriteLine("--- RESUMEN DE STOCK ---");
                Console.WriteLine("{0,-4} | {1,-20} | {2,-10}", "ID", "PRODUCTO", "TOTAL");
                Console.WriteLine(new string('-', 40));

                var prods = _productRepo.GetAllProducts();
                
                if (prods.Count == 0) Console.WriteLine("      (El catálogo está vacío)");
                else
                {
                    foreach(var p in prods)
                    {
                        int stock = _stockService.GetTotalStock(p.Id);
                        if(stock == 0) Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("{0,-4} | {1,-20} | {2,-10}", p.Id, Truncate(p.Name, 20), stock);
                        Console.ResetColor();
                    }
                }
                Console.WriteLine(new string('-', 40));

                Console.WriteLine("\nACCIONES:");
                Console.WriteLine("[1] 📥 REGISTRAR COMPRA (Entrada)");
                Console.WriteLine("[2] 📤 REGISTRAR VENTA  (Salida)");
                Console.WriteLine("[3] 📋 VER DETALLE DE LOTES (Costos y Fechas)"); // <--- NUEVO
                Console.WriteLine("[4] Volver");
                Console.Write("Opción: ");

                var op = Console.ReadLine();

                if (op == "1") // --- COMPRA ---
                {
                    if (prods.Count == 0) {
                        Console.WriteLine("\n⚠️ Primero cree productos en el catálogo.");
                        Console.ReadKey(); continue;
                    }

                    // 1. Validar Proveedores
                    var suppliers = _stakeholderService.GetAllSuppliers();
                    if (suppliers.Count == 0) {
                        Console.WriteLine("\n⚠️ No hay proveedores. Registre uno en la Opción 2.");
                        Console.ReadKey(); continue;
                    }

                    Console.WriteLine("\n--- NUEVA COMPRA ---");
                    
                    // Mostrar Proveedores
                    Console.WriteLine("Proveedores Disponibles:");
                    foreach (var s in suppliers) Console.WriteLine($"ID: {s.Id} - {s.Name}");
                    
                    Console.Write("ID Proveedor: ");
                    if(!int.TryParse(Console.ReadLine(), out int sid) || suppliers.Find(s=>s.Id==sid)==null) {
                        Console.WriteLine("❌ Proveedor no válido."); Console.ReadKey(); continue;
                    }

                    Console.Write("ID Producto: "); 
                    if(!int.TryParse(Console.ReadLine(), out int pid)) continue;
                    
                    // BUSCAMOS EL PRODUCTO PARA SABER SI ES PERECEDERO
                    var targetProduct = prods.Find(p => p.Id == pid);
                    if (targetProduct == null) {
                        Console.WriteLine("❌ Producto no encontrado."); Console.ReadKey(); continue;
                    }

                    Console.Write($"Cantidad de '{targetProduct.Name}': "); 
                    int.TryParse(Console.ReadLine(), out int qty);
                    
                    Console.Write("Costo Unitario ($): "); 
                    decimal.TryParse(Console.ReadLine(), out decimal cost);

                    // --- LÓGICA DE FECHAS MEJORADA ---
                    DateTime? finalExpDate = null;

                    if (targetProduct.IsPerishable)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("⚠️ ESTE PRODUCTO ES PERECEDERO. LA FECHA ES OBLIGATORIA.");
                        Console.ResetColor();
                        
                        bool validDate = false;
                        while (!validDate)
                        {
                            Console.Write("Fecha de Caducidad (yyyy-mm-dd): ");
                            if (DateTime.TryParse(Console.ReadLine(), out DateTime d))
                            {
                                if (d > DateTime.Now) {
                                    finalExpDate = d;
                                    validDate = true;
                                } else Console.WriteLine("❌ La fecha debe ser futura.");
                            }
                            else Console.WriteLine("❌ Formato incorrecto.");
                        }
                    }
                    else
                    {
                        // Si NO es perecedero, ni siquiera preguntamos.
                        finalExpDate = null;
                    }

                    var batch = new Batch
                    {
                        ProductId = pid, SupplierId = sid, Quantity = qty, CostPrice = cost,
                        EntryDate = DateTime.Now, ExpirationDate = finalExpDate
                    };

                    _stockService.RegisterEntry(batch, _currentUser.Username);
                    Console.WriteLine("✅ Compra registrada.");
                    Console.ReadKey();
                }
                else if (op == "2") // --- VENTA ---
                {
                    Console.WriteLine("\n--- VENTA ---");
                    Console.Write("ID Producto: ");
                    if(int.TryParse(Console.ReadLine(), out int pid)) {
                        int current = _stockService.GetTotalStock(pid);
                        if(current == 0) Console.WriteLine("❌ No hay stock.");
                        else {
                            Console.Write($"Cantidad (Máx {current}): ");
                            int.TryParse(Console.ReadLine(), out int qty);
                            try {
                                _stockService.RegisterExit(pid, qty, _currentUser.Username);
                                Console.WriteLine("✅ Venta registrada.");
                            } catch(Exception e) { Console.WriteLine($"❌ {e.Message}"); }
                        }
                        Console.ReadKey();
                    }
                }
                else if (op == "3") // --- VER DETALLE DE LOTES (Costos) ---
                {
                    ShowBatchesDetail(prods); // Llamamos a una nueva función auxiliar
                }
                else if (op == "4") back = true;
            }
        }

        // Función auxiliar para mostrar la tabla detallada con Costos
        static void ShowBatchesDetail(List<Product> products)
        {
            Console.Clear();
            Console.WriteLine("--- DETALLE DE LOTES ACTIVOS (COSTOS REALES) ---");
            Console.WriteLine("{0,-5} | {1,-15} | {2,-8} | {3,-10} | {4,-12}", "LOTE", "PRODUCTO", "CANT.", "COSTO U.", "CADUCIDAD");
            Console.WriteLine(new string('-', 65));

            // Hack rápido: Usamos reflexión interna o lógica directa.
            // Para hacerlo limpio, necesitamos un método en StockService que nos de los lotes.
            // Como no queremos complicar StockService ahora, haremos una consulta rápida aquí 
            // O idealmente agregamos un método en StockService (ver abajo).
            
            // POR AHORA: Agregaremos el método necesario en StockService rápidamente.
            // (Ver instrucciones abajo para agregar 'GetAllActiveBatches' en StockService)
            var batches = _stockService.GetAllActiveBatches(); 

            foreach (var b in batches)
            {
                var p = products.Find(x => x.Id == b.ProductId);
                string pName = p != null ? Truncate(p.Name, 15) : "???";
                string exp = b.ExpirationDate.HasValue ? b.ExpirationDate.Value.ToString("yyyy-MM-dd") : "-";

                Console.WriteLine("{0,-5} | {1,-15} | {2,-8} | {3,-10} | {4,-12}", 
                    b.Id, pName, b.Quantity, $"${b.CostPrice}", exp);
            }
            Console.WriteLine("\nPresione una tecla para volver...");
            Console.ReadKey();
        }

        static void ManageUsers()
        {
            Console.Clear();
            Console.WriteLine("--- USUARIOS ---");
            var users = _userService.GetAllUsers();
            foreach(var u in users) Console.WriteLine($"{u.Id} - {u.Username} ({u.Role})");
            
            Console.WriteLine("\n[1] Crear Empleado");
            Console.WriteLine("[2] Volver");
            if(Console.ReadLine() == "1") {
                Console.Write("User: "); string u = Console.ReadLine();
                Console.Write("Pass: "); string p = Console.ReadLine();
                _userService.RegisterUser(u, p, "Employee", _currentUser.Username);
            }
        }

        static string Truncate(string s, int max) => (s?.Length ?? 0) > max ? s.Substring(0, max-3)+"..." : s ?? "";
    }
}