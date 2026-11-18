namespace Backend;

public static class ShortestPaths
{

    public static Dictionary<string, int> Dijkstra(Graph<string> graph, string source, out Dictionary<string, string> previous)
    {
        Dictionary<string, int> distance = new Dictionary<string, int>();
        previous = new Dictionary<string, string>();

        PriorityQueue<string, int> queue = new PriorityQueue<string, int>();

        distance[source] = 0;
        queue.Enqueue(source, 0);

        while (queue.TryDequeue(out string? current, out int currentDistance))
        {
            if (currentDistance > distance[current])
            {
                continue;
            }

            foreach (Edge<string> edge in graph.Neighbors(current))
            {
                int candidate = currentDistance + edge.Weight;

                if (!distance.TryGetValue(edge.To, out int known) || candidate < known)
                {
                    distance[edge.To] = candidate;
                    previous[edge.To] = current;
                    queue.Enqueue(edge.To, candidate);
                }
            }
        }

        return distance;
    }

    public static List<string> BuildPath(Dictionary<string, string> previous, string source, string target)
    {
        List<string> path = new List<string>();

        string current = target;
        path.Add(current);
        while (current != source)
        {
            current = previous[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
