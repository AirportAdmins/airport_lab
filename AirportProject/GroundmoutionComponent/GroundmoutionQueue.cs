using System;
using System.Collections.Generic;
using AirportLibrary.Graph;
using AirportLibrary.DTO;
using AirportLibrary;

namespace GroundmoutionComponent
{
    class GroundmoutionQueue
    {
        public Dictionary<Edge<int>, Queue<MotionPermissionRequest>> dictBusyEdges { get; }

        public GroundmoutionQueue()
        {
            dictBusyEdges = new Dictionary<Edge<int>, Queue<MotionPermissionRequest>>();
            foreach (var edge in new Map().Graph.Edges)
                dictBusyEdges.Add(edge, new Queue<MotionPermissionRequest>());
        }

        private Edge<int> FindEdge(int vertexFrom, int vertexTo)
        {
            foreach (var edge in dictBusyEdges.Keys)
                if (edge.ConnVertices.Item1.Id == vertexFrom && edge.ConnVertices.Item2.Id == vertexTo || edge.ConnVertices.Item2.Id == vertexFrom && edge.ConnVertices.Item1.Id == vertexTo)
                    return edge;
            return null;
        }
        public bool IsFree(MotionPermissionRequest request)
        {
            var edge = FindEdge(request.StartVertex, request.DestinationVertex);
            return (edge != null) ? dictBusyEdges[edge].Count==0 : false;
        }

        public bool Enqueue(MotionPermissionRequest request)
        {
            var edge = FindEdge(request.StartVertex, request.DestinationVertex);
            if (edge != null)
            {
                dictBusyEdges[edge].Enqueue(request);
            }
            return edge!=null;
        }

        public MotionPermissionRequest Dequeue(MotionPermissionRequest request)
        {
            var edge = FindEdge(request.StartVertex, request.DestinationVertex);
            if (edge != null)
            {
                return dictBusyEdges[edge].Dequeue();
            }
            return null;
        }
        public MotionPermissionRequest Peek(MotionPermissionRequest request)
        {
            var edge = FindEdge(request.StartVertex, request.DestinationVertex);
            MotionPermissionRequest peek;
            try
            {
                peek = dictBusyEdges[edge].Peek();
            }
            catch
            {
                peek = null;
            }
            return peek;
        }
    }
}
