namespace Backend;

// Weighted graph stored as an adjacency list.
// Each vertex has a list with the edges that leave from it.
public class Graph<T> where T : notnull
{
    private readonly Dictionary<T, List<Edge<T>>> adjacency = new();

    public bool Directed { get; }

    public Graph(bool directed)
    {
        Directed = directed;
    }

    public int VertexCount
    {
        get { return adjacency.Count; }
    }

    public IEnumerable<T> Vertices
    {
        get { return adjacency.Keys; }
    }

    public bool Contains(T vertex)
    {
        return adjacency.ContainsKey(vertex);
    }

    public void AddVertex(T vertex)
    {
        if (!adjacency.ContainsKey(vertex))
        {
            adjacency[vertex] = new List<Edge<T>>();
        }
    }

    // In a directed graph this adds ONE edge (from -> to).
    // In an undirected graph it also adds the edge back (to -> from).
    public void AddEdge(T from, T to, int weight)
    {
        AddVertex(from);
        AddVertex(to);

        adjacency[from].Add(new Edge<T>(to, weight));

        if (!Directed)
        {
            adjacency[to].Add(new Edge<T>(from, weight));
        }
    }

    public IReadOnlyList<Edge<T>> Neighbors(T vertex)
    {
        return adjacency[vertex];
    }
}
