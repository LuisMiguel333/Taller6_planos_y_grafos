using System.Text;

namespace Backend;

public static class CityFile
{
    public static Graph<string> Load(string path, out int streetCount, out int oneWayCount)
    {
        streetCount = 0;
        oneWayCount = 0;

        if (!File.Exists(path))
        {
            throw new InvalidDataException("No se encontró el archivo del mapa: " + path);
        }

        string fileName = Path.GetFileName(path);
        Graph<string> graph = new Graph<string>(directed: true);
        List<string> errors = new List<string>();

        Dictionary<string, int> seenStreets = new Dictionary<string, int>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int lineNumber = i + 1;
            string line = lines[i].Trim();

            if (line == "" || line.StartsWith("#"))
            {
                continue;
            }

            string[] parts = line.Split(',');
            if (parts.Length != 4)
            {
                errors.Add(fileName + " línea " + lineNumber + ": se esperaban 4 campos (from,to,distancia,sentido) pero hay " + parts.Length + ".");
                continue;
            }

            string from = parts[0].Trim();
            string to = parts[1].Trim();
            string distanceText = parts[2].Trim();
            string direction = parts[3].Trim();

            if (from == "" || to == "")
            {
                errors.Add(fileName + " línea " + lineNumber + ": falta el nombre de una ubicación.");
                continue;
            }

            if (!int.TryParse(distanceText, out int distance) || distance <= 0)
            {
                errors.Add(fileName + " línea " + lineNumber + ": la distancia '" + distanceText + "' no es un número entero mayor que 0.");
                continue;
            }

            bool isTwoWay = direction.Equals("TwoWay", StringComparison.OrdinalIgnoreCase);
            bool isOneWay = direction.Equals("OneWay", StringComparison.OrdinalIgnoreCase);
            if (!isTwoWay && !isOneWay)
            {
                errors.Add(fileName + " línea " + lineNumber + ": el sentido '" + direction + "' no es TwoWay ni OneWay.");
                continue;
            }

            if (from == to)
            {
                errors.Add(fileName + " línea " + lineNumber + ": la calle va de '" + from + "' a sí misma.");
                continue;
            }

            // Same key for A,B and B,A
            string key = string.CompareOrdinal(from, to) < 0 ? from + "|" + to : to + "|" + from;
            if (seenStreets.TryGetValue(key, out int firstLine))
            {
                errors.Add(fileName + " línea " + lineNumber + ": la calle " + from + "-" + to + " ya apareció en la línea " + firstLine + ".");
                continue;
            }
            seenStreets[key] = lineNumber;

            graph.AddEdge(from, to, distance);
            if (isTwoWay)
            {
                graph.AddEdge(to, from, distance);
            }
            else
            {
                oneWayCount++;
            }
            streetCount++;
        }

        if (errors.Count == 0 && streetCount == 0)
        {
            errors.Add(fileName + ": el mapa no tiene ninguna calle.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidDataException(string.Join(Environment.NewLine, errors));
        }

        return graph;
    }
}
