using UnityEngine;

public class GenerateProceduralTree : MonoBehaviour {
    public bool hasLeaves;
    public float baseRadius; // base branch width
    public int iterationCount;

    private float baseLength = 1.5F; // base branch length
    private float branchFactor = 0.8F; // branch narrower and wider end width ratio
    private float sizeFactor = 0.8F; // branch length and width change per recursive step
    private float leavesRandomFactor = 0.3F;
    private float leavesScaleFactor = 1.7F;

    private float directionRange = 180; // direction range for the second branch, considering the direction of the first
    private float lowerBranchMinAngle = 45; //
    private float lowerBranchMaxAngle = 60; //
    private float upperBranchMinAngle = 0; //
    private float upperBranchMaxAngle = 50; //

    private float pNoBranch = 0.2F; // chance of no extra branching
    private float pEqualBranch = 0.5F; // chance of two equally sized branches
    //            pSmallerBranch = 1.0 - pNoBranch - pDoublEqualBranch; // chance of one bigger and one smaller branch

    private Color trunkColor = new Color(0.36F, 0.25F, 0.18F, 1.0F);
    private Color leavesColor = new Color(0.64F, 0.82F, 0.25F, 1.0F);

    private GeneralMethods met;
    private IcosphereGenerator icosphereGen;


    void Start() {
        icosphereGen = GetComponent<IcosphereGenerator>();
        met = GetComponent<GeneralMethods>();
        Mesh mesh = GenerateTree(iterationCount);
        mesh = met.ConvertToFlat(mesh);
        GetComponent<MeshFilter>().mesh = mesh;

        Shader standard = Shader.Find("Standard");
        Material trunkMaterial = new Material(standard);
        Material leavesMaterial = new Material(standard);
        trunkMaterial.color = trunkColor;
        leavesMaterial.color = leavesColor;
        trunkMaterial.SetFloat("_Glossiness", 0.0F);
        leavesMaterial.SetFloat("_Glossiness", 0.0F);
        GetComponent<Renderer>().materials = new Material[] { trunkMaterial, leavesMaterial };
    }

    private Mesh GenerateTree(int n) {
        // Generates a self-similar tree mesh of n iterations
        // invariant: first 8 vertices of the returned mesh must form the bottom octagon (for connecting meshes)
        Mesh whole = new Mesh();
        whole.subMeshCount = 2;

        Vector3[] bottom = Octagon();
        bottom = met.ApplyTransformation(bottom, met.Scale(baseRadius, 0, baseRadius));

        if (n <= 1) { // last branch 
            // branch
            Vector3[] branchVertices = new Vector3[9];
            bottom.CopyTo(branchVertices, 0);
            branchVertices[8] = new Vector3(0, baseLength, 0);
            int[] branchTriangles = new int[] {
                8, 0, 1,
                8, 1, 2,
                8, 2, 3,
                8, 3, 4,
                8, 4, 5,
                8, 5, 6,
                8, 6, 7,
                8, 7, 0
            };
            // leaves
            Mesh leaves;
            if (hasLeaves) {
                leaves = icosphereGen.CreateIcosphere(true);
                leaves.vertices = met.Randomize(leaves.vertices, leavesRandomFactor);
                float leavesScale = Random.Range(leavesScaleFactor * 0.8F, leavesScaleFactor * 1.3F);
                leaves.vertices = met.ApplyTransformation(leaves.vertices, met.Translate(0.0F, baseLength, 0.0F) * met.Scale(leavesScale, leavesScale, leavesScale));
            }
            else {
                leaves = new Mesh();
            }
            // combine
            Vector3[] wholeVertices = new Vector3[branchVertices.Length + leaves.vertices.Length];
            branchVertices.CopyTo(wholeVertices, 0);
            leaves.vertices.CopyTo(wholeVertices, branchVertices.Length);
            whole.vertices = wholeVertices;
            whole.SetTriangles(branchTriangles, 0, false);
            whole.SetTriangles(ShiftTriangles(leaves.triangles, branchVertices.Length), 1, false);
            return whole;
        }
        // is not last branch
        float r = (float)Random.Range(0.0F, 1.0F);
        if (r < pNoBranch) { // a single branch continues, only direction might change
            Mesh onlyBranch = GenerateTree(n - 1);
            float direction = Random.Range(0, 360);
            float angle = Random.Range(upperBranchMinAngle, upperBranchMaxAngle);
            Matrix4x4 branchMatrix = GenerateBranchMatrix(direction, angle);
            onlyBranch.vertices = met.ApplyTransformation(onlyBranch.vertices, branchMatrix);

            Vector3[] top = GetTop();
            Vector3[] baseVertices = new Vector3[16];
            bottom.CopyTo(baseVertices, 0);
            top.CopyTo(baseVertices, 8);
            Mesh baseBranch = new Mesh(); // base branch without its subbranches
            baseBranch.subMeshCount = 2;
            baseBranch.vertices = baseVertices;
            baseBranch.SetTriangles(ConnectOctagons(0, 8), 0, false);

            whole = CombineMeshes(baseBranch, onlyBranch);
            int[] trunkTriangles = new int[whole.GetTriangles(0).Length + 48];
            int[] topToFirstTraingles = ConnectOctagons(8, 16);
            whole.GetTriangles(0).CopyTo(trunkTriangles, 0);
            topToFirstTraingles.CopyTo(trunkTriangles, whole.GetTriangles(0).Length);
            whole.SetTriangles(trunkTriangles, 0);
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
            firstBranch.vertices = met.ApplyTransformation(firstBranch.vertices, firstBranchMatrix);
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
            secondBranch.vertices = met.ApplyTransformation(secondBranch.vertices, secondBranchMatrix);
            // whole
            Vector3[] top = GetTop();
            Vector3[] baseVertices = new Vector3[16];
            bottom.CopyTo(baseVertices, 0);
            top.CopyTo(baseVertices, 8);
            Mesh baseBranch = new Mesh(); // base branch without its subbranches
            baseBranch.subMeshCount = 2;
            baseBranch.vertices = baseVertices;
            baseBranch.SetTriangles(ConnectOctagons(0, 8), 0, false);
            baseBranch.SetTriangles(new int[0], 1, false);

            whole = CombineMeshes(CombineMeshes(baseBranch, firstBranch), secondBranch);
            int trianglesSum = whole.GetTriangles(0).Length + whole.GetTriangles(1).Length;
            int[] trunkTriangles = new int[trianglesSum + 96];
            int[] topToFirstTriangles = ConnectOctagons(8, 16);
            int[] topToSecondTriangles = ConnectOctagons(8, 16 + firstBranch.vertices.Length);
            whole.GetTriangles(0).CopyTo(trunkTriangles, 0);
            topToFirstTriangles.CopyTo(trunkTriangles, whole.GetTriangles(0).Length);
            topToSecondTriangles.CopyTo(trunkTriangles, whole.GetTriangles(0).Length + topToFirstTriangles.Length);
            whole.SetTriangles(trunkTriangles, 0, false);
        }
        return whole;
    }

