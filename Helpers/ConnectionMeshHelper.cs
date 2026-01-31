using System.Numerics;

/// <summary>
/// <para>Helper to generate geometry for connection arrows and lines between points in world space</para>
/// </summary>

public static class ConnectionMeshHelper
{
    public static (float[] vertices, uint[] indices) GenerateConnectionMesh(
        Vector2 startingPos,
        Vector2 endPos,
        ArrowType arrowType,
        float lineThickness = 3.0f
        )
    {
        return arrowType switch
        {
            ArrowType.Default => GenerateStraightArrow(startingPos, endPos, lineThickness, lineThickness * 4),
            ArrowType.Loose => GenerateDashedArrow(startingPos, endPos, lineThickness),
            ArrowType.LooseDiagonal => GenerateDiagonalDashedArrow(startingPos, endPos, lineThickness),
            _ => GenerateStraightArrow(startingPos, endPos, lineThickness, lineThickness * 4)
        };
    }

    private static (float[] vertices, uint[] indices) GenerateStraightArrow(
        Vector2 start, Vector2 end, float thickness, float arrowSize)
    {
        Vector2 direction = end - start;
        float distance = direction.Length();

        if (distance < float.Epsilon)
            return ([], []);

        direction = Vector2.Normalize(direction);
        Vector2 perpendicular = new(-direction.Y, direction.X);

        Vector2 lineStart = start + direction;
        Vector2 lineEnd = end - direction;
        Vector2 arrowBase = lineEnd - direction * arrowSize;

        float[] vertices = new float[7 * 3]; // 7 vertices * (x, y, z)
        uint[] indices = new uint[9]; // 3 triangles

        // Line vertices
        SetVertex(vertices, 0, lineStart - perpendicular * thickness);
        SetVertex(vertices, 1, lineStart + perpendicular * thickness);
        SetVertex(vertices, 2, arrowBase + perpendicular * thickness);
        SetVertex(vertices, 3, arrowBase - perpendicular * thickness);

        // Arrowhead vertices
        SetVertex(vertices, 4, arrowBase - perpendicular * arrowSize);
        SetVertex(vertices, 5, arrowBase + perpendicular * arrowSize);
        SetVertex(vertices, 6, end);

        // Line triangles
        indices[0] = 0; indices[1] = 1; indices[2] = 2;
        indices[3] = 0; indices[4] = 2; indices[5] = 3;

        // Arrowhead triangle
        indices[6] = 4; indices[7] = 5; indices[8] = 6;

        return (vertices, indices);
    }

    private static (float[] vertices, uint[] indices) GenerateDashedArrow(
        Vector2 start, Vector2 end, float thickness)
    {
        Vector2 direction = end - start;
        float distance = direction.Length();

        if (distance < float.Epsilon)
            return ([], []);

        direction = Vector2.Normalize(direction);
        Vector2 perpendicular = new(-direction.Y, direction.X);

        Vector2 lineStart = start;
        Vector2 lineEnd = end;

        int dashCount = (int)(distance / thickness) + 1;

        List<float> verticesList = [];
        List<uint> indicesList = [];
        uint currentIndex = 0;

        for (int i = 0; i < dashCount; i++)
        {
            float startT = i * 2 / (float)(dashCount * 2);
            float endT = ((i * 2) + 1) / (float)(dashCount * 2);

            Vector2 dashStart = Vector2.Lerp(lineStart, lineEnd, startT);
            Vector2 dashEnd = Vector2.Lerp(lineStart, lineEnd, endT);

            AddQuad(verticesList, indicesList, ref currentIndex,
                   dashStart, dashEnd, perpendicular, thickness);
        }

        return (verticesList.ToArray(), indicesList.ToArray());
    }

