namespace Backend;

public class RoutePlanner
{
    // How many random restarts the heuristic does (fixed seed, so the result is always the same)
    private const int Restarts = 1000;
    private const int Seed = 12345;

    // Used when a point cannot be reached (kept small so sums never overflow)
    private const int Unreachable = 1_000_000_000;

    private readonly Graph<string> city;
    private readonly IReadOnlyList<Package> packages;
    private readonly string start;
    private readonly int packageCount;

    // The points that matter: index -> location name, and the reverse
    private readonly List<string> points = new List<string>();
    private readonly Dictionary<string, int> pointIndex = new Dictionary<string, int>();

    private readonly int[,] distance;

    private readonly Dictionary<string, string>[] previous;

    private readonly int[] stopLocation;
    private readonly int startIndex;

    public RoutePlanner(Graph<string> city, IReadOnlyList<Package> packages, string start)
    {
        this.city = city;
        this.packages = packages;
        this.start = start;
        packageCount = packages.Count;

        if (!city.Contains(start))
        {
            throw new InvalidDataException("La posición inicial '" + start + "' no existe en el mapa.");
        }

        AddPoint(start);
        foreach (Package package in packages)
        {
            AddPoint(package.Pickup);
            AddPoint(package.Delivery);
        }
        startIndex = pointIndex[start];

        distance = new int[points.Count, points.Count];
        previous = new Dictionary<string, string>[points.Count];

        for (int from = 0; from < points.Count; from++)
        {
            Dictionary<string, int> fromDistances = ShortestPaths.Dijkstra(city, points[from], out Dictionary<string, string> prev);
            previous[from] = prev;

            for (int to = 0; to < points.Count; to++)
            {
                if (fromDistances.TryGetValue(points[to], out int meters))
                {
                    distance[from, to] = meters;
                }
                else
                {
                    distance[from, to] = Unreachable;
                }
            }
        }

        CheckReachability();

        stopLocation = new int[2 * packageCount];
        for (int p = 0; p < packageCount; p++)
        {
            stopLocation[p] = pointIndex[packages[p].Pickup];
            stopLocation[p + packageCount] = pointIndex[packages[p].Delivery];
        }
    }

    private void AddPoint(string location)
    {
        if (!pointIndex.ContainsKey(location))
        {
            pointIndex[location] = points.Count;
            points.Add(location);
        }
    }

