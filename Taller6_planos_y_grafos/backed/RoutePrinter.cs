using System.Globalization;

namespace Backend;

public static class RoutePrinter
{
    public static List<string> Build(RoutePlanner planner, List<Stop> route, string start,
        int packageCount, int locationCount, int streetCount, int oneWayCount)
    {
        List<string> lines = new List<string>();

        lines.Add("Posición inicial: " + start);
        lines.Add("Paquetes: " + packageCount + " Ubicaciones: " + locationCount
            + " Calles: " + streetCount + " (" + oneWayCount + " de un solo sentido)");
        lines.Add("");

        int stepNumber = 1;
        int totalMeters = 0;
        string currentLocation = start;
        int i = 0;

        while (i < route.Count)
        {
            string location = route[i].Location;
            List<int> deliveries = new List<int>();
            List<int> pickups = new List<int>();

            while (i < route.Count && route[i].Location == location)
            {
                if (route[i].Kind == StopKind.Delivery)
                {
                    deliveries.Add(route[i].PackageId);
                }
                else
                {
                    pickups.Add(route[i].PackageId);
                }
                i++;
            }
            deliveries.Sort();
            pickups.Sort();

            string tasks = DescribeTasks(deliveries, pickups);

            if (location == currentLocation)
            {
                lines.Add(stepNumber + ". En " + location + ": " + tasks + ".");
            }
            else
            {
                int meters = planner.Distance(currentLocation, location);
                totalMeters += meters;

                lines.Add(stepNumber + ". Ir de " + currentLocation + " a " + location + " (" + meters + " m): " + tasks + ".");
                lines.Add("   Camino: " + string.Join(" -> ", planner.PathBetween(currentLocation, location)));
            }

            currentLocation = location;
            stepNumber++;
        }

        lines.Add("Todos los paquetes fueron recogidos y entregados.");

        double kilometers = totalMeters / 1000.0;
        lines.Add("Recorrido total: " + kilometers.ToString("F2", CultureInfo.InvariantCulture) + " km");

        return lines;
    }

    private static string DescribeTasks(List<int> deliveries, List<int> pickups)
    {
        List<string> parts = new List<string>();

        if (deliveries.Count > 0)
        {
            parts.Add("entregar " + Describe(deliveries));
        }
        if (pickups.Count > 0)
        {
            parts.Add("recoger " + Describe(pickups));
        }

        return string.Join(" y ", parts);
    }

    private static string Describe(List<int> ids)
    {
        if (ids.Count == 1)
        {
            return "paquete " + ids[0];
        }

        List<string> texts = new List<string>();
        foreach (int id in ids)
        {
            texts.Add(id.ToString());
        }

        string last = texts[texts.Count - 1];
        texts.RemoveAt(texts.Count - 1);
        return "paquetes " + string.Join(", ", texts) + " y " + last;
    }
}
