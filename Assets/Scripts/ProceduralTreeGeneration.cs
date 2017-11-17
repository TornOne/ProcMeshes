using UnityEngine;

public class ProceduralTreeGeneration : MonoBehaviour {
    private float baseLength = 6.0F; // base branch length
    private float baseRadius = 0.3F; // base branch width
    private float branchFactor = 0.8F; // branch narrower and wider end width ratio
    private float sizeFactor = 0.8F; // branch length and width change per recursive step

    private float directionRange = 180; // direction range for the second branch, considering the direction of the first
    private float lowerBranchMinAngle = 45; //
    private float lowerBranchMaxAngle = 60; //
    private float upperBranchMinAngle = 0; //
    private float upperBranchMaxAngle = 50; //

    private float pNoBranch = 0.2F; // chance of no extra branching
    private float pEqualBranch = 0.5F; // chance of two equally sized branches
    //            pSmallerBranch = 1.0 - pNoBranch - pDoublEqualBranch; // chance of one bigger and one smaller branch

    void Start() {
        GetComponent<MeshFilter>().mesh = GenerateTree(6);
        GetComponent<Renderer>().material.color = new Color(0.78F, 0.62F, 0.39F, 1.0F);
    }

    void Update() {

    }

    Mesh GenerateTree(int n) {
        // Generates a self-similar tree mesh of n iterations
        // invariant: first 8 vertices of the returned mesh must form the bottom octagon (for connecting meshes)
        Mesh whole = new Mesh();
        Vector3[] wholeVertices;
        int[] wholeTriangles;

        Vector3[] bottom = Octagon();
        bottom = ApplyTransformation(bottom, Scale(baseRadius, 0, baseRadius));

        if (n <= 1) { // last branch, thus a cone
            wholeVertices = new Vector3[9];
            for (int i = 0; i < 8; i++) {
                wholeVertices[i] = bottom[i];
            }
            wholeVertices[8] = new Vector3(0, baseLength, 0);
            wholeTriangles = new int[] {
                8, 0, 1,
                8, 1, 2,
                8, 2, 3,
                8, 3, 4,
                8, 4, 5,
                8, 5, 6,
                8, 6, 7,
                8, 7, 0
            };
            whole.vertices = wholeVertices;
            whole.triangles = wholeTriangles;
            return whole;
        }
        
        // is not last branch
        float r = (float)Random.Range(0.0F, 1.0F);
        if (r < pNoBranch) { // a single branch continues, only direction might change
            Mesh onlyBranch = GenerateTree(n - 1);
            float direction = Random.Range(0, 360);
            float angle = Random.Range(upperBranchMinAngle, upperBranchMaxAngle);
            Matrix4x4 branchMatrix = GenerateBranchMatrix(direction, angle);
            onlyBranch.vertices = ApplyTransformation(onlyBranch.vertices, branchMatrix);

            Vector3[] top = GetTop();
            Vector3[] baseVertices = new Vector3[16];
            bottom.CopyTo(baseVertices, 0);
            top.CopyTo(baseVertices, 8);
            Mesh baseBranch = new Mesh(); // base branch without its subbranches
            baseBranch.vertices = baseVertices;
            baseBranch.triangles = ConnectOctagons(0, 8);

            whole = CombineMeshes(baseBranch, onlyBranch);
            wholeTriangles = new int[whole.triangles.Length + 48];
            int[] topToFirstTraingles = ConnectOctagons(8, 16);
            whole.triangles.CopyTo(wholeTriangles, 0);
            topToFirstTraingles.CopyTo(wholeTriangles, whole.triangles.Length);
            whole.triangles = wholeTriangles;
        }
        else { // branches to two subbrances
            // first

            Mesh firstBranch = GenerateTree(n - 1);

            float directionFirst = Random.Range(0, 360);
            float angleFirst;
            if (r < pNoBranch + pEqualBranch) {
                angleFirst = Random.Range(lowerBranchMinAngle, lowerBranchMaxAngle);
            }
            else {
                angleFirst = Random.Range(upperBranchMinAngle, upperBranchMaxAngle);
            }

            Matrix4x4 firstBranchMatrix = GenerateBranchMatrix(directionFirst, angleFirst);
            firstBranch.vertices = ApplyTransformation(firstBranch.vertices, firstBranchMatrix);

            // second branch
            Mesh secondBranch;
            if (r < pNoBranch + pEqualBranch) { // branches to two (n - 1) subbranches
                secondBranch = GenerateTree(n - 1);
            }
            else { // branches to one (n - 1) and one (n - 2) subbranches
                secondBranch = GenerateTree(n - 2);
            }

            float directionSecond = Random.Range(directionFirst + 180 - (directionRange / 2), directionFirst + 180 + (directionRange / 2));
            float angleSecond = Random.Range(lowerBranchMinAngle, lowerBranchMaxAngle);

            Matrix4x4 secondBranchMatrix = GenerateBranchMatrix(directionSecond, angleSecond);
            secondBranch.vertices = ApplyTransformation(secondBranch.vertices, secondBranchMatrix);

            // whole
            Vector3[] top = GetTop();
            Vector3[] baseVertices = new Vector3[16];
            bottom.CopyTo(baseVertices, 0);
            top.CopyTo(baseVertices, 8);
            Mesh baseBranch = new Mesh(); // base branch without its subbranches
            baseBranch.vertices = baseVertices;
            baseBranch.triangles = ConnectOctagons(0, 8);

            whole = CombineMeshes(CombineMeshes(baseBranch, firstBranch), secondBranch);
            wholeTriangles = new int[whole.triangles.Length + 96];
            int[] topToFirstTriangles = ConnectOctagons(8, 16);
            int[] topToSecondTriangles = ConnectOctagons(8, 16 + firstBranch.vertices.Length);
            whole.triangles.CopyTo(wholeTriangles, 0);
            topToFirstTriangles.CopyTo(wholeTriangles, whole.triangles.Length);
            topToSecondTriangles.CopyTo(wholeTriangles, whole.triangles.Length + topToFirstTriangles.Length);
            whole.triangles = wholeTriangles;
        }

        return whole;
    }

