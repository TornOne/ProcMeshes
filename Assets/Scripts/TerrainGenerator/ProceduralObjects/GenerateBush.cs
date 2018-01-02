using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateBush : MonoBehaviour {
    private Color bushColor = new Color(0.64F, 0.82F, 0.25F, 1.0F);
    private float maxRandomDistance = 0.2F;
    private float shortBushChance = 0.7F;
    private float bushScale = 0.5F;

	void Start () {
        IcosphereGenerator icosphereGen = GetComponent<IcosphereGenerator>();
        GeneralMethods met = GetComponent<GeneralMethods>();
        Mesh mesh = icosphereGen.CreateIcosphere(true);
        mesh.vertices = met.Randomize(mesh.vertices, maxRandomDistance);
        float r = Random.Range(0.0F, 1.0F);
        float yScale;
        if (r < shortBushChance) {
            // round bush
            yScale = Random.Range(0.8F, 1.0F);
        }
        else {
            // tall bush
            yScale = Random.Range(1.8F, 2.2F);
        }
        mesh.vertices = met.ApplyTransformation(mesh.vertices, met.Translate(0.0F, 0.5F * yScale * bushScale, 0.0F) * met.Scale(bushScale, bushScale * yScale, bushScale));

        mesh = met.ConvertToFlat(mesh);
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<Renderer>().material.color = bushColor;
        GetComponent<Renderer>().material.SetFloat("_Glossiness", 0.0F);
    }
}
