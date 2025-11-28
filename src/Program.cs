//ARCHIVO TRABAJADO POR MAURICIO

using System; // Importa funcionalidades básicas del sistema (Console, DateTime, etc.)
using System.Collections.Generic; // Importa el uso de Listas (List<T>)

// --- ARQUITECTURA HEXAGONAL: IMPORTACIÓN DE CAPAS ---
// Aquí demostramos que el programa principal conecta todas las piezas.
using InventorySystem.Domain;        // Capa de DOMINIO: Entidades (User, Product, Batch)
using InventorySystem.Infraestructure; // Capa de INFRAESTRUCTURA: Base de Datos (Repositories)
using InventorySystem.Application;    // Capa de APLICACIÓN: Lógica de Negocio (Services)

namespace InventorySystem
{
    // [CAPA DE PRESENTACIÓN / UI]
    // Esta clase 'Program' actúa como la Interfaz de Usuario.
    // Su única responsabilidad es mostrar datos y capturar el teclado.
    // No contiene lógica compleja, solo delega tareas a los servicios.
    class Program
    {
        // --- INYECCIÓN DE DEPENDENCIAS (MANUAL) ---
        // Instanciamos los servicios como variables 'static' para tener una única instancia
        // compartida durante toda la vida de la aplicación.
        
        // Servicio de Usuarios: Se encarga del Login, Hashing y Seguridad.
        static UserService _userService = new UserService();//SERVICIO DE USUARIOS✅✅✅✅✅✅
        
        // Repositorio de Productos: Se encarga del CRUD del catálogo en la BD.
        static ProductRepository _productRepo = new ProductRepository();
        
        // Repositorio de Proveedores: Maneja la información de terceros.
        static StakeholderService _stakeholderService = new StakeholderService();//SERVICIO DE PROVEEDORES✅✅✅✅✅✅

        // Servicio de Stock: EL NÚCLEO. Maneja el algoritmo FIFO y los Lotes.
        static StockService _stockService = new StockService();
        
        // --- GESTIÓN DE SESIÓN ---
        // Variable para almacenar quién está usando el sistema actualmente.
        // Es necesario para la auditoría (Saber quién creó o borró algo).
        // El '?' indica que puede ser nulo (nadie logueado).
        static User? _currentUser;

        // PUNTO DE ENTRADA PRINCIPAL DEL PROGRAMA
        static void Main(string[] args)
        {
            // 1. Inicialización de Infraestructura
            // Llamamos al método que verifica que la base de datos física exista.
            InitializeDatabase();
            
            // 2. Semilla de Datos (Data Seeding)
            // Creamos un usuario Admin por defecto para no quedar bloqueados fuera del sistema.
            _userService.RegisterUser("admin", "admin123", "Admin", "SYSTEM");

            // --- BUCLE DE VIDA DE LA APLICACIÓN (GAME LOOP) ---
            // Usamos 'while(true)' para que el programa nunca se cierre por sí solo.

            while (true)//WHILE INFINITO PARA QUE EL PROGRAMA NUNCA SE CIERRE✅✅✅✅✅✅
            {
                // Reiniciamos la sesión actual a null.
                // Esto asegura que si el usuario cierra sesión, se le obligue a loguearse de nuevo.
                _currentUser = null;
                
                // Bucle de Login: Bloquea la pantalla hasta que _currentUser tenga un valor válido.
                while (_currentUser == null) ShowLoginScreen();

                // Bucle del Menú Principal: Mantiene al usuario dentro del sistema tras loguearse.
                bool logout = false; // Bandera para controlar la salida del bucle interno
                while (!logout)
                {
                    Console.Clear(); // Limpiamos la consola para mantener la interfaz ordenada
                    
                    // Mostramos cabecera con Feedback de Sesión (Nombre del usuario)
                    Console.WriteLine("=== SISTEMA DE INVENTARIO ===");
                    Console.WriteLine($"Usuario Activo: {_currentUser.Username} (Rol: {_currentUser.Role})");
                    Console.WriteLine("-----------------------------");
                    
                    // Opciones del Menú
                    Console.WriteLine("1. PRODUCTOS (Gestión del Catálogo)");
                    Console.WriteLine("2. PROVEEDORES (Gestión de Terceros)");
                    Console.WriteLine("3. MOVIMIENTOS (Compras y Ventas - FIFO)"); 
                    
                    // [SEGURIDAD - RENDERIZADO CONDICIONAL]
                    // Verificamos el Rol del usuario. Si NO es Admin, ocultamos la opción 4.
                    if (_currentUser.Role == "Admin") Console.WriteLine("4. USUARIOS (Administración)");

                    Console.WriteLine("5. Salir (Cerrar Sesión)");
                    Console.Write("\nSeleccione una opción: ");

                    // [ENRUTAMIENTO]
                    // Leemos la tecla y decidimos a qué módulo ir.
                    switch (Console.ReadLine())
                    {
                        case "1": ManageCatalog(); break;      // Ir al módulo de Productos
                        case "2": ManageStakeholders(); break; // Ir al módulo de Proveedores
                        case "3": ManageInventory(); break;    // Ir al módulo de Stock (Core)
                        case "4": 
                            // [SEGURIDAD EN PROFUNDIDAD]
                            // Volvemos a validar el rol aquí. Si un usuario normal intenta forzar
                            // la opción 4, el if lo bloquea.
                            if (_currentUser.Role == "Admin") ManageUsers(); 
                            break;
                        case "5": logout = true; break; // Cambiamos la bandera a true para romper el bucle
                    }
                }
            }
        }