    private void CheckReachability()
    {
        List<string> errors = new List<string>();

        for (int from = 0; from < points.Count; from++)
        {
            List<string> unreachable = new List<string>();

            for (int to = 0; to < points.Count; to++)
            {
                if (!IsTaskPoint(to))
                {
                    continue;
                }

                if (distance[from, to] >= Unreachable)
                {
                    unreachable.Add(points[to]);
                }
            }

            if (unreachable.Count > 0)
            {
                errors.Add("Desde '" + points[from] + "' no se puede llegar a: " + string.Join(", ", unreachable) + ".");
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidDataException(string.Join(Environment.NewLine, errors));
        }
    }

    private bool IsTaskPoint(int index)
    {
        string name = points[index];
        foreach (Package package in packages)
        {
            if (package.Pickup == name || package.Delivery == name)
            {
                return true;
            }
        }
        return false;
    }

    // =========================================================
    // Public API
    // =========================================================

    // Best route found by the heuristic
    public List<Stop> Plan()
    {
        Random random = new Random(Seed);

        List<int>? bestRoute = null;
        int bestCost = int.MaxValue;

        for (int attempt = 0; attempt < Restarts; attempt++)
        {
            // Packages in a random order (the first attempt uses the file order)
            List<int> order = new List<int>();
            for (int p = 0; p < packageCount; p++)
            {
                order.Add(p);
            }
            if (attempt > 0)
            {
                Shuffle(order, random);
            }

            List<int> route = BuildByInsertion(order);
            ImproveRoute(route);

            int cost = RouteCost(route);
            if (cost < bestCost)
            {
                bestCost = cost;
                bestRoute = route;
            }
        }

        return ToStops(bestRoute!);
    }

    // Simple "nearest neighbor" route (no improvements). Useful to compare with Plan().
    public List<Stop> PlanNearestNeighbor()
    {
        List<int> route = new List<int>();
        bool[] picked = new bool[packageCount];
        bool[] delivered = new bool[packageCount];
        int current = startIndex;

        for (int step = 0; step < 2 * packageCount; step++)
        {
            int bestStop = -1;
            int bestDistance = int.MaxValue;

            for (int stop = 0; stop < 2 * packageCount; stop++)
            {
                bool valid;
                if (stop < packageCount)
                {
                    valid = !picked[stop];
                }
                else
                {
                    int p = stop - packageCount;
                    valid = picked[p] && !delivered[p];
                }

                if (valid && distance[current, stopLocation[stop]] < bestDistance)
                {
                    bestDistance = distance[current, stopLocation[stop]];
                    bestStop = stop;
                }
            }

            route.Add(bestStop);
            current = stopLocation[bestStop];
            if (bestStop < packageCount)
            {
                picked[bestStop] = true;
            }
            else
            {
                delivered[bestStop - packageCount] = true;
            }
        }

        return ToStops(route);
    }

    // Total meters of a route, starting at the start position
    public int TotalDistance(List<Stop> route)
    {
        int total = 0;
        int current = startIndex;

        foreach (Stop stop in route)
        {
            int next = pointIndex[stop.Location];
            total += distance[current, next];
            current = next;
        }

        return total;
    }

    // Checks the rules: every package is picked up once, and delivered once AFTER its pickup
    public bool IsValid(List<Stop> route)
    {
        if (route.Count != 2 * packageCount)
        {
            return false;
        }

        Dictionary<int, Package> byId = new Dictionary<int, Package>();
        foreach (Package package in packages)
        {
            byId[package.Id] = package;
        }

        HashSet<int> picked = new HashSet<int>();
        HashSet<int> delivered = new HashSet<int>();

        foreach (Stop stop in route)
        {
            if (!byId.TryGetValue(stop.PackageId, out Package package))
            {
                return false;
            }

            if (stop.Kind == StopKind.Pickup)
            {
                if (stop.Location != package.Pickup || picked.Contains(stop.PackageId))
                {
                    return false;
                }
                picked.Add(stop.PackageId);
            }
            else
            {
                if (stop.Location != package.Delivery || !picked.Contains(stop.PackageId) || delivered.Contains(stop.PackageId))
                {
                    return false;
                }
                delivered.Add(stop.PackageId);
            }
        }

        return picked.Count == packageCount && delivered.Count == packageCount;
    }

    // Streets to follow from one point to another (both must be points that matter)
    public List<string> PathBetween(string from, string to)
    {
        return ShortestPaths.BuildPath(previous[pointIndex[from]], from, to);
    }

    // Minimum meters between two points that matter
    public int Distance(string from, string to)
    {
        return distance[pointIndex[from], pointIndex[to]];
    }

    // =========================================================
    // Heuristic: build + improve
    // =========================================================

    // Adds the packages one by one, each one in the cheapest place
    private List<int> BuildByInsertion(List<int> order)
    {
        List<int> route = new List<int>();

        foreach (int p in order)
        {
            FindBestInsertion(route, p, out int pickupPosition, out int deliveryPosition);
            route.Insert(deliveryPosition, p + packageCount);
            route.Insert(pickupPosition, p);
        }

        return route;
    }

    // Repeats the improvements until none of them finds anything better
    private void ImproveRoute(List<int> route)
    {
        bool improved = true;
        while (improved)
        {
            improved = false;

            if (RelocatePackages(route))
            {
                improved = true;
            }
            if (MoveSingleStops(route))
            {
                improved = true;
            }
        }
    }

    // Takes a package out (pickup + delivery) and puts it back in its best place
    private bool RelocatePackages(List<int> route)
    {
        bool improved = false;

        for (int p = 0; p < packageCount; p++)
        {
            int oldCost = RouteCost(route);

            List<int> candidate = new List<int>(route);
            candidate.Remove(p);
            candidate.Remove(p + packageCount);

            int extra = FindBestInsertion(candidate, p, out int pickupPosition, out int deliveryPosition);
            int newCost = RouteCost(candidate) + extra;

            if (newCost < oldCost)
            {
                candidate.Insert(deliveryPosition, p + packageCount);
                candidate.Insert(pickupPosition, p);

                route.Clear();
                route.AddRange(candidate);
                improved = true;
            }
        }

        return improved;
    }

    // Takes a single stop out and tries it in another place (keeping pickup before delivery)
    private bool MoveSingleStops(List<int> route)
    {
        bool improved = false;

        for (int k = 0; k < route.Count; k++)
        {
            int stop = route[k];
            int stopPoint = stopLocation[stop];

            // How many meters we save by removing this stop from where it is
            int savings = InsertionCost(LocationBefore(route, k), stopPoint, LocationAfter(route, k + 1));

            List<int> without = new List<int>(route);
            without.RemoveAt(k);

            // Pickup must stay before its delivery, and delivery after its pickup
            int firstGap = 0;
            int lastGap = without.Count;
            if (stop < packageCount)
            {
                lastGap = without.IndexOf(stop + packageCount);
            }
            else
            {
                firstGap = without.IndexOf(stop - packageCount) + 1;
            }

            int bestGap = -1;
            int bestCost = savings;
            for (int gap = firstGap; gap <= lastGap; gap++)
            {
                int cost = InsertionCost(LocationBefore(without, gap), stopPoint, LocationAfter(without, gap));
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestGap = gap;
                }
            }

            if (bestGap != -1)
            {
                without.Insert(bestGap, stop);
                route.Clear();
                route.AddRange(without);
                improved = true;
            }
        }

        return improved;
    }

