using System;
using System.Collections.Generic;
using System.Linq;

namespace WolfGoatCabbage
{
    public class VertexCoverGraph
    {
        private List<Vertex> _vertices = new List<Vertex>();
        private List<Edge> _edges = new List<Edge>();
        private List<int> _finalK = new List<int>();
        private List<Vertex> _minimumVertexCovers = new List<Vertex>();
        private List<Vertex> _currentVertexCovers = new List<Vertex>();

        private struct Vertex
        {
            public string Name { get; set; }
            public List<Edge> ConnectingEdges { get; set; }
            public string[] Conflicts { get; }

            public Vertex(string name, string[] conflicts)
            {
                Name = name;
                Conflicts = conflicts.ToArray();
                ConnectingEdges = new List<Edge>();
            }

            public bool Equals(Vertex v)
            {
                return Name == v.Name && ConnectingEdges.Equals(v.ConnectingEdges);
            }
        }
        private struct Edge
        {
            public Vertex Index1 { get; }
            public Vertex Index2 { get; }

            public Edge(Vertex index1, Vertex index2)
            {
                Index1 = index1;
                Index2 = index2;
            }

            public bool Equals(Edge e)
            {
                return Index1.Name == e.Index1.Name && Index2.Name == e.Index2.Name;
            }
        }
        public bool ContainEdge(string first, string second)
        {
            return _edges.Any(e => (e.Index1.Name == first && e.Index2.Name == second) || (e.Index1.Name == second && e.Index2.Name == first));
        }
        public bool CreateNode(string name, string[] conflicts)
        {
            if (_vertices.Any(v => v.Name == name)) return false;
            Vertex newNode = new Vertex(name, conflicts);
            _vertices.Add(newNode);
            return true;
        }
        public void GenerateConnectingEdge()
        {
            _vertices.ForEach(v => _vertices.Where(u => !u.Equals(v)).ToList().ForEach(w =>
                {
                    if (v.Conflicts.Contains(w.Name) && !ContainEdge(v.Name, w.Name))
                    {
                        Edge e = new Edge(v, w);
                        _edges.Add(e);
                        v.ConnectingEdges.Add(e);
                        w.ConnectingEdges.Add(e);
                    }
                }));
        }
        public int AlcuinNumber()
        {
            if (_finalK.Count(x => x == _vertices.Count - _minimumVertexCovers.Count) > 1) return _minimumVertexCovers.Count;
            _finalK.Clear();
            HashSet<Vertex> nonVertexCovers = new HashSet<Vertex>(_vertices.Where(x => !_minimumVertexCovers.Contains(x)));
            for (int i = 1; i < Math.Pow(2, _minimumVertexCovers.Count); i++)
            {
                HashSet<Vertex> s1 = new HashSet<Vertex>();
                for (int k = 0; k < _minimumVertexCovers.Count; k++)
                {
                    int nextDigitValue = (int)Math.Pow(2, k + 1);
                    int currentDigitValue = (int)Math.Pow(2, k);
                    if (i % nextDigitValue / currentDigitValue == 1) s1.Add(_minimumVertexCovers[k]);
                }
                HashSet<Vertex> x1 = new HashSet<Vertex>(nonVertexCovers.Where(x => !x.ConnectingEdges.Any(e => s1.Contains(e.Index1) || s1.Contains(e.Index2))));
                for (int j = 1; j < Math.Pow(2, _minimumVertexCovers.Count); j++)
                {
                    HashSet<Vertex> s2 = new HashSet<Vertex>();
                    for (int k = 0; k < _minimumVertexCovers.Count; k++)
                    {
                        int nextDigitValue = (int)Math.Pow(2, k + 1);
                        int currentDigitValue = (int)Math.Pow(2, k);
                        if (j % nextDigitValue / currentDigitValue == 1) s2.Add(_minimumVertexCovers[k]);
                    }
                    HashSet<Vertex> x2 = new HashSet<Vertex>(nonVertexCovers.Where(x => !x1.Contains(x)).Where(x => !x.ConnectingEdges.Any(e => s2.Contains(e.Index1) || s2.Contains(e.Index2))));
                    x2.UnionWith(x1);
                    HashSet<Vertex> x3 = new HashSet<Vertex>(nonVertexCovers.Where(x => !x2.Contains(x)));
                    if (s1.Count + s2.Count >= x3.Count) return _minimumVertexCovers.Count;
                }
            }
            return _minimumVertexCovers.Count + 1;
        }
        public int BoundSearchTree(int k)
        {
            if (k == 0 || _edges.Count == 0)
            {
                if (_minimumVertexCovers.Count == 0 || _minimumVertexCovers.Count > _currentVertexCovers.Count)
                    _minimumVertexCovers = new List<Vertex>(_currentVertexCovers);
                _finalK.Add(k);
                return 0;
            }
            Edge edge = _edges[0];
            List<Edge> removedEdges = new List<Edge>();
            Vertex vertex = edge.Index1;
            _currentVertexCovers.Add(vertex);
            foreach (Edge e in _edges.ToArray())
            {
                if (e.Index1.Equals(vertex) || e.Index2.Equals(vertex))
                {
                    removedEdges.Add(e);
                    _edges.Remove(e);
                }
            }
            _vertices.Remove(vertex);
            int i = BoundSearchTree(k - 1);
            _vertices.Add(vertex);
            _currentVertexCovers.Remove(vertex);
            _edges.AddRange(removedEdges);
            removedEdges.Clear();
            vertex = edge.Index2;
            foreach (Edge e in _edges.ToArray())
            {
                if (e.Index1.Equals(vertex) || e.Index2.Equals(vertex))
                {
                    removedEdges.Add(e);
                    _edges.Remove(e);
                }
            }
            _vertices.Remove(vertex);
            _currentVertexCovers.Add(vertex);
            int j = BoundSearchTree(k - 1);
            _vertices.Add(vertex);
            _currentVertexCovers.Remove(vertex);
            _edges.AddRange(removedEdges);
            removedEdges.Clear();
            return Math.Min(i, j) + 1;
        }
    }
}