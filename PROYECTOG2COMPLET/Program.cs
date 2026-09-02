using System;
using System.Globalization;
using System.IO;

namespace Proyecto2_SistemaProduccion
{
    class Program
    {
        // ========================================
        // BLOQUE 1: CONFIGURACIÓN GLOBAL
        // ========================================

        static string NombreArchivoActual = $"produccion_{DateTime.Today.Year}.txt";
        const int MAX_REGISTROS = 100;
        const int META_DEFECTO = 120;

        // Umbrales de margen para decidir viabilidad de producción (ajustables)
        const double MARGEN_MINIMO = 10.0;   // debajo de esto: NO VIABLE
        const double MARGEN_BUENO = 25.0;    // encima de esto: VIABLE

        // Arreglos paralelos (base de datos en memoria)
        static DateTime[] fechas = new DateTime[MAX_REGISTROS];
        static string[] lineasProd = new string[MAX_REGISTROS];
        static int[] turnos = new int[MAX_REGISTROS];
        static int[] unidades = new int[MAX_REGISTROS];
        static string[] operarios = new string[MAX_REGISTROS];
        static double[] costosMateriaPrima = new double[MAX_REGISTROS]; // costo unitario ($/unidad)
        static double[] preciosVenta = new double[MAX_REGISTROS];       // precio de venta unitario ($/unidad)

        static int totalRegistros = 0;

        // ========================================
        // BLOQUE 2: MENÚ PRINCIPAL
        // ========================================

        static void Main(string[] args)
        {
            CargarDatosDesdeArchivo();

            int opcion;
            do
            {
                Console.Clear();
                MostrarEncabezado();
                MostrarMenu();

                Console.Write("Seleccione una opción: ");
                if (!int.TryParse(Console.ReadLine(), out opcion))
                {
                    opcion = 0;
                }

                Console.Clear();
                ProcesarOpcion(opcion);

                if (opcion != 10)
                {
                    Console.WriteLine("\nPresione cualquier tecla para volver al menú...");
                    Console.ReadKey();
                }

            } while (opcion != 10);

            GuardarDatosEnArchivo();
            Console.WriteLine("Datos guardados correctamente. ¡Hasta luego!");
        }

        static void MostrarEncabezado()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==============================================");
            Console.WriteLine("     OPTIMACODE INDUSTRIAL - PRODUCCIÓN");
            Console.WriteLine($"     Gestión {DateTime.Today.Year}");
            Console.WriteLine("==============================================");
            Console.ResetColor();
        }

        static void MostrarMenu()
        {
            Console.WriteLine("\nMENÚ PRINCIPAL:");
            Console.WriteLine("1. Registrar producción");
            Console.WriteLine("2. Listar registros");
            Console.WriteLine("3. Calcular eficiencia de un registro");
            Console.WriteLine("4. Reporte por línea");
            Console.WriteLine("5. Reporte por turno");
            Console.WriteLine("6. Mejor y peor día del mes");
            Console.WriteLine("7. Comparativa mensual con años anteriores");
            Console.WriteLine("8. Reporte de costos y viabilidad de producción");
            Console.WriteLine("9. Buscar registro");
            Console.WriteLine("10. Salir del sistema");
            Console.WriteLine("----------------------------------------------");
        }

        // ========================================
        // BLOQUE 3: LÓGICA DE NEGOCIO
        // ========================================

        static void ProcesarOpcion(int opcion)
        {
            switch (opcion)
            {
                case 1: RegistrarProduccion(); break;
                case 2: ListarRegistros(); break;
                case 3: CalcularEficienciaRegistro(); break;
                case 4: ReportePorLinea(); break;
                case 5: ReportePorTurno(); break;
                case 6: MejorYPeorDia(); break;
                case 7: ComparativaMensualAnios(); break;
                case 8: ReporteCostosViabilidad(); break;
                case 9: BuscarRegistro(); break;
                case 10: break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR: Opción no válida.");
                    Console.ResetColor();
                    break;
            }
        }

        // Lee texto capturando la tecla física [Esc] o palabras clave (0, A, ESC)
        static string LeerTextoConEscape(out bool fueEscape)
        {
            fueEscape = false;
            string entrada = "";

            while (true)
            {
                ConsoleKeyInfo tecla = Console.ReadKey(true);

                if (tecla.Key == ConsoleKey.Escape)
                {
                    fueEscape = true;
                    Console.WriteLine();
                    return "";
                }

                if (tecla.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    string resultado = entrada.Trim();

                    if (resultado.Equals("ESC", StringComparison.OrdinalIgnoreCase) ||
                        resultado.Equals("0") ||
                        resultado.Equals("A", StringComparison.OrdinalIgnoreCase) ||
                        resultado.Equals("ATRAS", StringComparison.OrdinalIgnoreCase))
                    {
                        fueEscape = true;
                        return "";
                    }

                    return resultado;
                }

                if (tecla.Key == ConsoleKey.Backspace)
                {
                    if (entrada.Length > 0)
                    {
                        entrada = entrada.Substring(0, entrada.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(tecla.KeyChar))
                {
                    entrada += tecla.KeyChar;
                    Console.Write(tecla.KeyChar);
                }
            }
        }

        // ---------- Opción 1: Registrar producción ----------
        static void RegistrarProduccion()
        {
            if (totalRegistros >= MAX_REGISTROS)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Memoria llena. No se pueden registrar más datos.");
                Console.ResetColor();
                return;
            }

            DateTime fechaRegistro = DateTime.Today;
            string linea = "";
            int turno = 1;
            int cantidad = 0;
            string operario = "";
            double costoMP = 0;
            double precioVenta = 0;

            int paso = 1;

            while (paso >= 1 && paso <= 8)
            {
                Console.Clear();
                MostrarEncabezado();
                Console.WriteLine("--- REGISTRAR PRODUCCIÓN ---\n");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("(Presione la tecla [Esc] o escriba '0' para retroceder / cancelar)\n");
                Console.ResetColor();

                switch (paso)
                {
                    case 1: // Fecha
                        Console.WriteLine("¿De qué fecha es la producción que va a ingresar?");
                        Console.WriteLine($"1. Fecha de hoy ({DateTime.Today:dd/MM/yyyy})");
                        Console.WriteLine("2. Otra fecha específica (ej: días anteriores o fin de mes)");
                        Console.WriteLine("0. Cancelar y salir al menú principal");
                        Console.Write("\nSeleccione una opción: ");

                        string opcFecha = LeerTextoConEscape(out bool escPaso1);

                        if (escPaso1 || opcFecha == "0")
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Operación cancelada. Regresando...");
                            Console.ResetColor();
                            return;
                        }
                        else if (opcFecha == "1")
                        {
                            fechaRegistro = DateTime.Today;
                            paso++;
                        }
                        else if (opcFecha == "2")
                        {
                            bool fechaValida = false;
                            while (!fechaValida)
                            {
                                Console.Write($"\nIngrese la fecha (dd/MM/{DateTime.Today.Year}) [o tecla Esc para volver]: ");
                                string inputFecha = LeerTextoConEscape(out bool escFecha);

                                if (escFecha)
                                {
                                    break;
                                }

                                if (DateTime.TryParseExact(inputFecha, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaRegistro))
                                {
                                    if (fechaRegistro.Year == DateTime.Today.Year && fechaRegistro <= DateTime.Today)
                                    {
                                        fechaValida = true;
                                        paso++;
                                    }
                                    else if (fechaRegistro.Year != DateTime.Today.Year)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"Año inválido. Solo se admiten registros del año actual ({DateTime.Today.Year}).");
                                        Console.ResetColor();
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("Fecha inválida. No se pueden registrar fechas futuras.");
                                        Console.ResetColor();
                                    }
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"Formato incorrecto. Use el formato exacto día/mes/año (ej: 26/08/{DateTime.Today.Year}).");
                                    Console.ResetColor();
                                }
                            }
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Opción no válida.");
                            Console.ResetColor();
                            Console.ReadKey();
                        }
                        break;