    // Tries every pair of places (pickup place, delivery place) for a package and returns
    // how many extra meters the best one adds. A "gap" i means "before route[i]".
    private int FindBestInsertion(List<int> route, int p, out int bestPickupGap, out int bestDeliveryGap)
    {
        int pickupPoint = stopLocation[p];
        int deliveryPoint = stopLocation[p + packageCount];

        int bestExtra = int.MaxValue;
        bestPickupGap = 0;
        bestDeliveryGap = 0;

        for (int i = 0; i <= route.Count; i++)
        {
            int beforeI = LocationBefore(route, i);
            int afterI = LocationAfter(route, i);
            int pickupExtra = InsertionCost(beforeI, pickupPoint, afterI);

            for (int j = i; j <= route.Count; j++)
            {
                int extra;

                if (j == i)
                {
                    // Pickup and delivery together in the same gap: before -> pickup -> delivery -> after
                    extra = distance[beforeI, pickupPoint] + distance[pickupPoint, deliveryPoint];
                    if (afterI >= 0)
                    {
                        extra += distance[deliveryPoint, afterI] - distance[beforeI, afterI];
                    }
                }
                else
                {
                    // Different gaps: each insertion is independent
                    extra = pickupExtra + InsertionCost(LocationBefore(route, j), deliveryPoint, LocationAfter(route, j));
                }

                if (extra < bestExtra)
                {
                    bestExtra = extra;
                    bestPickupGap = i;
                    bestDeliveryGap = j;
                }
            }
        }

        return bestExtra;
    }

    // Extra meters of putting "location" between "before" and "after" (after = -1 means end of route)
    private int InsertionCost(int before, int location, int after)
    {
        int cost = distance[before, location];
        if (after >= 0)
        {
            cost += distance[location, after] - distance[before, after];
        }
        return cost;
    }

    // Point where the route is just before gap i (the start position if gap is 0)
    private int LocationBefore(List<int> route, int gap)
    {
        if (gap == 0)
        {
            return startIndex;
        }
        return stopLocation[route[gap - 1]];
    }

    // Point right after gap i (-1 if the gap is at the end of the route)
    private int LocationAfter(List<int> route, int gap)
    {
        if (gap >= route.Count)
        {
            return -1;
        }
        return stopLocation[route[gap]];
    }

    private int RouteCost(List<int> route)
    {
        int total = 0;
        int current = startIndex;

        foreach (int stop in route)
        {
            total += distance[current, stopLocation[stop]];
            current = stopLocation[stop];
        }

        return total;
    }

    private void Shuffle(List<int> list, Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            int temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    // Converts the internal numbers into Stop objects
    private List<Stop> ToStops(List<int> route)
    {
        List<Stop> stops = new List<Stop>();

        foreach (int stop in route)
        {
            if (stop < packageCount)
            {
                stops.Add(new Stop(points[stopLocation[stop]], StopKind.Pickup, packages[stop].Id));
            }
            else
            {
                stops.Add(new Stop(points[stopLocation[stop]], StopKind.Delivery, packages[stop - packageCount].Id));
            }
        }

        return stops;
    }
}
