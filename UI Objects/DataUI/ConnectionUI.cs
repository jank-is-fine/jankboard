using System.Drawing;
using System.Numerics;
using Managers;
using System.Diagnostics;

namespace Rendering.UI
{
    public class ConnectionUI : UIObject
    {
        public bool RenderReady { get; private set; } = false;
        public Connection ReferenceConnection = null!;
        private Vector2? _source;
        private Vector2? _target;
        private float[] _lineVertices = [];
        private uint[] _lineIndices = [];

        public override long RenderKey
        {
            get => ReferenceConnection.SavedRenderKey;
            set
            {
                ReferenceConnection.SavedRenderKey = value;
            }
        }

        public ConnectionUI(Connection TargetConnection)
        {
            Shader = ShaderManager.GetShaderByName("Default Shader");
            if (Shader == null)
            {
                Logger.Log("ConnectionUI", $"Could not get Default Shader, hiding Connection Guid: {TargetConnection.guid}", LogLevel.ERROR);
                Debug.WriteLine("ConnectionUI: could not get Default Shader, hiding"); IsVisible = false; return;
            }

            ReferenceConnection = TargetConnection;

            UpdateConnection();
        }

        public Vector3[]? GetBatchVertices()
        {
            if (_lineVertices == null || _lineVertices.Length == 0)
                return null;

            var vertices = new Vector3[_lineVertices.Length / 3];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector3(
                    _lineVertices[i * 3],
                    _lineVertices[i * 3 + 1],
                    _lineVertices[i * 3 + 2]
                );
            }
            return vertices;
        }

        public uint[]? GetBatchIndices()
        {
            if (_lineIndices == null)
                return null;

            return _lineIndices;
        }

        public void UpdateConnection()
        {
            var sourceEntry = EntryManager.GetEntryUIByGuid(ReferenceConnection.SourceEntry);
            var targetEntry = EntryManager.GetEntryUIByGuid(ReferenceConnection.TargetEntry);

            if (sourceEntry == null || targetEntry == null)
            {
                if (!ReferenceConnection.IsDeleted)
                {
                    Logger.Log("ConnectionUI", $"source or target not found in connection. Connection Guid: {ReferenceConnection.guid}", LogLevel.WARNING);
                    Debug.WriteLine("source or target not found in connection");
                    IsVisible = false;
                    RenderReady = false;

                    //most likely a left behind connection which did not get cleaned up last time due to the UndoRedoManager
                    //just to be safe lets remove this connection
                    ReferenceConnection.IsDeleted = true;
                }
                return;
            }

            var sourceBounds = sourceEntry.Bounds;
            var targetBounds = targetEntry.Bounds;

            Vector2 sourceCenter = new(sourceBounds.X + sourceBounds.Width / 2, sourceBounds.Y + sourceBounds.Height / 2);
            Vector2 targetCenter = new(targetBounds.X + targetBounds.Width / 2, targetBounds.Y + targetBounds.Height / 2);

            _source = CalculateConnectionPoint(sourceBounds, targetCenter);
            _target = CalculateConnectionPoint(targetBounds, sourceCenter);

            UpdateLineGeometry();
            if (IsVisible)
            {
                RenderReady = true;
                ChunkManager.RemoveObject(this);
                ChunkManager.AddObject(this);
            }
        }

