using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateRock : MonoBehaviour {
    private Color rockColor = new Color(0.53F, 0.52F, 0.49F, 1.0F);
    private float maxRandomDistance = 0.4F;
    private float smallRockChance = 0.7F;
    private float smallRockScale = 0.4F;
    private float bigRockScale = 0.75F;
    private float maxRandomScale = 1.5F;
    private float randomScaleChance = 0.3F;

    void Start () {
        IcosphereGenerator icosphereGen = GetComponent<IcosphereGenerator>();
        GeneralMethods met = GetComponent<GeneralMethods>();
        Mesh mesh;
        float r = Random.Range(0.0F, 1.0F);
        if (r < smallRockChance) {
            // small rock
            mesh = icosphereGen.CreateIcosphere(false);
            mesh.vertices = met.Randomize(mesh.vertices, maxRandomDistance);
            mesh.vertices = met.ApplyTransformation(mesh.vertices, met.Scale(smallRockScale, smallRockScale, smallRockScale));
        }
        else {
            // big rock
            mesh = icosphereGen.CreateIcosphere(true);
            mesh.vertices = met.Randomize(mesh.vertices, maxRandomDistance);
            mesh.vertices = met.ApplyTransformation(mesh.vertices, met.Scale(bigRockScale, bigRockScale, bigRockScale));
        }
        
        for (int i = 0; i < 3; i++) {
            r = Random.Range(0.0F, 1.0F);
            if (r < randomScaleChance) {
                float scale = Random.Range(1.0F, maxRandomScale);
                Matrix4x4 mat;
                switch (i) {
                    case 1:
                        mat = met.Scale(scale, 1.0F, 1.0F);
                        break;
                    case 2:
                        mat = met.Scale(1.0F, scale, 1.0F);
                        break;
                    default:
                        mat = met.Scale(1.0F, 1.0F, scale);
                        break;
                }
                mesh.vertices = met.ApplyTransformation(mesh.vertices, mat);
            }
        }
        
        mesh = met.ConvertToFlat(mesh);
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<Renderer>().material.color = rockColor;
        GetComponent<Renderer>().material.SetFloat("_Glossiness", 0.0F);
    }
}