    private Vector3[] GetTop() {
        Vector3[] top = Octagon();
        Matrix4x4 scaleTop = met.Scale(baseRadius * branchFactor, 0, baseRadius * branchFactor);
        Matrix4x4 translateTop = met.Translate(0, baseLength, 0);
        return met.ApplyTransformation(top, translateTop * scaleTop);
    }

    private Matrix4x4 GenerateBranchMatrix(float direction, float angle) {
        float baseTopRadius = baseRadius * branchFactor;
        float firstBottomRadius = baseRadius * sizeFactor;

        float h = (float)System.Math.Sin(met.Radians(angle)) * firstBottomRadius;
        float l = baseTopRadius - (float)System.Math.Cos(met.Radians(angle)) * firstBottomRadius;
        float lx = l * (float)System.Math.Cos(met.Radians(direction));
        float lz = l * (float)System.Math.Sin(met.Radians(direction));

        Matrix4x4 scaleBranch = met.Scale(sizeFactor, sizeFactor, sizeFactor);
        Matrix4x4 rotateBranch1 = met.Rotate(0, direction, 0);
        Matrix4x4 rotateBranch2 = met.Rotate(0, 0, angle);
        Matrix4x4 rotateBranch3 = met.Rotate(0, -direction, 0);
        Matrix4x4 translateBranch1 = met.Translate(0, baseLength, 0);
        Matrix4x4 translateBranch2 = met.Translate(-lx, h, -lz);
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

    private int[] ShiftTriangles(int[] oldTriangles, int s) {
        int[] newTriangles = new int[oldTriangles.Length];
        for (int i = 0; i < oldTriangles.Length; i++) {
            newTriangles[i] = oldTriangles[i] + s;
        }
        return newTriangles;
    }

    private Mesh CombineMeshes(Mesh first, Mesh second) {
        //only when same submesh count
        Mesh combined = new Mesh();
        combined.subMeshCount = first.subMeshCount;
        Vector3[] combinedVertices = new Vector3[first.vertices.Length + second.vertices.Length];
        first.vertices.CopyTo(combinedVertices, 0);
        second.vertices.CopyTo(combinedVertices, first.vertices.Length);
        combined.vertices = combinedVertices;
        for (int i = 0; i < first.subMeshCount; i++) {
            int[] triangles = new int[first.GetTriangles(i).Length + second.GetTriangles(i).Length];
            first.GetTriangles(i).CopyTo(triangles, 0);
            ShiftTriangles(second.GetTriangles(i), first.vertices.Length).CopyTo(triangles, first.GetTriangles(i).Length);
            combined.SetTriangles(triangles, i, false);
        }
        return combined;
    }
}