    private static (float[] vertices, uint[] indices) GenerateDiagonalDashedArrow(
    Vector2 start, Vector2 end, float thickness)
    {
        Vector2 direction = end - start;
        float distance = direction.Length();

        if (distance < float.Epsilon)
            return ([], []);

        direction = Vector2.Normalize(direction);
        Vector2 perpendicular = new(-direction.Y, direction.X);

        Vector2 diagonalOffset = direction * thickness * 2f;
        start += diagonalOffset / 2;
        end -= diagonalOffset / 2;

        int dashCount = (int)(distance / thickness/2f);

        List<float> verticesList = [];
        List<uint> indicesList = [];
        uint currentIndex = 0;

        float gapSize = 5f;
        float totalLineLength = distance;
        float availableSpace = totalLineLength - (gapSize * (dashCount + 1));
        float dashLength = availableSpace / dashCount;

        // Add starting triangle for first gap
        Vector2 triangleStart = start - diagonalOffset;
        Vector2 triangleEnd = start + diagonalOffset;

        Vector2 v0 = triangleStart - perpendicular * thickness; // bottom-left of start
        Vector2 v1 = triangleStart + perpendicular * thickness; // top-left of start  
        Vector2 v2 = triangleEnd + perpendicular * thickness;

        verticesList.Add(v0.X); verticesList.Add(v0.Y); verticesList.Add(0f);
        verticesList.Add(v1.X); verticesList.Add(v1.Y); verticesList.Add(0f);
        verticesList.Add(v2.X); verticesList.Add(v2.Y); verticesList.Add(0f);

        indicesList.Add(currentIndex);
        indicesList.Add(currentIndex + 1);
        indicesList.Add(currentIndex + 2);

        currentIndex += 3;


        // diagonal dashes
        for (int i = 0; i < dashCount; i++)
        {
            float startDistance = gapSize + i * (dashLength + gapSize);
            float endDistance = startDistance + dashLength;

            float startT = startDistance / totalLineLength;
            float endT = endDistance / totalLineLength;

            Vector2 dashStart = Vector2.Lerp(start, end, startT);
            Vector2 dashEnd = Vector2.Lerp(start, end, endT);

            AddQuad(verticesList, indicesList, ref currentIndex,
                   dashStart, dashEnd, perpendicular, thickness, diagonalOffset);
        }

        triangleStart = end + diagonalOffset;
        triangleEnd = end - diagonalOffset;

        // Add ending triangle for last gap
        v0 = triangleStart + perpendicular * thickness;
        v1 = triangleStart - perpendicular * thickness;
        v2 = triangleEnd - perpendicular * thickness;

        verticesList.Add(v0.X); verticesList.Add(v0.Y); verticesList.Add(0f);
        verticesList.Add(v1.X); verticesList.Add(v1.Y); verticesList.Add(0f);
        verticesList.Add(v2.X); verticesList.Add(v2.Y); verticesList.Add(0f);

        indicesList.Add(currentIndex);
        indicesList.Add(currentIndex + 1);
        indicesList.Add(currentIndex + 2);

        currentIndex += 3;

        return (verticesList.ToArray(), indicesList.ToArray());
    }

    // Helper methods
    private static void SetVertex(float[] vertices, int index, Vector2 position)
    {
        vertices[index * 3] = position.X;
        vertices[index * 3 + 1] = position.Y;
        vertices[index * 3 + 2] = 0f; 
    }

    private static void AddQuad(List<float> vertices, List<uint> indices, ref uint currentIndex,
                                Vector2 start, Vector2 end, Vector2 perpendicular, float thickness, Vector2? diagonalOffset = null)
    {
        uint baseIndex = currentIndex;

        if (diagonalOffset == null)
        {
            diagonalOffset = new Vector2(0, 0);
        }

        Vector2 _diagonalOffset = (Vector2)diagonalOffset;

        // Bottom-left
        vertices.Add(start.X - _diagonalOffset.X - perpendicular.X * thickness);
        vertices.Add(start.Y - _diagonalOffset.Y - perpendicular.Y * thickness);
        vertices.Add(0f);

        // Bottom-right  
        vertices.Add(end.X - _diagonalOffset.X - perpendicular.X * thickness);
        vertices.Add(end.Y - _diagonalOffset.Y - perpendicular.Y * thickness);
        vertices.Add(0f);

        // Top-left
        vertices.Add(start.X + _diagonalOffset.X + perpendicular.X * thickness);
        vertices.Add(start.Y + _diagonalOffset.Y + perpendicular.Y * thickness);
        vertices.Add(0f);

        // Top-right
        vertices.Add(end.X + _diagonalOffset.X + perpendicular.X * thickness);
        vertices.Add(end.Y + _diagonalOffset.Y + perpendicular.Y * thickness);
        vertices.Add(0f);

        // First triangle
        indices.Add(baseIndex);
        indices.Add(baseIndex + 1);
        indices.Add(baseIndex + 3);

        // Second triangle
        indices.Add(baseIndex);
        indices.Add(baseIndex + 3);
        indices.Add(baseIndex + 2);

        currentIndex += 4;
    }
}