        // Método auxiliar para inicializar tablas
        static void InitializeDatabase()
        {
            // Delegamos a cada servicio la responsabilidad de crear su propia tabla si no existe.
            _userService.EnsureTableExists();
            _productRepo.EnsureTableExists();
            _stakeholderService.EnsureTablesExist();
            _stockService.EnsureTableExists();
        }

        // Pantalla de Login
        static void ShowLoginScreen()
        {
            Console.Clear();
            Console.WriteLine("=== INICIO DE SESIÓN ===");
            Console.Write("Usuario: "); string user = Console.ReadLine() ?? ""; // ?? "" evita nulos
            Console.Write("Password: "); string pass = Console.ReadLine() ?? "";
            
            // Llamamos a la Capa de Aplicación (UserService) para validar.
            // El servicio se encarga de Hashear la contraseña y compararla con la BD.
            _currentUser = _userService.Login(user, pass);
            
            // Si devuelve null, es que falló.
            if (_currentUser == null) { Console.WriteLine("Credenciales incorrectas. Enter para reintentar."); Console.ReadKey(); }
        }

        // ==========================================
        //         MÓDULO 1: PRODUCTOS
        // ==========================================
        static void ManageCatalog()
        {
            bool back = false; // Control para volver al menú principal
            while (!back)
            {
                Console.Clear();
                Console.WriteLine("--- CATÁLOGO DE PRODUCTOS ---");
                
                // [CONSULTA A BD] Pedimos al repositorio la lista completa
                var list = _productRepo.GetAllProducts();
                
                // [RENDERIZADO DE TABLA] Usamos formato de columnas fijas {-4} {-20}
                Console.WriteLine("{0,-4} | {1,-20} | {2,-10} | {3,-10}", "ID", "NOMBRE", "SKU", "TIPO");
                Console.WriteLine(new string('-', 60)); // Línea separadora
                
                // Recorremos la lista y mostramos cada ítem
                foreach (var p in list)
                    Console.WriteLine("{0,-4} | {1,-20} | {2,-10} | {3,-10}", p.Id, Truncate(p.Name, 20), p.Sku, p.Category);

                // Sub-menú de acciones
                Console.WriteLine("\n[1] Nuevo Producto");
                Console.WriteLine("[2] Editar Nombre");
                Console.WriteLine("[3] Eliminar Producto");
                Console.WriteLine("[4] Volver");
                Console.Write("Opción: ");

                switch (Console.ReadLine())
                {
                    case "1": 
                        // --- CREAR PRODUCTO ---
                        Console.Write("Nombre: "); string n = Console.ReadLine();
                        Console.Write("SKU (Código único): "); string sku = Console.ReadLine();
                        
                        // Selección de Categoría (Mapeo de input usuario -> Enum del sistema)
                        Console.WriteLine("Categoría: [1] Alimentos [2] Electrónica");
                        var cat = (Console.ReadLine() == "2") ? ProductCategory.Electronics : ProductCategory.Groceries;
                        
                        // Selección de Perecedero (Bool)
                        Console.Write("¿Es Perecedero? (s/n): "); bool per = Console.ReadLine() == "s";
                        
                        // Instanciamos el objeto Product (Dominio) y lo enviamos al Repo (Infraestructura)
                        _productRepo.CreateProduct(new Product{Name=n, Sku=sku, Category=cat, IsPerishable=per}, _currentUser.Username);
                        break;
                        
                    case "2": 
                        // --- EDITAR PRODUCTO ---
                        Console.Write("Ingrese ID a Editar: "); 
                        // TryParse intenta convertir texto a número. Si falla, devuelve false y no entra al if.
                        //TRYPARSE INTENTA CONVERTIR TEXTO A NÚMERO✅✅✅✅✅✅
                        if(int.TryParse(Console.ReadLine(), out int ide)) //TRYPARSE INTENTA CONVERTIR TEXTO A NÚMERO✅✅✅✅✅✅
                        {
                            // Primero buscamos el objeto original
                            var p = _productRepo.GetProductById(ide);
                            if(p!=null){
                                Console.Write("Nuevo Nombre: "); p.Name = Console.ReadLine();
                                // Enviamos el objeto modificado para actualizar en BD
                                _productRepo.UpdateProduct(p, _currentUser.Username);
                            }
                        }
                        break;
                        
                    case "3": 
                        // --- ELIMINAR PRODUCTO ---
                        // Realizamos un Soft Delete (Borrado Lógico)
                        Console.Write("Ingrese ID a Borrar: "); 
                        if(int.TryParse(Console.ReadLine(), out int idd)) //TRYPARSE INTENTA CONVERTIR TEXTO A NÚMERO✅✅✅✅✅✅
                            _productRepo.DeleteProduct(idd, _currentUser.Username);
                        break;
                        
                    case "4": back = true; break; // Volver al menú principal
                }
            }
        }