        private Vector2 CalculateConnectionPoint(RectangleF bounds, Vector2 targetPoint)
        {
            Vector2 center = new(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

            Vector2 direction = targetPoint - center;

            if (direction.LengthSquared() <= float.Epsilon) { return center; }

            direction = Vector2.Normalize(direction);

            float scaleX = bounds.Width / 2f / Math.Abs(direction.X);
            float scaleY = bounds.Height / 2f / Math.Abs(direction.Y);

            float scale = Math.Min(scaleX, scaleY);

            return center + direction * scale;
        }

        private void UpdateLineGeometry()
        {
            if (_source == null || _target == null || ReferenceConnection.IsDeleted)
            {
                IsVisible = false;
                return;
            }

            try
            {
                Vector2 sourceCenter = (Vector2)_source;
                Vector2 targetCenter = (Vector2)_target;

                Vector2 direction = targetCenter - sourceCenter;
                float distance = direction.Length();

                if (distance < float.Epsilon)
                {
                    return;
                }

                var (vertices, indices) = ConnectionMeshHelper.GenerateConnectionMesh(
                    sourceCenter,
                    targetCenter,
                    ReferenceConnection.arrowType,
                    Settings.ConnectionSize
                );

                _lineVertices = vertices;
                _lineIndices = indices;

                IsVisible = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ConnectionUI: Error in UpdateLineGeometry: {ex.Message}");
                Logger.Log(
                    "ConnectionUI", $"Error in UpdateLineGeometry. Connection: {ReferenceConnection.guid}\n Error: {ex.Message}\nStacktrace:\n{ex.StackTrace}",
                    LogLevel.ERROR
                );

                IsVisible = false;
            }
        }

        public override RectangleF Bounds
        {
            get
            {
                if (_source == null || _target == null)
                    return RectangleF.Empty;

                Vector2 a = (Vector2)_source;
                Vector2 b = (Vector2)_target;

                float left = Math.Min(a.X, b.X);
                float top = Math.Min(a.Y, b.Y);
                float width = Math.Abs(b.X - a.X);
                float height = Math.Abs(b.Y - a.Y);

                return new RectangleF(
                    left - Settings.ConnectionSize * 0.5f,
                    top - Settings.ConnectionSize * 0.5f,
                    width + Settings.ConnectionSize,
                    height + Settings.ConnectionSize
                );
            }
        }

        public override bool ContainsPoint(Vector2 point)
        {
            if (_source == null || _target == null)
                return false;

            Vector2 a = (Vector2)_source;
            Vector2 b = (Vector2)_target;

            Vector2 AtoB = b - a;

            Vector2 AtoPoint = point - a;

            float abLengthSquared = Vector2.Dot(AtoB, AtoB);
            if (abLengthSquared <= float.Epsilon)
            {
                return Vector2.Distance(a, point) <= Settings.ConnectionSize;
            }

            float t = Vector2.Dot(AtoPoint, AtoB) / abLengthSquared;
            t = Math.Clamp(t, 0f, 1f);

            Vector2 closest = a + AtoB * t;

            float distanceSquared = Vector2.DistanceSquared(point, closest);
            return distanceSquared <= Settings.ConnectionSize * Settings.ConnectionSize;
        }

        public override bool IntersectsWithRect(RectangleF rect)
        {
            if (_source == null || _target == null)
                return false;

            Vector2 sourcePos = (Vector2)_source;
            Vector2 targetPos = (Vector2)_target;

            if (rect.Contains(new PointF(sourcePos.X, sourcePos.Y)) || rect.Contains(new PointF(targetPos.X, targetPos.Y)))
            {
                return true;
            }

            Vector2 r1 = new(rect.Left - Settings.ConnectionSize / 2f, rect.Top - Settings.ConnectionSize / 2f);
            Vector2 r2 = new(rect.Right + Settings.ConnectionSize / 2f, rect.Top - Settings.ConnectionSize / 2f);
            Vector2 r3 = new(rect.Right + Settings.ConnectionSize / 2f, rect.Bottom + Settings.ConnectionSize / 2f);
            Vector2 r4 = new(rect.Left - Settings.ConnectionSize / 2f, rect.Bottom + Settings.ConnectionSize / 2f);

            return
                LinesIntersect(sourcePos, targetPos, r1, r2) ||
                LinesIntersect(sourcePos, targetPos, r2, r3) ||
                LinesIntersect(sourcePos, targetPos, r3, r4) ||
                LinesIntersect(sourcePos, targetPos, r4, r1);
        }

        private static bool LinesIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            float d1 = Direction(p3, p4, p1);
            float d2 = Direction(p3, p4, p2);
            float d3 = Direction(p1, p2, p3);
            float d4 = Direction(p1, p2, p4);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            {
                return true;
            }

            return false;
        }

        private static float Direction(Vector2 a, Vector2 b, Vector2 c)
        {
            return (c.X - a.X) * (b.Y - a.Y) -
                   (c.Y - a.Y) * (b.X - a.X);
        }

        public override void Dispose()
        {
            IsDisposed = true;
        }
    }
}