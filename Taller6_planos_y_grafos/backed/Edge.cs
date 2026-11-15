namespace Backend;

// One outgoing street: where it leads and how long it is (in meters).
public readonly record struct Edge<T>(T To, int Weight);