        // ==========================================
        //         MÓDULO 2: PROVEEDORES
        // ==========================================
        static void ManageStakeholders()
        {
            // La lógica es idéntica a Productos (CRUD Básico), por lo que los comentarios aplican igual.
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
        //   MÓDULO 3: MOVIMIENTOS (EL NÚCLEO LÓGICO)
        // ==========================================
        static void ManageInventory()
        {
            bool back = false;
            while(!back)
            {
                Console.Clear();
                Console.WriteLine("--- RESUMEN DE STOCK ACTUAL ---");
                Console.WriteLine("{0,-4} | {1,-20} | {2,-10}", "ID", "PRODUCTO", "TOTAL");
                Console.WriteLine(new string('-', 40));

                // 1. Obtenemos todos los productos definidos
                var prods = _productRepo.GetAllProducts();
                
                // [VALIDACIÓN DE UX] Si no hay productos, avisamos para evitar confusión.
                if (prods.Count == 0) Console.WriteLine("      (El catálogo está vacío)");
                else
                {
                    foreach(var p in prods)
                    {
                        // [LÓGICA DE NEGOCIO EN TIEMPO REAL]
                        // No leemos un campo 'Stock' de la tabla Productos.
                        // Llamamos al StockService para que SUME los lotes activos en este preciso instante.
                        int stock = _stockService.GetTotalStock(p.Id);
                        
                        // [FEEDBACK VISUAL] Pintamos de rojo si el stock es crítico (0)
                        if(stock == 0) Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("{0,-4} | {1,-20} | {2,-10}", p.Id, Truncate(p.Name, 20), stock);
                        Console.ResetColor(); // Volvemos al color normal
                    }
                }
                Console.WriteLine(new string('-', 40));

                Console.WriteLine("\nACCIONES DE INVENTARIO:");
                Console.WriteLine("[1] 📥 REGISTRAR COMPRA (Entrada -> Crea un Lote)");
                Console.WriteLine("[2] 📤 REGISTRAR VENTA  (Salida -> Aplica FIFO)");
                Console.WriteLine("[3] 📋 VER DETALLE DE LOTES (Costos y Fechas)");
                Console.WriteLine("[4] Volver");
                Console.Write("Opción: ");

                var op = Console.ReadLine();

                if (op == "1") // --- OPCIÓN: COMPRA (ENTRADA) ---
                {
                    // [VALIDACIÓN DE INTEGRIDAD]
                    // Verificamos que existan productos y proveedores antes de empezar.
                    // Si falta alguno, bloqueamos la operación para evitar datos corruptos.
                    if (prods.Count == 0) {
                        Console.WriteLine("\n⚠️ Error: Primero cree productos en el catálogo.");
                        Console.ReadKey(); continue; // Reinicia el ciclo
                    }

                    var suppliers = _stakeholderService.GetAllSuppliers();
                    if (suppliers.Count == 0) {
                        Console.WriteLine("\n⚠️ Error: No hay proveedores. Registre uno en la Opción 2.");
                        Console.ReadKey(); continue;
                    }

                    Console.WriteLine("\n--- NUEVA COMPRA ---");
                    
                    // [AYUDA VISUAL] Mostramos la lista de proveedores para que el usuario elija.
                    Console.WriteLine("Proveedores Disponibles:");
                    foreach (var s in suppliers) Console.WriteLine($"ID: {s.Id} - {s.Name}");
                    
                    Console.Write("Ingrese ID Proveedor: ");
                    // Validamos que el ID sea un número Y que exista en la lista
                    if(!int.TryParse(Console.ReadLine(), out int sid) || suppliers.Find(s=>s.Id==sid)==null)//TRYPARSE INTENTA CONVERTIR TEXTO A NÚMERO✅✅✅✅✅✅
                     {
                        Console.WriteLine("❌ Proveedor no válido."); Console.ReadKey(); continue;
                    }

                    Console.Write("Ingrese ID Producto: "); 
                    if(!int.TryParse(Console.ReadLine(), out int pid)) continue;
                    
                    // Buscamos el objeto producto completo para verificar si es perecedero
                    var targetProduct = prods.Find(p => p.Id == pid);
                    if (targetProduct == null) {
                        Console.WriteLine("❌ Producto no existe."); Console.ReadKey(); continue;
                    }

                    Console.Write($"Cantidad a ingresar: "); 
                    int.TryParse(Console.ReadLine(), out int qty);
                    
                    Console.Write("Costo Unitario ($): "); 
                    decimal.TryParse(Console.ReadLine(), out decimal cost);

                    // [LÓGICA CONDICIONAL DE FECHAS]
                    DateTime? finalExpDate = null;
                    if (targetProduct.IsPerishable)
                    {
                        // Si es perecedero, OBLIGAMOS a poner fecha.
                        Console.ForegroundColor = ConsoleColor.Yellow;//FOREGROUND COLOR AMARILLO✅✅✅✅✅✅
                        Console.WriteLine("⚠️ PRODUCTO PERECEDERO: FECHA OBLIGATORIA.");
                        Console.ResetColor();
                        
                        bool validDate = false;
                        while (!validDate)
                        {
                            Console.Write("Fecha Caducidad (yyyy-mm-dd): ");
                            if (DateTime.TryParse(Console.ReadLine(), out DateTime d))
                            {
                                // Validamos que la fecha sea futura
                                if (d > DateTime.Now) {
                                    finalExpDate = d;
                                    validDate = true;
                                } else Console.WriteLine("❌ La fecha debe ser futura.");
                            }
                            else Console.WriteLine("❌ Formato incorrecto.");
                        }
                    }
                    // Si no es perecedero, 'finalExpDate' se queda en null.

                    // Creamos el objeto BATCH (Lote) que representa la mercancía física
                    var batch = new Batch
                    {
                        ProductId = pid, SupplierId = sid, Quantity = qty, CostPrice = cost,
                        EntryDate = DateTime.Now, ExpirationDate = finalExpDate
                    };

                    // Enviamos al servicio para guardar en BD
                    _stockService.RegisterEntry(batch, _currentUser.Username);
                    Console.WriteLine("✅ Compra registrada correctamente.");
                    Console.ReadKey();
                }
                else if (op == "2") // --- OPCIÓN: VENTA (SALIDA FIFO) ---
                {
                    Console.WriteLine("\n--- VENTA ---");
                    Console.Write("ID Producto a vender: ");
                    if(int.TryParse(Console.ReadLine(), out int pid)) {
                        // Verificamos stock disponible antes de intentar vender
                        int current = _stockService.GetTotalStock(pid);
                        if(current == 0) Console.WriteLine("❌ No hay stock disponible.");//VALIDACION DE ESTADO CON CONTADOR
                        else {
                            Console.Write($"Cantidad (Máx {current}): ");
                            int.TryParse(Console.ReadLine(), out int qty);
                            
                            // [MANEJO DE EXCEPCIONES]
                            // El algoritmo FIFO puede fallar si intentamos vender más de lo que hay.
                            // Envolvemos en try-catch para capturar el error y mostrarlo amigablemente.
                            try {
                                _stockService.RegisterExit(pid, qty, _currentUser.Username);
                                Console.WriteLine("✅ Venta registrada (FIFO aplicado).");
                            } catch(Exception e) { 
                                Console.WriteLine($"❌ Error de Negocio: {e.Message}"); 
                            }
                        }
                        Console.ReadKey();
                    }
                }
                else if (op == "3") // --- REPORTE DE LOTES ---
                {
                    ShowBatchesDetail(prods); // Llamamos a función auxiliar
                }
                else if (op == "4") back = true;
            }
        }

        // Función auxiliar para mostrar el detalle técnico de los lotes
        static void ShowBatchesDetail(List<Product> products)
        {
            Console.Clear();
            Console.WriteLine("--- DETALLE DE LOTES ACTIVOS ---");
            Console.WriteLine("{0,-5} | {1,-15} | {2,-8} | {3,-10} | {4,-12}", "LOTE", "PRODUCTO", "CANT.", "COSTO U.", "CADUCIDAD");
            Console.WriteLine(new string('-', 65));

            // Solicitamos al servicio SOLO los lotes que tienen cantidad > 0
            var batches = _stockService.GetAllActiveBatches(); 

            foreach (var b in batches)
            {
                // Cruzamos información: ID Producto -> Nombre Producto
                var p = products.Find(x => x.Id == b.ProductId);
                string pName = p != null ? Truncate(p.Name, 15) : "???";
                // Formateamos fecha o mostramos guion si es null
                string exp = b.ExpirationDate.HasValue ? b.ExpirationDate.Value.ToString("yyyy-MM-dd") : "-";

                Console.WriteLine("{0,-5} | {1,-15} | {2,-8} | {3,-10} | {4,-12}", 
                    b.Id, pName, b.Quantity, $"${b.CostPrice}", exp);
            }
            Console.WriteLine("\nPresione una tecla para volver...");
            Console.ReadKey();
        }

        // Módulo de Gestión de Usuarios (Solo Admin)
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
                // Forzamos el rol "Employee"
                _userService.RegisterUser(u, p, "Employee", _currentUser.Username);
            }
        }

        // Función "Helper" para cortar textos largos (evita que se rompa la tabla visualmente)
        static string Truncate(string s, int max) => (s?.Length ?? 0) > max ? s.Substring(0, max-3)+"..." : s ?? "";
    }
}