                    case 2: // Línea de producción
                        Console.WriteLine($"[Fecha seleccionada: {fechaRegistro:dd/MM/yyyy}]\n");
                        Console.Write("Línea de producción (ej: Linea) [o tecla Esc para volver a Fecha]: ");
                        string inputLinea = LeerTextoConEscape(out bool escPaso2);

                        if (escPaso2)
                        {
                            paso--;
                        }
                        else
                        {
                            linea = string.IsNullOrWhiteSpace(inputLinea) ? "General" : inputLinea;
                            paso++;
                        }
                        break;

                    case 3: // Turno
                        Console.WriteLine($"[Fecha: {fechaRegistro:dd/MM/yyyy} | Línea: {linea}]\n");
                        Console.Write("Turno (1 Mañana, 2 Tarde, 3 Noche) [o tecla Esc para volver a Línea]: ");
                        string inputTurno = LeerTextoConEscape(out bool escPaso3);

                        if (escPaso3)
                        {
                            paso--;
                        }
                        else if (int.TryParse(inputTurno, out int t) && t >= 1 && t <= 3)
                        {
                            turno = t;
                            paso++;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Entrada inválida. Debe ingresar 1, 2 o 3.");
                            Console.ResetColor();
                            Console.WriteLine("Presione cualquier tecla para corregir...");
                            Console.ReadKey();
                        }
                        break;

                    case 4: // Unidades
                        Console.WriteLine($"[Fecha: {fechaRegistro:dd/MM/yyyy} | Línea: {linea} | Turno: {turno}]\n");
                        Console.Write("Unidades producidas [o tecla Esc para volver a Turno]: ");
                        string inputUnid = LeerTextoConEscape(out bool escPaso4);

                        if (escPaso4)
                        {
                            paso--;
                        }
                        else if (int.TryParse(inputUnid, out int u) && u > 0)
                        {
                            cantidad = u;
                            paso++;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Entrada inválida. Ingrese una cantidad numérica mayor a 0.");
                            Console.ResetColor();
                            Console.WriteLine("Presione cualquier tecla para corregir...");
                            Console.ReadKey();
                        }
                        break;

                    case 5: // Operario
                        Console.WriteLine($"[Fecha: {fechaRegistro:dd/MM/yyyy} | Línea: {linea} | Turno: {turno} | Unid: {cantidad}]\n");
                        Console.Write("Nombre del operario [o tecla Esc para volver a Unidades]: ");
                        string inputOp = LeerTextoConEscape(out bool escPaso5);

