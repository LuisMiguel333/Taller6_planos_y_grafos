using System.Text;
using Backend;

internal static class Program
{
    
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        string cityPath;
        string packagesPath;
        string start;

        if (args.Length == 3)
        {
            cityPath = args[0];
            packagesPath = args[1];
            start = args[2].Trim();
        }
        else if (args.Length == 0)
        {
           
            Console.Write("Archivo del mapa (por ejemplo city.txt): ");
            cityPath = (Console.ReadLine() ?? "").Trim();
            Console.Write("Archivo de paquetes (por ejemplo packages.txt): ");
            packagesPath = (Console.ReadLine() ?? "").Trim();
            Console.Write("Posición inicial (por ejemplo D): ");
            start = (Console.ReadLine() ?? "").Trim();
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("Uso: dotnet run -- <city.txt> <packages.txt> <posicionInicial>");
            return 1;
        }

        try
        {
            
            Graph<string> city = CityFile.Load(cityPath, out int streetCount, out int oneWayCount);
            List<Package> packages = PackageFile.Load(packagesPath, city);

            
            RoutePlanner planner = new RoutePlanner(city, packages, start);

            
            List<Stop> route = planner.Plan();

            
            if (!planner.IsValid(route))
            {
                Console.WriteLine("Error interno: la ruta calculada no es válida.");
                return 2;
            }

            
            List<string> lines = RoutePrinter.Build(planner, route, start, packages.Count,
                city.VertexCount, streetCount, oneWayCount);
            foreach (string line in lines)
            {
                Console.WriteLine(line);
            }

            return 0;
        }
        catch (InvalidDataException ex)
        {
            Console.WriteLine("No se pudo calcular la ruta. Errores encontrados:");
            Console.WriteLine(ex.Message);
            return 1;
        }
    }
}