using System;
using System.Collections.Generic;

namespace AirportLibrary.Graph
{
    public interface IAlgorithm<T>
    {
        List<T> FindShortcut(T vertex1, T vertex2);
    }

    public class Vertex<T>
    {
        public T Id { get; }
        public List<Edge<T>> Edges { get; }
        public Vertex(T id)
        {
            this.Id = id;
            Edges = new List<Edge<T>>();
        }
        public void AddEdge(Edge<T> edge)
        {
            Edges.Add(edge);
        }
    }

    public class Edge<T>
    {
        public Tuple<Vertex<T>, Vertex<T>> ConnVertices { get; }
        public int Weight { get; }
        public Edge(Tuple<Vertex<T>, Vertex<T>> connVertices, int weight)
        {
            ConnVertices = connVertices;
            Weight = weight;
        }
        public Vertex<T> GetOppsiteVertex(Vertex<T> vertex)
        {
            return vertex.Equals(ConnVertices.Item1) ? ConnVertices.Item2 : (vertex.Equals(ConnVertices.Item2) ? ConnVertices.Item1 : null);
        }
    }
    public class Graph<T>
    {
        public List<Vertex<T>> Vertices { get; }
        public Graph()
        {
            Vertices = new List<Vertex<T>>();
        }
        public void AddVertex(T id)
        {
            AddVertex(new Vertex<T>(id));
        }
        public void AddVertex(Vertex<T> vertex)
        {
            if (FindVertex(vertex.Id) == null)
                Vertices.Add(vertex);
        }
        public Vertex<T> FindVertex(T id)
        {
            foreach (var vertex in Vertices)
            {
                if (vertex.Id.Equals(id))
                    return vertex;
            }
            return null;
        }
        public void AddEdge(T id1, T id2, int weight)
        {
            AddEdge(FindVertex(id1), FindVertex(id2), weight);
        }
        public void AddEdge(Vertex<T> vertex1, Vertex<T> vertex2, int weight)
        {
            if (vertex1 != null && vertex2 != null)
            {
                var currEdge = new Edge<T>(new Tuple<Vertex<T>, Vertex<T>>(vertex1, vertex2), weight);
                vertex1.AddEdge(currEdge);
                vertex1.AddEdge(currEdge);
            }
        }
        public int GetWeightBetweenNearVerties(T v1, T v2)
        {
            Vertex<T> vertex1 = FindVertex(v1);
            Vertex<T> vertex2 = FindVertex(v2);

            foreach (Edge<T> edge in FindVertex(v1).Edges)
            {
                if (edge.GetOppsiteVertex(vertex1) == vertex2)
                    return edge.Weight;
            }
            return -1;
        }
    }

    public class Dijkstra<T> : IAlgorithm<T>
    {
        public class DijkstraVertex
        {
            public Vertex<T> Vertex { get; }
            public Vertex<T> PrevVertex;
            public bool IsUnvisited = true;
            public int SumEdgesWeight = int.MaxValue;
            public DijkstraVertex(Vertex<T> vertex)
            {
                Vertex = vertex;
            }
        }
        List<DijkstraVertex> Vertices = new List<DijkstraVertex>();
        Graph<T> Graph;
        public Dijkstra(Graph<T> graph)
        {
            Graph = graph;
            foreach (var vertex in Graph.Vertices)
            {
                Vertices.Add(new DijkstraVertex(vertex));
            }
        }
        public DijkstraVertex GetDijkstraVertex(Vertex<T> vertex)
        {
            foreach (var dijVertex in Vertices)
            {
                if (dijVertex.Vertex.Equals(vertex))
                    return dijVertex;
            }
            return null;
        }
        public DijkstraVertex GetUnvisitedVertexWithMinSum()
        {
            var valMinSum = int.MaxValue;
            DijkstraVertex vertexWithMinSum = null;
            foreach (var vertex in Vertices)
            {
                if (vertex.IsUnvisited && vertex.SumEdgesWeight < valMinSum)
                {
                    valMinSum = vertex.SumEdgesWeight;
                    vertexWithMinSum = vertex;
                }
            }
            return vertexWithMinSum;
        }
        private void SetSumToNearVertices(DijkstraVertex currVertex)
        {
            currVertex.IsUnvisited = false;
            foreach (var edge in currVertex.Vertex.Edges)
            {
                var dijkstVertexNear = GetDijkstraVertex(edge.GetOppsiteVertex(currVertex.Vertex));
                var sumVertexNear = currVertex.SumEdgesWeight + edge.Weight;
                if (sumVertexNear < dijkstVertexNear.SumEdgesWeight)
                {
                    dijkstVertexNear.PrevVertex = currVertex.Vertex;
                    dijkstVertexNear.SumEdgesWeight = sumVertexNear;
                }
            }
        }
        public List<T> FindShortcut(T vertex1, T vertex2)
        {
            var fromVertex = Graph.FindVertex(vertex1);
            var toVertex = Graph.FindVertex(vertex2);

            GetDijkstraVertex(fromVertex).SumEdgesWeight = 0;
            while (true)
            {
                var currDijkstVertex = GetUnvisitedVertexWithMinSum();
                if (currDijkstVertex == null)
                    break;
                SetSumToNearVertices(currDijkstVertex);
            }
            return GetShortcut(fromVertex, toVertex);
        }
        private List<T> GetShortcut(Vertex<T> vertex1, Vertex<T> vertex2)
        {
            var shortcut = new List<T>() { vertex2.Id };
            for (var i = 0; i < Vertices.Count * (Vertices.Count - 1) / 2; i++)
            {
                if (vertex1 == vertex2)
                    return shortcut;
                vertex2 = GetDijkstraVertex(vertex2).PrevVertex;
                shortcut.Insert(0, vertex2.Id);
            }
            return null;
        }
    }
}