                        if (escPaso5)
                        {
                            paso--;
                        }
                        else if (string.IsNullOrWhiteSpace(inputOp))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("El nombre del operario es obligatorio.");
                            Console.ResetColor();
                            Console.WriteLine("Presione cualquier tecla para corregir...");
                            Console.ReadKey();
                        }
                        else
                        {
                            operario = inputOp;
                            paso++;
                        }
                        break;

                    case 6: // Costo de materia prima por unidad
                        Console.WriteLine($"[Fecha: {fechaRegistro:dd/MM/yyyy} | Línea: {linea} | Turno: {turno} | Unid: {cantidad} | Operario: {operario}]\n");
                        Console.Write("Costo de materia prima por unidad, en $ (ej: 3.50) [o tecla Esc para volver a Operario]: ");
                        string inputCosto = LeerTextoConEscape(out bool escPaso6);

                        if (escPaso6)
                        {
                            paso--;
                        }
                        else if (double.TryParse(inputCosto, NumberStyles.Float, CultureInfo.InvariantCulture, out double c) && c >= 0)
                        {
                            costoMP = c;
                            paso++;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Entrada inválida. Ingrese un costo numérico (0 o mayor), usando punto decimal.");
                            Console.ResetColor();
                            Console.WriteLine("Presione cualquier tecla para corregir...");
                            Console.ReadKey();
                        }
                        break;

                    case 7: // Precio de venta por unidad
                        Console.WriteLine($"[Costo materia prima: ${costoMP:F2} por unidad]\n");
                        Console.Write("Precio de venta por unidad, en $ (ej: 6.00) [o tecla Esc para volver a Costo]: ");
                        string inputPrecio = LeerTextoConEscape(out bool escPaso7);

                        if (escPaso7)
                        {
                            paso--;
                        }
                        else if (double.TryParse(inputPrecio, NumberStyles.Float, CultureInfo.InvariantCulture, out double p) && p > 0)
                        {
                            precioVenta = p;
                            paso++;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Entrada inválida. Ingrese un precio numérico mayor a 0, usando punto decimal.");
                            Console.ResetColor();
                            Console.WriteLine("Presione cualquier tecla para corregir...");
                            Console.ReadKey();
                        }
                        break;

                    case 8: // Confirmación final
                        double margenUnit = precioVenta - costoMP;
                        double margenPorc = precioVenta > 0 ? (margenUnit / precioVenta) * 100.0 : 0;
                        string nivelPrevio;
                        ConsoleColor colorPrevio;
                        ClasificarViabilidad(margenPorc, out nivelPrevio, out colorPrevio);

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("==============================================");
                        Console.WriteLine("          REVISIÓN DE DATOS A GUARDAR         ");
                        Console.WriteLine("==============================================");
                        Console.ResetColor();
                        Console.WriteLine($" Fecha:            {fechaRegistro:dd/MM/yyyy}");
                        Console.WriteLine($" Línea:            {linea}");
                        Console.WriteLine($" Turno:            Turno {turno}");
                        Console.WriteLine($" Unidades:         {cantidad}");
                        Console.WriteLine($" Operario:         {operario}");
                        Console.WriteLine($" Costo materia p.: ${costoMP:F2} / unidad");
                        Console.WriteLine($" Precio de venta:  ${precioVenta:F2} / unidad");
                        Console.WriteLine($" Margen unitario:  ${margenUnit:F2}  ({margenPorc:F1}%)");
                        Console.ForegroundColor = colorPrevio;
                        Console.WriteLine($" Viabilidad:       {nivelPrevio}");
                        Console.ResetColor();
                        Console.WriteLine("----------------------------------------------");

                        Console.WriteLine("\nOpciones de confirmación:");
                        Console.WriteLine(" [S]   Guardar registro");
                        Console.WriteLine(" [Esc] Volver atrás (Editar Precio de venta)");
                        Console.WriteLine(" [R]   Reiniciar formulario completo");
                        Console.WriteLine(" [C]   Cancelar y descartar");
                        Console.Write("\nElija una opción (S / Esc / R / C): ");

                        string conf = LeerTextoConEscape(out bool escFinal);

                        if (escFinal)
                        {
                            paso = 7;
                        }
                        else if (conf.ToUpper() == "S")
                        {
                            fechas[totalRegistros] = fechaRegistro;
                            lineasProd[totalRegistros] = linea;
                            turnos[totalRegistros] = turno;
                            unidades[totalRegistros] = cantidad;
                            operarios[totalRegistros] = operario;
                            costosMateriaPrima[totalRegistros] = costoMP;
                            preciosVenta[totalRegistros] = precioVenta;
                            totalRegistros++;

                            GuardarDatosEnArchivo();

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"\n✓ Registro guardado exitosamente en el archivo.");
                            Console.ResetColor();
                            paso = 9;
                        }
                        else if (conf.ToUpper() == "R")
                        {
                            paso = 1;
                        }
                        else if (conf.ToUpper() == "C")
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("\nRegistro cancelado. No se modificó ningún dato.");
                            Console.ResetColor();
                            paso = 0;
                        }
                        break;
                }
            }
        }

        // ---------- Funciones de dibujo de tablas ----------

        static void ImprimirSeparador(int[] anchos)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("+");
            for (int i = 0; i < anchos.Length; i++)
            {
                Console.Write(new string('-', anchos[i] + 2));
                Console.Write("+");
            }
            Console.WriteLine();
            Console.ResetColor();
        }

        static void ImprimirFila(int[] anchos, string[] valores, ConsoleColor color)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("|");
            Console.ResetColor();

            for (int i = 0; i < anchos.Length; i++)
            {
                Console.Write(" ");
                Console.ForegroundColor = color;
                Console.Write(valores[i].PadRight(anchos[i]));
                Console.ResetColor();
                Console.Write(" ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("|");
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        static void ClasificarEficiencia(double eficiencia, out string nivel, out ConsoleColor color)
        {
            if (eficiencia < 70)
            {
                nivel = "CRÍTICO";
                color = ConsoleColor.Red;
            }
            else if (eficiencia < 90)
            {
                nivel = "PRECAUCIÓN";
                color = ConsoleColor.Yellow;
            }
            else
            {
                nivel = "ACEPTABLE";
                color = ConsoleColor.Green;
            }
        }

        // Clasifica la viabilidad de producción según el % de margen (precioVenta vs costoMateriaPrima)
        static void ClasificarViabilidad(double margenPorcentaje, out string nivel, out ConsoleColor color)
        {
            if (margenPorcentaje < MARGEN_MINIMO)
            {
                nivel = "NO VIABLE";
                color = ConsoleColor.Red;
            }
            else if (margenPorcentaje < MARGEN_BUENO)
            {
                nivel = "PRECAUCIÓN";
                color = ConsoleColor.Yellow;
            }
            else
            {
                nivel = "VIABLE";
                color = ConsoleColor.Green;
            }
        }

        // ---------- Opción 2: Listar registros ----------
        static void ListarRegistros()
        {
            if (totalRegistros == 0)
            {
                Console.WriteLine("No hay registros guardados todavía.");
                return;
            }

            int[] anchos = { 3, 10, 10, 6, 18, 8, 10, 12 };
            string[] titulos = { "N°", "Fecha", "Línea", "Turno", "Operario", "Unid.", "Eficien.", "Alerta" };

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=== LISTA DE REGISTROS ===\n");
            Console.ResetColor();

            ImprimirSeparador(anchos);
            ImprimirFila(anchos, titulos, ConsoleColor.Cyan);
            ImprimirSeparador(anchos);

            for (int i = 0; i < totalRegistros; i++)
            {
                double eficiencia = (unidades[i] * 100.0) / META_DEFECTO;
                string nivel;
                ConsoleColor color;
                ClasificarEficiencia(eficiencia, out nivel, out color);

                string[] fila = {
                    (i + 1).ToString(),
                    fechas[i].ToString("dd/MM/yyyy"),
                    lineasProd[i],
                    $"T{turnos[i]}",
                    operarios[i],
                    unidades[i].ToString(),
                    $"{eficiencia:F1}%",
                    nivel
                };

                ImprimirFila(anchos, fila, color);
            }

            ImprimirSeparador(anchos);
            Console.WriteLine($"Total de registros: {totalRegistros}  |  Meta de referencia: {META_DEFECTO} unidades");
            Console.WriteLine("Leyenda: Rojo = CRÍTICO (<70%) | Amarillo = PRECAUCIÓN (70-89%) | Verde = ACEPTABLE (>=90%)");
        }

        // ---------- Opción 3: Calcular eficiencia de un registro ----------
        static void CalcularEficienciaRegistro()
        {
            if (totalRegistros == 0)
            {
                Console.WriteLine("No hay registros guardados todavía.");
                return;
            }

            ListarRegistros();

            int numero;
            bool ok;
            do
            {
                Console.Write($"\nIngrese el número de registro a evaluar (1-{totalRegistros}): ");
                ok = int.TryParse(Console.ReadLine(), out numero) && numero >= 1 && numero <= totalRegistros;
                if (!ok)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Número inválido. Debe ser entre 1 y {totalRegistros}.");
                    Console.ResetColor();
                }
            } while (!ok);

            int indice = numero - 1;

            Console.Write($"Meta de producción (Enter para usar {META_DEFECTO}): ");
            string entradaMeta = Console.ReadLine();
            int meta = string.IsNullOrWhiteSpace(entradaMeta) ? META_DEFECTO : int.Parse(entradaMeta);

            double eficiencia = meta > 0 ? (unidades[indice] * 100.0) / meta : 0;

            Console.WriteLine($"\nFecha: {fechas[indice]:dd/MM/yyyy} | Línea: {lineasProd[indice]} | Turno: {turnos[indice]} | " +
                               $"{unidades[indice]} uds | Operario: {operarios[indice]}");
            Console.WriteLine($"Meta: {meta} unidades");
            Console.WriteLine($"Eficiencia: {eficiencia:F2}%");
        }

        // ---------- Opción 9: Buscar registro ----------
        static void BuscarRegistro()
        {
            if (totalRegistros == 0)
            {
                Console.WriteLine("No hay registros guardados todavía.");
                return;
            }

            int opcion;

            do
            {
                Console.Clear();
                MostrarEncabezado();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n=== BUSCAR REGISTRO ===");
                Console.ResetColor();

                Console.WriteLine("\n1. Buscar por fecha");
                Console.WriteLine("2. Buscar por línea");
                Console.WriteLine("3. Buscar por operador");
                Console.WriteLine("0. Volver al menú principal");
                Console.Write("\nSeleccione una opción: ");

                if (!int.TryParse(Console.ReadLine(), out opcion))
                {
                    opcion = -1;
                }

                switch (opcion)
                {
                    case 1:
                        BuscarPorFecha();
                        break;

                    case 2:
                        BuscarPorLinea();
                        break;

                    case 3:
                        BuscarPorOperador();
                        break;

                    case 0:
                        Console.WriteLine("Regresando al menú principal...");
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Opción no válida.");
                        Console.ResetColor();
                        Console.WriteLine("Presione cualquier tecla para continuar...");
                        Console.ReadKey();
                        break;
                }

                if (opcion >= 1 && opcion <= 3)
                {
                    Console.WriteLine("\nPresione cualquier tecla para volver al menú de búsqueda...");
                    Console.ReadKey();
                }

            } while (opcion != 0);
        }

        // Buscar registros por fecha exacta
        static void BuscarPorFecha()
        {
            Console.Clear();
            MostrarEncabezado();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=== BUSCAR POR FECHA ===");
            Console.ResetColor();

            Console.Write("\nIngrese la fecha (dd/MM/yyyy): ");
            string entrada = Console.ReadLine();

            if (!DateTime.TryParseExact(
                entrada,
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime fechaBuscada))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Formato de fecha inválido. Use dd/MM/yyyy.");
                Console.ResetColor();
                return;
            }

            MostrarResultadosBusqueda(fechaBuscada);
        }

        // Buscar registros por línea
        static void BuscarPorLinea()
        {
            Console.Clear();
            MostrarEncabezado();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=== BUSCAR POR LÍNEA ===");
            Console.ResetColor();

            Console.Write("\nIngrese el nombre de la línea: ");
            string lineaBuscada = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(lineaBuscada))
            {
                Console.WriteLine("Debe ingresar una línea.");
                return;
            }

            int encontrados = 0;

            int[] anchos = { 10, 10, 8, 18, 8, 10, 12 };
            string[] titulos = { "Fecha", "Línea", "Turno", "Operario", "Unid.", "Eficien.", "Alerta" };

            Console.WriteLine();
            ImprimirSeparador(anchos);
            ImprimirFila(anchos, titulos, ConsoleColor.Cyan);
            ImprimirSeparador(anchos);

            for (int i = 0; i < totalRegistros; i++)
            {
                if (lineasProd[i].IndexOf(lineaBuscada, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    double eficiencia = (unidades[i] * 100.0) / META_DEFECTO;

                    string nivel;
                    ConsoleColor color;
                    ClasificarEficiencia(eficiencia, out nivel, out color);

                    string[] fila =
                    {
                        fechas[i].ToString("dd/MM/yyyy"),
                        lineasProd[i],
                        $"T{turnos[i]}",
                        operarios[i],
                        unidades[i].ToString(),
                        $"{eficiencia:F1}%",
                        nivel
                    };

                    ImprimirFila(anchos, fila, color);
                    encontrados++;
                }
            }

            ImprimirSeparador(anchos);
            Console.WriteLine($"\nRegistros encontrados: {encontrados}");
        }

        // Buscar registros por operador
        static void BuscarPorOperador()
        {
            Console.Clear();
            MostrarEncabezado();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=== BUSCAR POR OPERADOR ===");
            Console.ResetColor();

            Console.Write("\nIngrese el nombre del operador: ");
            string operadorBuscado = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(operadorBuscado))
            {
                Console.WriteLine("Debe ingresar el nombre del operador.");
                return;
            }

            int encontrados = 0;

            int[] anchos = { 10, 10, 8, 18, 8, 10, 12 };
            string[] titulos = { "Fecha", "Línea", "Turno", "Operario", "Unid.", "Eficien.", "Alerta" };

            Console.WriteLine();
            ImprimirSeparador(anchos);
            ImprimirFila(anchos, titulos, ConsoleColor.Cyan);
            ImprimirSeparador(anchos);

            for (int i = 0; i < totalRegistros; i++)
            {
                if (operarios[i].IndexOf(operadorBuscado, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    double eficiencia = (unidades[i] * 100.0) / META_DEFECTO;

                    string nivel;
                    ConsoleColor color;
                    ClasificarEficiencia(eficiencia, out nivel, out color);

                    string[] fila =
                    {
                        fechas[i].ToString("dd/MM/yyyy"),
                        lineasProd[i],
                        $"T{turnos[i]}",
                        operarios[i],
                        unidades[i].ToString(),
                        $"{eficiencia:F1}%",
                        nivel
                    };

                    ImprimirFila(anchos, fila, color);
                    encontrados++;
                }
            }

            ImprimirSeparador(anchos);
            Console.WriteLine($"\nRegistros encontrados: {encontrados}");
        }

        // Mostrar resultados de una búsqueda por fecha
        static void MostrarResultadosBusqueda(DateTime fechaBuscada)
        {
            int encontrados = 0;

            int[] anchos = { 10, 10, 8, 18, 8, 10, 12 };
            string[] titulos = { "Fecha", "Línea", "Turno", "Operario", "Unid.", "Eficien.", "Alerta" };

            Console.WriteLine();
            ImprimirSeparador(anchos);
            ImprimirFila(anchos, titulos, ConsoleColor.Cyan);
            ImprimirSeparador(anchos);

            for (int i = 0; i < totalRegistros; i++)
            {
                if (fechas[i].Date == fechaBuscada.Date)
                {
                    double eficiencia = (unidades[i] * 100.0) / META_DEFECTO;

                    string nivel;
                    ConsoleColor color;
                    ClasificarEficiencia(eficiencia, out nivel, out color);

                    string[] fila =
                    {
                        fechas[i].ToString("dd/MM/yyyy"),
                        lineasProd[i],
                        $"T{turnos[i]}",
                        operarios[i],
                        unidades[i].ToString(),
                        $"{eficiencia:F1}%",
                        nivel
                    };

                    ImprimirFila(anchos, fila, color);
                    encontrados++;
                }
            }

            ImprimirSeparador(anchos);

            Console.WriteLine($"\nFecha buscada: {fechaBuscada:dd/MM/yyyy}");
            Console.WriteLine($"Registros encontrados: {encontrados}");
        }

        // ---------- Opción 4: Reporte por línea ----------
        static void ReportePorLinea()
        {
            if (totalRegistros == 0)
            {
                Console.WriteLine("No hay registros guardados todavía.");
                return;
            }

            Console.WriteLine("--- REPORTE POR LÍNEA ---");
            Console.WriteLine("1. Ver resumen general (todas las líneas)");
            Console.WriteLine("2. Filtrar por una línea específica");
            Console.Write("Seleccione una opción: ");
            string opcion = Console.ReadLine();

            if (opcion == "2")
            {
                Console.Write("\nIngrese el nombre de la línea (ej:Linea): ");
                string lineaBuscada = Console.ReadLine()?.Trim();
                ReporteLineaEspecifica(lineaBuscada);
            }
            else
            {
                ReporteLineaGeneral();
            }
        }

        static void ReporteLineaGeneral()
        {
            string[] lineasUnicas = new string[MAX_REGISTROS];
            int[] totalesPorLinea = new int[MAX_REGISTROS];
            int[] cantidadPorLinea = new int[MAX_REGISTROS];
            int cantidadLineas = 0;

            for (int i = 0; i < totalRegistros; i++)
            {
                int idx = BuscarEnArreglo(lineasUnicas, cantidadLineas, lineasProd[i]);
                if (idx == -1)
                {
                    lineasUnicas[cantidadLineas] = lineasProd[i];
                    totalesPorLinea[cantidadLineas] = unidades[i];
                    cantidadPorLinea[cantidadLineas] = 1;
                    cantidadLineas++;
                }
                else
                {
                    totalesPorLinea[idx] += unidades[i];
                    cantidadPorLinea[idx]++;
                }
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=== REPORTE POR LÍNEA (RESUMEN GENERAL) ===");
            Console.ResetColor();

            int[] anchosDetalle = { 10, 8, 18, 8, 10, 12 };
            string[] titulosDetalle = { "Fecha", "Turno", "Operario", "Unid.", "Eficien.", "Alerta" };

            for (int i = 0; i < cantidadLineas; i++)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"\n--- Línea {lineasUnicas[i]} ---");
                Console.ResetColor();

                ImprimirSeparador(anchosDetalle);
                ImprimirFila(anchosDetalle, titulosDetalle, ConsoleColor.Cyan);
                ImprimirSeparador(anchosDetalle);

                for (int j = 0; j < totalRegistros; j++)
                {
                    if (lineasProd[j].Equals(lineasUnicas[i], StringComparison.OrdinalIgnoreCase))
                    {
                        double eficReg = (unidades[j] * 100.0) / META_DEFECTO;
                        string nivelReg;
                        ConsoleColor colorReg;
                        ClasificarEficiencia(eficReg, out nivelReg, out colorReg);

                        string[] filaDetalle = {
                            fechas[j].ToString("dd/MM/yyyy"),
                            $"Turno {turnos[j]}",
                            operarios[j],
                            unidades[j].ToString(),
                            $"{eficReg:F1}%",
                            nivelReg
                        };

                        ImprimirFila(anchosDetalle, filaDetalle, colorReg);
                    }
                }

                ImprimirSeparador(anchosDetalle);
            }

            int[] anchos = { 12, 12, 10, 12 };
            string[] titulos = { "Línea", "Total Uds.", "Registros", "Rendimiento" };

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=== RESUMEN DE TOTALES POR LÍNEA ===\n");
            Console.ResetColor();

            ImprimirSeparador(anchos);
            ImprimirFila(anchos, titulos, ConsoleColor.Cyan);
            ImprimirSeparador(anchos);

            for (int i = 0; i < cantidadLineas; i++)
            {
                double metaLinea = META_DEFECTO * (double)cantidadPorLinea[i];
                double eficienciaLinea = (totalesPorLinea[i] * 100.0) / metaLinea;

                string nivel;
                ConsoleColor color;
                ClasificarEficiencia(eficienciaLinea, out nivel, out color);

                string[] fila = {
                    lineasUnicas[i],
                    totalesPorLinea[i].ToString(),
                    cantidadPorLinea[i].ToString(),
                    nivel
                };

                ImprimirFila(anchos, fila, color);
            }

            ImprimirSeparador(anchos);
        }

        static void ReporteLineaEspecifica(string linea)
        {
            bool hayDatos = false;
            int totalLinea = 0;
            int mejorTurno = 0;
            int mejorTotalTurno = -1;

            int[] anchos = { 8, 10, 18, 8, 10, 12 };
            string[] titulos = { "Turno", "Fecha", "Operario", "Unid.", "Eficien.", "Alerta" };

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n=== REPORTE DETALLADO - LÍNEA {linea.ToUpper()} ===\n");
            Console.ResetColor();

            ImprimirSeparador(anchos);
            ImprimirFila(anchos, titulos, ConsoleColor.Cyan);
            ImprimirSeparador(anchos);

            for (int t = 1; t <= 3; t++)
            {
                int totalTurno = 0;

                for (int i = 0; i < totalRegistros; i++)
                {
                    if (lineasProd[i].Equals(linea, StringComparison.OrdinalIgnoreCase) && turnos[i] == t)
                    {
                        double eficiencia = (unidades[i] * 100.0) / META_DEFECTO;
                        string nivel;
                        ConsoleColor color;
                        ClasificarEficiencia(eficiencia, out nivel, out color);

                        string[] fila = {
                            $"Turno {t}",
                            fechas[i].ToString("dd/MM/yyyy"),
                            operarios[i],
                            unidades[i].ToString(),
                            $"{eficiencia:F1}%",
                            nivel
                        };

                        ImprimirFila(anchos, fila, color);

                        totalTurno += unidades[i];
                        hayDatos = true;
                    }
                }

                if (totalTurno > 0)
                {
                    totalLinea += totalTurno;
                    if (totalTurno > mejorTotalTurno)
                    {
                        mejorTotalTurno = totalTurno;
                        mejorTurno = t;
                    }
                }
            }

            ImprimirSeparador(anchos);

            if (!hayDatos)
            {
                Console.WriteLine($"No hay registros para la línea '{linea}'.");
                return;
            }

            Console.WriteLine($"\nTotal producido en línea {linea}: {totalLinea} unidades");
            Console.WriteLine($"Turno con mayor producción en esta línea: Turno {mejorTurno} ({mejorTotalTurno} unidades)");
        }

        static int BuscarEnArreglo(string[] arreglo, int cantidad, string valor)
        {
            for (int i = 0; i < cantidad; i++)
            {
                if (arreglo[i].Equals(valor, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        // ---------- Opción 5: Reporte por turno ----------
        static void ReportePorTurno()
        {
            if (totalRegistros == 0)
            {
                Console.WriteLine("No hay registros guardados todavía.");
                return;
            }

            int[] totalesPorTurno = new int[4];
            int[] cantidadPorTurno = new int[4];

            for (int i = 0; i < totalRegistros; i++)
            {
                totalesPorTurno[turnos[i]] += unidades[i];
                cantidadPorTurno[turnos[i]]++;
            }

            string[] nombresTurno = { "", "Turno 1 - Mañana", "Turno 2 - Tarde", "Turno 3 - Noche" };

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=== REPORTE POR TURNO ===");
            Console.ResetColor();

            int[] anchosDetalle = { 10, 10, 18, 8, 10, 12 };
            string[] titulosDetalle = { "Fecha", "Línea", "Operario", "Unid.", "Eficien.", "Alerta" };

            for (int t = 1; t <= 3; t++)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"\n--- {nombresTurno[t]} ---");
                Console.ResetColor();

                ImprimirSeparador(anchosDetalle);
                ImprimirFila(anchosDetalle, titulosDetalle, ConsoleColor.Cyan);
                ImprimirSeparador(anchosDetalle);

                bool turnoTieneDatos = false;

                for (int i = 0; i < totalRegistros; i++)
                {
                    if (turnos[i] == t)
                    {
                        double eficReg = (unidades[i] * 100.0) / META_DEFECTO;
                        string nivelReg;
                        ConsoleColor colorReg;
                        ClasificarEficiencia(eficReg, out nivelReg, out colorReg);

                        string[] filaDetalle = {
                            fechas[i].ToString("dd/MM/yyyy"),
                            lineasProd[i],
                            operarios[i],
                            unidades[i].ToString(),
                            $"{eficReg:F1}%",
                            nivelReg
                        };

                        ImprimirFila(anchosDetalle, filaDetalle, colorReg);
                        turnoTieneDatos = true;
                    }
                }

                if (!turnoTieneDatos)
                {
                    Console.WriteLine("   (Sin registros en este turno)");
                }

                ImprimirSeparador(anchosDetalle);
            }

            int[] anchos = { 18, 12, 10, 12 };
            string[] titulos = { "Turno", "Total Uds.", "Registros", "Rendimiento" };

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=== RESUMEN DE TOTALES POR TURNO ===\n");
            Console.ResetColor();

            ImprimirSeparador(anchos);
            ImprimirFila(anchos, titulos, ConsoleColor.Cyan);
            ImprimirSeparador(anchos);

            for (int t = 1; t <= 3; t++)
            {
                double metaTurno = META_DEFECTO * (double)Math.Max(cantidadPorTurno[t], 1);
                double eficienciaTurno = (totalesPorTurno[t] * 100.0) / metaTurno;

                string nivel;
                ConsoleColor color;
                ClasificarEficiencia(cantidadPorTurno[t] == 0 ? 0 : eficienciaTurno, out nivel, out color);

                string[] fila = {
                    nombresTurno[t],
                    totalesPorTurno[t].ToString(),
                    cantidadPorTurno[t].ToString(),
                    nivel
                };

                ImprimirFila(anchos, fila, color);
            }

            ImprimirSeparador(anchos);
        }

        // ---------- Opción 6: Mejor y peor día del mes ----------
        static void MejorYPeorDia()
        {
            if (totalRegistros == 0)
            {
                Console.WriteLine("No hay registros guardados todavía.");
                return;
            }

            DateTime[] diasUnicos = new DateTime[MAX_REGISTROS];
            int[] totalesPorDia = new int[MAX_REGISTROS];
            int cantidadDias = 0;

            for (int i = 0; i < totalRegistros; i++)
            {
                DateTime dia = fechas[i].Date;
                int idx = -1;

                for (int j = 0; j < cantidadDias; j++)
                {
                    if (diasUnicos[j] == dia)
                    {
                        idx = j;
                        break;
                    }
                }

                if (idx == -1)
                {
                    diasUnicos[cantidadDias] = dia;
                    totalesPorDia[cantidadDias] = unidades[i];
                    cantidadDias++;
                }
                else
                {
                    totalesPorDia[idx] += unidades[i];
                }
            }

            int idxMejor = 0, idxPeor = 0;
            for (int i = 1; i < cantidadDias; i++)
            {
                if (totalesPorDia[i] > totalesPorDia[idxMejor]) idxMejor = i;
                if (totalesPorDia[i] < totalesPorDia[idxPeor]) idxPeor = i;
            }

            Console.WriteLine("\n=== MEJOR Y PEOR DÍA DEL MES ===");
            Console.WriteLine($"Mejor día: {diasUnicos[idxMejor]:dd/MM/yyyy} con {totalesPorDia[idxMejor]} unidades");
            Console.WriteLine($"Peor día:  {diasUnicos[idxPeor]:dd/MM/yyyy} con {totalesPorDia[idxPeor]} unidades");
        }

        // ---------- Opción 7: Comparativa mensual con años anteriores ----------
        static void ComparativaMensualAnios()
        {
            string[] archivos = Directory.GetFiles(".", "produccion_*.txt");

            if (archivos.Length == 0)
            {
                Console.WriteLine("No se encontraron archivos de producción para comparar.");
                return;
            }

            int MAX_ANIOS = 20;
            int[] aniosEncontrados = new int[MAX_ANIOS];
            int[,] totalesPorAnioMes = new int[MAX_ANIOS, 13];
            int cantidadAnios = 0;

            foreach (string archivo in archivos)
            {
                string nombreSolo = Path.GetFileNameWithoutExtension(archivo);
                string[] partesNombre = nombreSolo.Split('_');
                if (partesNombre.Length != 2) continue;
                if (!int.TryParse(partesNombre[1], out int anio)) continue;

                int idxAnio = -1;
                for (int i = 0; i < cantidadAnios; i++)
                {
                    if (aniosEncontrados[i] == anio) { idxAnio = i; break; }
                }
                if (idxAnio == -1 && cantidadAnios < MAX_ANIOS)
                {
                    aniosEncontrados[cantidadAnios] = anio;
                    idxAnio = cantidadAnios;
                    cantidadAnios++;
                }
                if (idxAnio == -1) continue;

                string[] lineasArchivo = File.ReadAllLines(archivo);
                foreach (string lineaTexto in lineasArchivo)
                {
                    if (string.IsNullOrWhiteSpace(lineaTexto)) continue;
                    string[] partes = lineaTexto.Split('|');
                    if (partes.Length < 5) continue;

                    if (DateTime.TryParseExact(partes[0].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaLeida)
                        && int.TryParse(partes[3], out int unidLeidas))
                    {
                        totalesPorAnioMes[idxAnio, fechaLeida.Month] += unidLeidas;
                    }
                }
            }

            for (int i = 0; i < cantidadAnios - 1; i++)
            {
                for (int j = 0; j < cantidadAnios - 1 - i; j++)
                {
                    if (aniosEncontrados[j] > aniosEncontrados[j + 1])
                    {
                        int tmp = aniosEncontrados[j];
                        aniosEncontrados[j] = aniosEncontrados[j + 1];
                        aniosEncontrados[j + 1] = tmp;

                        for (int m = 1; m <= 12; m++)
                        {
                            int tmpVal = totalesPorAnioMes[j, m];
                            totalesPorAnioMes[j, m] = totalesPorAnioMes[j + 1, m];
                            totalesPorAnioMes[j + 1, m] = tmpVal;
                        }
                    }
                }
            }

            string[] nombresMes = { "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                                     "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };

            int[] anchos = new int[cantidadAnios + 1];
            anchos[0] = 12;
            for (int i = 1; i <= cantidadAnios; i++) anchos[i] = 10;

            string[] titulos = new string[cantidadAnios + 1];
            titulos[0] = "Mes";
            for (int i = 0; i < cantidadAnios; i++) titulos[i + 1] = aniosEncontrados[i].ToString();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=== COMPARATIVA MENSUAL ENTRE GESTIONES (AÑOS) ===\n");
            Console.ResetColor();

            ImprimirSeparador(anchos);
            ImprimirFila(anchos, titulos, ConsoleColor.Cyan);
            ImprimirSeparador(anchos);

            for (int m = 1; m <= 12; m++)
            {
                string[] fila = new string[cantidadAnios + 1];
                fila[0] = nombresMes[m];
                for (int a = 0; a < cantidadAnios; a++)
                {
                    fila[a + 1] = totalesPorAnioMes[a, m].ToString();
                }
                ImprimirFila(anchos, fila, ConsoleColor.White);
            }

            ImprimirSeparador(anchos);
            Console.WriteLine($"Gestiones comparadas: {string.Join(", ", aniosEncontrados, 0, cantidadAnios)}");
        }

        // ---------- Opción 8: Reporte de costos y viabilidad de producción ----------
        static void ReporteCostosViabilidad()
        {
            if (totalRegistros == 0)
            {
                Console.WriteLine("No hay registros guardados todavía.");
                return;
            }

            // Detalle registro por registro
            int[] anchosDetalle = { 10, 10, 8, 10, 10, 10, 12 };
            string[] titulosDetalle = { "Fecha", "Línea", "Unid.", "Costo/u", "Precio/u", "Margen%", "Viabilidad" };

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=== REPORTE DE COSTOS Y VIABILIDAD (DETALLE) ===\n");
            Console.ResetColor();

            ImprimirSeparador(anchosDetalle);
            ImprimirFila(anchosDetalle, titulosDetalle, ConsoleColor.Cyan);
            ImprimirSeparador(anchosDetalle);

            bool hayDatosCosto = false;

            for (int i = 0; i < totalRegistros; i++)
            {
                if (preciosVenta[i] <= 0)
                {
                    continue; // registros antiguos sin datos de costo cargados
                }

                hayDatosCosto = true;
                double margenUnit = preciosVenta[i] - costosMateriaPrima[i];
                double margenPorc = (margenUnit / preciosVenta[i]) * 100.0;

                string nivel;
                ConsoleColor color;
                ClasificarViabilidad(margenPorc, out nivel, out color);

                string[] fila = {
                    fechas[i].ToString("dd/MM/yyyy"),
                    lineasProd[i],
                    unidades[i].ToString(),
                    $"${costosMateriaPrima[i]:F2}",
                    $"${preciosVenta[i]:F2}",
                    $"{margenPorc:F1}%",
                    nivel
                };

                ImprimirFila(anchosDetalle, fila, color);
            }

            ImprimirSeparador(anchosDetalle);

            if (!hayDatosCosto)
            {
                Console.WriteLine("Ningún registro tiene datos de costo/precio cargados todavía.");
                Console.WriteLine("(Los registros antiguos, guardados antes de esta opción, no tienen esta información.)");
                return;
            }

            // Resumen de rentabilidad por línea
            string[] lineasUnicas = new string[MAX_REGISTROS];
            double[] costoTotalPorLinea = new double[MAX_REGISTROS];
            double[] ingresoTotalPorLinea = new double[MAX_REGISTROS];
            int cantidadLineas = 0;

            for (int i = 0; i < totalRegistros; i++)
            {
                if (preciosVenta[i] <= 0) continue;

                int idx = BuscarEnArreglo(lineasUnicas, cantidadLineas, lineasProd[i]);
                if (idx == -1)
                {
                    lineasUnicas[cantidadLineas] = lineasProd[i];
                    idx = cantidadLineas;
                    cantidadLineas++;
                }

                costoTotalPorLinea[idx] += costosMateriaPrima[i] * unidades[i];
                ingresoTotalPorLinea[idx] += preciosVenta[i] * unidades[i];
            }

            int[] anchosResumen = { 12, 12, 12, 12, 10, 12 };
            string[] titulosResumen = { "Línea", "Costo Tot.", "Ingreso Tot.", "Utilidad", "Margen%", "Viabilidad" };

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=== RENTABILIDAD POR LÍNEA ===\n");
            Console.ResetColor();

            ImprimirSeparador(anchosResumen);
            ImprimirFila(anchosResumen, titulosResumen, ConsoleColor.Cyan);
            ImprimirSeparador(anchosResumen);

            for (int i = 0; i < cantidadLineas; i++)
            {
                double utilidad = ingresoTotalPorLinea[i] - costoTotalPorLinea[i];
                double margenPorc = ingresoTotalPorLinea[i] > 0 ? (utilidad / ingresoTotalPorLinea[i]) * 100.0 : 0;

                string nivel;
                ConsoleColor color;
                ClasificarViabilidad(margenPorc, out nivel, out color);

                string[] fila = {
                    lineasUnicas[i],
                    $"${costoTotalPorLinea[i]:F2}",
                    $"${ingresoTotalPorLinea[i]:F2}",
                    $"${utilidad:F2}",
                    $"{margenPorc:F1}%",
                    nivel
                };

                ImprimirFila(anchosResumen, fila, color);
            }

            ImprimirSeparador(anchosResumen);
            Console.WriteLine($"Leyenda: Rojo = NO VIABLE (<{MARGEN_MINIMO}%) | Amarillo = PRECAUCIÓN ({MARGEN_MINIMO}-{MARGEN_BUENO}%) | Verde = VIABLE (>={MARGEN_BUENO}%)");
        }

        // ========================================
        // BLOQUE 5: PERSISTENCIA (ARCHIVOS)
        // ========================================

        static void CargarDatosDesdeArchivo()
        {
            if (!File.Exists(NombreArchivoActual))
            {
                return;
            }

            try
            {
                string[] lineasArchivo = File.ReadAllLines(NombreArchivoActual);

                foreach (string linea in lineasArchivo)
                {
                    if (string.IsNullOrWhiteSpace(linea)) continue;

                    string[] partes = linea.Split('|');

                    // Formato nuevo (7 campos, con costo y precio) o formato antiguo (5 campos, sin costo)
                    if ((partes.Length == 7 || partes.Length == 5) && totalRegistros < MAX_REGISTROS)
                    {
                        if (DateTime.TryParseExact(partes[0].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaCargada))
                        {
                            fechas[totalRegistros] = fechaCargada;
                        }
                        else
                        {
                            fechas[totalRegistros] = DateTime.Parse(partes[0].Trim());
                        }

                        lineasProd[totalRegistros] = partes[1];
                        turnos[totalRegistros] = int.Parse(partes[2]);
                        unidades[totalRegistros] = int.Parse(partes[3]);
                        operarios[totalRegistros] = partes[4];

                        if (partes.Length == 7)
                        {
                            costosMateriaPrima[totalRegistros] = double.Parse(partes[5], CultureInfo.InvariantCulture);
                            preciosVenta[totalRegistros] = double.Parse(partes[6], CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            // Registro antiguo sin datos de costo: se deja en 0
                            costosMateriaPrima[totalRegistros] = 0;
                            preciosVenta[totalRegistros] = 0;
                        }

                        totalRegistros++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer archivo: {ex.Message}");
            }
        }

        static void GuardarDatosEnArchivo()
        {
            try
            {
                using (StreamWriter escritor = new StreamWriter(NombreArchivoActual, false))
                {
                    for (int i = 0; i < totalRegistros; i++)
                    {
                        string linea = $"{fechas[i]:dd/MM/yyyy}|{lineasProd[i]}|{turnos[i]}|{unidades[i]}|{operarios[i]}|" +
                                       $"{costosMateriaPrima[i].ToString(CultureInfo.InvariantCulture)}|" +
                                       $"{preciosVenta[i].ToString(CultureInfo.InvariantCulture)}";
                        escritor.WriteLine(linea);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar archivo: {ex.Message}");
            }
        }
    }
}
