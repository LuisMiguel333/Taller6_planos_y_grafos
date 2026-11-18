namespace Backend;

public static class PackageFile
{
    private class PackageDraft
    {
        public string? Pickup;
        public int PickupLine;
        public string? Delivery;
        public int DeliveryLine;
    }

    public static List<Package> Load(string path, Graph<string> city)
    {
        if (!File.Exists(path))
        {
            throw new InvalidDataException("No se encontró el archivo de paquetes: " + path);
        }

        string fileName = Path.GetFileName(path);
        List<string> errors = new List<string>();

        // SortedDictionary keeps the packages ordered by id
        SortedDictionary<int, PackageDraft> drafts = new SortedDictionary<int, PackageDraft>();

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
            if (parts.Length != 3)
            {
                errors.Add(fileName + " línea " + lineNumber + ": se esperaban 3 campos (ubicación,acción,paquete) pero hay " + parts.Length + ".");
                continue;
            }

            string location = parts[0].Trim();
            string action = parts[1].Trim();
            string idText = parts[2].Trim();

            if (!int.TryParse(idText, out int id) || id <= 0)
            {
                errors.Add(fileName + " línea " + lineNumber + ": el número de paquete '" + idText + "' no es un entero mayor que 0.");
                continue;
            }

            bool isPickup = action.Equals("Pickup", StringComparison.OrdinalIgnoreCase);
            bool isDelivery = action.Equals("Delivery", StringComparison.OrdinalIgnoreCase);
            if (!isPickup && !isDelivery)
            {
                errors.Add(fileName + " línea " + lineNumber + ": la acción '" + action + "' no es Pickup ni Delivery.");
                continue;
            }

            if (!city.Contains(location))
            {
                errors.Add(fileName + " línea " + lineNumber + ": la ubicación '" + location + "' no existe en el mapa.");
                continue;
            }

            if (!drafts.ContainsKey(id))
            {
                drafts[id] = new PackageDraft();
            }
            PackageDraft draft = drafts[id];

            if (isPickup)
            {
                if (draft.Pickup != null)
                {
                    errors.Add(fileName + " línea " + lineNumber + ": el paquete " + id + " ya tiene una recogida (línea " + draft.PickupLine + ").");
                    continue;
                }
                draft.Pickup = location;
                draft.PickupLine = lineNumber;
            }
            else
            {
                if (draft.Delivery != null)
                {
                    errors.Add(fileName + " línea " + lineNumber + ": el paquete " + id + " ya tiene una entrega (línea " + draft.DeliveryLine + ").");
                    continue;
                }
                draft.Delivery = location;
                draft.DeliveryLine = lineNumber;
            }
        }

        // Every package needs exactly one pickup and one delivery
        List<Package> packages = new List<Package>();
        foreach (KeyValuePair<int, PackageDraft> pair in drafts)
        {
            if (pair.Value.Pickup == null)
            {
                errors.Add(fileName + ": al paquete " + pair.Key + " le falta la recogida (Pickup).");
            }
            else if (pair.Value.Delivery == null)
            {
                errors.Add(fileName + ": al paquete " + pair.Key + " le falta la entrega (Delivery).");
            }
            else
            {
                packages.Add(new Package(pair.Key, pair.Value.Pickup, pair.Value.Delivery));
            }
        }

        if (errors.Count == 0 && packages.Count == 0)
        {
            errors.Add(fileName + ": no hay ningún paquete.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidDataException(string.Join(Environment.NewLine, errors));
        }

        return packages;
    }
}