    private Vector3[] GetTop() {
        Vector3[] top = Octagon();
        Matrix4x4 scaleTop = Scale(baseRadius * branchFactor, 0, baseRadius * branchFactor);
        Matrix4x4 translateTop = Translate(0, baseLength, 0);
        return ApplyTransformation(top, translateTop * scaleTop);
    }

    private Matrix4x4 GenerateBranchMatrix(float direction, float angle) {
        float baseTopRadius = baseRadius * branchFactor;
        float firstBottomRadius = baseRadius * sizeFactor;

        float h = (float)System.Math.Sin(Radians(angle)) * firstBottomRadius;
        float l = baseTopRadius - (float)System.Math.Cos(Radians(angle)) * firstBottomRadius;
        float lx = l * (float)System.Math.Cos(Radians(direction));
        float lz = l * (float)System.Math.Sin(Radians(direction));

        Matrix4x4 scaleBranch = Scale(sizeFactor, sizeFactor, sizeFactor);
        Matrix4x4 rotateBranch1 = Rotate(0, direction, 0);
        Matrix4x4 rotateBranch2 = Rotate(0, 0, angle);
        Matrix4x4 rotateBranch3 = Rotate(0, -direction, 0);
        Matrix4x4 translateBranch1 = Translate(0, baseLength, 0);
        Matrix4x4 translateBranch2 = Translate(-lx, h, -lz);
        Matrix4x4 stacked = translateBranch2 * translateBranch1 * rotateBranch3 * rotateBranch2 * rotateBranch1 * scaleBranch;
        return stacked;
    }

    private static Vector3[] Octagon() {
        // regular convex octagon; ranges: x[-1;1], y=[0;0], z[-1;1]
        float s = (float)System.Math.Sqrt(2) / 2;
        Vector3[] vertices = new Vector3[] {
            new Vector3(0, 0, 1),
            new Vector3(s, 0, s),
            new Vector3(1, 0, 0),
            new Vector3(s, 0, -s),
            new Vector3(0, 0, -1),
            new Vector3(-s, 0, -s),
            new Vector3(-1, 0, 0),
            new Vector3(-s, 0, s)
        };
        return vertices;
    }

    private static int[] ConnectOctagons(int first, int second) {
        // first, second - octagon vertices starting positions
        // first one should be below the second, otherwise triangles inside on the wrong side
        // returns triangles between the octagons
        int[] triangles = new int[48];
        for (int i = 0; i < 7; i++) {
            int k = i * 6;
            triangles[k + 0] = i + first + 0;
            triangles[k + 1] = i + second + 1;
            triangles[k + 2] = i + second + 0;
            triangles[k + 3] = i + first + 0;
            triangles[k + 4] = i + first + 1;
            triangles[k + 5] = i + second + 1;
        }
        triangles[42] = first + 7;
        triangles[43] = second + 0;
        triangles[44] = second + 7;
        triangles[45] = first + 7;
        triangles[46] = first + 0;
        triangles[47] = second + 0;
        return triangles;
    }

    private static Mesh CombineMeshes(Mesh first, Mesh second) {
        Vector3[] combinedVertices = new Vector3[first.vertices.Length + second.vertices.Length];
        first.vertices.CopyTo(combinedVertices, 0);
        second.vertices.CopyTo(combinedVertices, first.vertices.Length);

        int[] combinedTriangles = new int[first.triangles.Length + second.triangles.Length];
        first.triangles.CopyTo(combinedTriangles, 0);
        for (int i = 0; i < second.triangles.Length; i++) {
            combinedTriangles[first.triangles.Length + i] = second.triangles[i] + first.vertices.Length;
        }

        Mesh combined = new Mesh();
        combined.vertices = combinedVertices;
        combined.triangles = combinedTriangles;
        return combined;
    }

    static Matrix4x4 Scale(float x, float y, float z) {
        return Matrix4x4.Scale(new Vector3(x, y, z));
    }

    static Matrix4x4 Translate(float x, float y, float z) {
        return Matrix4x4.Translate(new Vector3(x, y, z));
    }

    static Matrix4x4 Rotate(float x, float y, float z) {
        return Matrix4x4.Rotate(Quaternion.Euler(x, y, z));
    }
    
    static Vector3[] ApplyTransformation(Vector3[] origVertices, Matrix4x4 mat) {
        Vector3[] newVertices = new Vector3[origVertices.Length];
        for (int i = 0; i < origVertices.Length; i++) {
            newVertices[i] = mat.MultiplyPoint3x4(origVertices[i]);
        }
        return newVertices;
    }
    
    static float Radians(float angleInDegrees) {
        // converts angle from degrees to radians
        return (float)(System.Math.PI / 180) * angleInDegrees;
    }
}
