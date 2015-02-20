using UnityEngine;
using System.Collections;
using Exocortex.DSP;
//using CSML;
[ExecuteInEditMode]
public class Plane : MonoBehaviour 
{
    public Color DeepWaterColor = Color.blue;
    public Color ShallowWaterColor = Color.white;
	public Material material;
    Mesh mesh;
    MeshCollider meshC;
    Vector3[] Vertices;
    Vector2[] UVs;
    Vector3[] Normals;
    Vector4[] Tangents;
    int[] Triangles;
    public int LengthWidth = 32;
    public float PlaneScale = 1.0f;
    public float Magnitude = 1.0f;
    public float Speed = 1.0f;
	public float TransverseSpeed = 1.0f;
    public float Height = 1.0f;
	public float Choppiness = 1.0f;
    public bool DoTheWave = false;

	public float OceanSize = 1000;

    public int TextureResolution = 1024;

    public int tiles_x = 2;
    public int tiles_y = 2;

    public Vector2 Wind;

    private int _LengthWidth = 32;
    private float _PlaneScale = 1.0f;

    public float g = 9.81f;

    //Random ran;

    Vector3[] DisplacementMap;
    Vector3[] OriginalPos;
    Texture2D TransparencyMap;
    Texture2D DiffuseMap;
    Texture2D NormalMap;

	public Texture2D DebugDisplacement;
    public Texture2D DebugDiffuse;
	public Texture2D DebugTransparency;
	public Texture2D DebugNormal;

    Color[] Transparency;
    Color[] Diffuse;
    Color[] Normal;
	Color[] Displacement;

    ComplexF[] ComplexDisplacementMap;      //Htilde0
    ComplexF[] ComplexDisplacementMapConj;  //Htilde0mk Conjugate
    ComplexF[] SlopeX;
    ComplexF[] SlopeZ;
    ComplexF[] DisplaceX;
    ComplexF[] DisplaceZ;

	void Start () {
        mesh = new Mesh();
        meshC = GetComponent<MeshCollider>();
        GetComponent<MeshFilter>().mesh = mesh;
        SetUpMesh();
        
        TransparencyMap = new Texture2D(TextureResolution, TextureResolution);
        DiffuseMap = new Texture2D(TextureResolution, TextureResolution);
        NormalMap = new Texture2D(TextureResolution, TextureResolution);

        Transparency = new Color[TextureResolution * TextureResolution];
        Diffuse = new Color[TextureResolution * TextureResolution];
        Normal = new Color[TextureResolution * TextureResolution];
        Displacement = new Color[TextureResolution * TextureResolution];

		DebugDisplacement = new Texture2D(TextureResolution, TextureResolution);
        DebugDiffuse = new Texture2D(TextureResolution, TextureResolution);
		DebugNormal = new Texture2D(TextureResolution, TextureResolution);
		DebugTransparency = new Texture2D(TextureResolution, TextureResolution);

        //Debug.Log("Set up base maps and arrays at " + Time.time);

        ComplexDisplacementMap = new ComplexF[TextureResolution * TextureResolution];
        ComplexDisplacementMapConj = new ComplexF[TextureResolution * TextureResolution];
        DisplaceX = new ComplexF[TextureResolution * TextureResolution];
        DisplaceZ = new ComplexF[TextureResolution * TextureResolution];
        SlopeX = new ComplexF[TextureResolution * TextureResolution];
        SlopeZ = new ComplexF[TextureResolution * TextureResolution];

        _LengthWidth = LengthWidth;
        _PlaneScale = PlaneScale;
        //ran = new Random();

        if (!DoTheWave)
        {
            SetUpComplex();
            //InitGenerator();
        }
        
	}

    void SetUpMesh()
    {
        if(_LengthWidth != LengthWidth || _PlaneScale != PlaneScale)
        {
            Vertices = new Vector3[LengthWidth * LengthWidth];
            Triangles = new int[2 * 3 * (LengthWidth - 1) * (LengthWidth - 1)]; // *2]; // for when we have doubled normals
            UVs = new Vector2[LengthWidth * LengthWidth];
            Normals = new Vector3[LengthWidth * LengthWidth]; // *2]; // for when we have doubled normals
            Tangents = new Vector4[LengthWidth * LengthWidth]; // *2]; // for when we have doubled normals
            DisplacementMap = new Vector3[TextureResolution * TextureResolution];

            int triIndex = 0;                          // start at -1 so the first triIndex++ is 0
            for (int x = 0; x < LengthWidth; x++)
            {
                for (int y = 0; y < LengthWidth; y++)
                {
                    int index = y * LengthWidth + x;
                    Vertices[index] = new Vector3(x *PlaneScale, 0, y * PlaneScale);
                    UVs[index] = new Vector2((float)x/(LengthWidth-1), (float)y/(LengthWidth-1));
                    if (x != (LengthWidth - 1) && y != (LengthWidth - 1))
                    {
                        Triangles[triIndex++] = index;                    // (x,   y)
                        Triangles[triIndex++] = index + LengthWidth;      // (x,   y+1)
                        Triangles[triIndex++] = index + 1;                // (x+1, y)
                        Triangles[triIndex++] = index + LengthWidth;      // (x,   y+1)
                        Triangles[triIndex++] = index + LengthWidth + 1;  // (x+1, y+1)
                        Triangles[triIndex++] = index + 1;                // (x+1, y)
                    }
                }
            }

            for (int x = 0; x < TextureResolution; x++)
            {
                for (int y = 0; y < TextureResolution; y++)
                {
                    int index = y * TextureResolution + x;

                    DisplacementMap[index] = Vector3.zero;
                }
            }
            mesh.Clear();
            mesh.vertices = Vertices;
            mesh.uv = UVs;
            mesh.triangles = Triangles;
            meshC.sharedMesh = mesh;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Tangents = mesh.tangents;
            CalculateTangents();
            Normals = mesh.normals;
            renderer.sharedMaterial = material;
            _LengthWidth = LengthWidth;
            _PlaneScale = PlaneScale;
        }
        //Debug.Log("Setup Mesh OK");
    }

	ComplexF [] htilde0_initial;

    void SetUpComplex()
    {
				htilde0_initial = new ComplexF[TextureResolution * TextureResolution];

		for (int m_prime = 0; m_prime < TextureResolution; m_prime++) {
			for (int n_prime = 0; n_prime < TextureResolution; n_prime++) {

				float kz = 2 * Mathf.PI * (m_prime - TextureResolution / 2) / OceanSize;
				float kx = 2 * Mathf.PI * (n_prime - TextureResolution / 2) / OceanSize;
                
								float len = Mathf.Sqrt (kx * kx + kz * kz);
                
				int index = m_prime * TextureResolution + n_prime;
				htilde0_initial[index] = hTilde_0(n_prime, m_prime);
			}
		}

		//ApplyComplexMapToHeightMap ();


        mesh.Clear();
        mesh.vertices = Vertices;
        mesh.uv = UVs;
        mesh.triangles = Triangles;
        meshC.sharedMesh = mesh;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        Tangents = mesh.tangents;
        CalculateTangents();
        Normals = mesh.normals;
        renderer.sharedMaterial = material;
        _LengthWidth = LengthWidth;
        _PlaneScale = PlaneScale;
        //Debug.Log("Set up complex maps and arrays at " + Time.time);
    }

    void Update()
    {
        if(Application.isEditor)
            SetUpMesh();
        if (DoTheWave)
            UpdateHeightMap();
        //Wind.Normalize();
	}

    void FixedUpdate()
    {
        if(!DoTheWave)EvaluateWaves();
    }

    void UpdateHeightMap()
    {
        for (int x = 0; x < TextureResolution; x++)
        {
            for (int y = 0; y < TextureResolution; y++)
            {
                int index = TextureResolution * y + x;
                float xoffset = x % ((TextureResolution - 1) / (LengthWidth - 1));// *x % ((TextureResolution - 1) / (LengthWidth - 1));
                DisplacementMap[index].x = xoffset * PlaneScale + Magnitude * Mathf.Cos((Speed * Time.time + x/(TextureResolution/LengthWidth)));
                DisplacementMap[index].y = Height * Mathf.Sin((TransverseSpeed * Time.time) + (float)x / (TextureResolution / LengthWidth));   //(float)
                //DebugDiffuse.SetPixel(x, y, new Color(1, (DisplacementMap[index].y+Height)/(2*Height), DisplacementMap[index].z, 1));
                //DisplacementMap[index].z = y % (TextureResolution / LengthWidth) * y % (TextureResolution / LengthWidth) * PlaneScale + Magnitude * Mathf.Cos((Speed * Time.time + x)) * Time.deltaTime * .2f;
                //Debug.Log(x % ((TextureResolution - 1) / (LengthWidth - 1)) * x % ((TextureResolution - 1) / (LengthWidth - 1)) * PlaneScale);
            }
        }
        //DebugDiffuse.Apply();
        GenerateColorFromHeight();
        GenerateTransparencyFromHeight();
        ApplyHeightMap();
        GenerateNormalMap();
    }

    // Needs to be sent to gpu for calculation
    void CalculateTangents()
    {
		Tangents = new Vector4[LengthWidth*LengthWidth];
        Vector3 Q1, Q2;
        Vector2 UV1, UV2;

        Matrix UVMatrix = new Matrix(2, 2);
        Matrix QQMatrix = new Matrix(2, 3);
        Matrix TanMatrix = new Matrix(3, 3);

        for (int x = 0; x < LengthWidth; x++)
        {
            for (int y = 0; y < LengthWidth; y++)
            {
                int index = LengthWidth * y + x;
                if (y == LengthWidth - 1)
                {
                    if (x == LengthWidth - 1)
                    {
                        Q1 = Vertices[index - LengthWidth] - Vertices[index];
                        Q2 = Vertices[index - 1] - Vertices[index];
                        UV1 = UVs[index - LengthWidth] - UVs[index];
                        UV2 = UVs[index - 1] - UVs[index];
                    }
                    else
                    {
                        Q1 = Vertices[index - LengthWidth + 1] - Vertices[index];
                        Q2 = Vertices[index + 1] - Vertices[index];
                        UV1 = UVs[index - LengthWidth + 1] - UVs[index];
                        UV2 = UVs[index + 1] - UVs[index];
                    }
                    
                }
                else if (x == LengthWidth - 1)
                {
                    Q1 = Vertices[index + LengthWidth - 1] - Vertices[index];
                    Q2 = Vertices[index + LengthWidth] - Vertices[index];
                    UV1 = UVs[index + LengthWidth - 1] - UVs[index];
                    UV2 = UVs[index + LengthWidth] - UVs[index];
                }
                else
                {
                    Q1 = Vertices[index + 1] - Vertices[index];
                    Q2 = Vertices[index + LengthWidth] - Vertices[index];
                    UV1 = UVs[index + 1] - UVs[index];
                    UV2 = UVs[index + LengthWidth] - UVs[index];

                }
                QQMatrix = Matrix.Parse("" + Q1.x + " " + Q1.y + " " + Q1.z +
                                    "\r\n" + Q2.x + " " + Q2.y + " " + Q2.z);
                UVMatrix = Matrix.Parse("" + UV2.y + " " + -1 * UV1.y +
                                        "\r\n" + -1 * UV2.x + " " + UV1.x);

                float UV2inv = 1 / (UV1.x * UV2.y - UV2.x * UV1.y);

                TanMatrix = UV2inv * UVMatrix * QQMatrix;

                if (index > Tangents.Length)
                    Debug.LogError("" + index + ">" + Tangents.Length);

                Vector3 Tangent = new Vector3((float)TanMatrix[0,0], (float)TanMatrix[0,1], (float)TanMatrix[0,2]).normalized;
                //Tangent.Normalize();
                Tangents[index] = new Vector4(Tangent.x, Tangent.y, Tangent.z, 1).normalized;
            }
        }
        mesh.tangents = Tangents;
    }

    // Needs to be sent to gpu for calculation
    void GenerateNormalMap()
    {
        // Set Up Sobel Filter
        // x Filter
        // [1.0 0.0 -1.0]
        // [2.0 0.0 -2.0]
        // [1.0 0.0 -1.0]
        // y Filter
        // [1.0   2.0  1.0]
        // [0.0   0.0  0.0]
        // [-1.0 -2.0 -1.0]

        // Local Texture Coordinates + Height)/ Height
        // 0,0 | 1,0 | 2,0
		// ----+-----+----
		// 0,1 | 1,1 | 2,1
		// ----+-----+----
		// 0,2 | 1,2 | 2,2

        float h00;
        float h01;
        float h02;
        float h10;
        float h12;
        float h20;
        float h21;
        float h22;
        float gX;
        float gY;
        float gZ;
        Vector3 temp;

        // Corners
        // Upper Left (0,0)
        h00 = 0;
        h01 = 0;
        h02 = 0;
        h10 = 0;
        h20 = 0;
        h12 = (DisplacementMap[1].y+ Height)/ Height;
        h21 = (DisplacementMap[TextureResolution + 1].y+ Height)/ Height;
        h22 = (DisplacementMap[TextureResolution + 2].y + Height) / Height;

        gX = h00 - h20 + 2.0f * h01 - 2.0f * h21 + h02 - h22;
        gY = h00 + 2.0f * h10 + h20 - h02 - 2.0f * h12 - h22;
        gZ = 0.5f * Mathf.Sqrt(1.0f - gX * gX - gY * gY);
        temp = Vector3.Normalize(new Vector3(gX, gY, gZ));
        Normal[0] = new Color(temp.x, temp.y, temp.z);

        // Upper Right (TextureResolution,0) index = (TextureResolution-1, 0)
        h00 = 0;
        h01 = (DisplacementMap[TextureResolution - 2].y + Height) / Height;
        h02 = (DisplacementMap[TextureResolution + TextureResolution - 2].y + Height) / Height;
        h10 = 0;
        h20 = 0;
        h12 = (DisplacementMap[TextureResolution + TextureResolution - 1].y + Height)/ Height;
        h21 = 0;
        h22 = 0;
        gX = h00 - h20 + 2.0f * h01 - 2.0f * h21 + h02 - h22;
        gY = h00 + 2.0f * h10 + h20 - h02 - 2.0f * h12 - h22;
        gZ = 0.5f * Mathf.Sqrt(1.0f - gX * gX - gY * gY);
        temp = Vector3.Normalize(new Vector3(gX, gY, gZ));
        Normal[TextureResolution-1] = new Color(temp.x, temp.y, temp.z);

        // Lower Left(0, TextureResolution) index = (0, TextureResolution-1) y*TextureResolution+x
        h00 = 0;
        h01 = 0;
        h02 = 0;
        h10 = (DisplacementMap[TextureResolution * (TextureResolution - 1) - TextureResolution].y + Height) / Height;
        h20 = (DisplacementMap[TextureResolution * (TextureResolution - 1) - TextureResolution + 1].y + Height)/ Height;
        h12 = 0;
        h21 = (DisplacementMap[TextureResolution * (TextureResolution - 1) + 1].y + Height)/ Height;
        h22 = 0;
        gX = h00 - h20 + 2.0f * h01 - 2.0f * h21 + h02 - h22;
        gY = h00 + 2.0f * h10 + h20 - h02 - 2.0f * h12 - h22;
        gZ = 0.5f * Mathf.Sqrt(1.0f - gX * gX - gY * gY);
        temp = Vector3.Normalize(new Vector3(gX, gY, gZ));
        Normal[TextureResolution*(TextureResolution - 1)] = new Color(temp.x, temp.y, temp.z);

        // Lower Right(TextureResolution,TextureResolution)
        h00 = (DisplacementMap[TextureResolution * (TextureResolution - 2) + TextureResolution - 2].y + Height)/ Height;
        h01 = (DisplacementMap[TextureResolution * (TextureResolution - 1) + TextureResolution - 2].y + Height)/ Height;
        h02 = 0;
        h10 = (DisplacementMap[TextureResolution * (TextureResolution - 2) + TextureResolution - 1].y + Height) / Height;
        h20 = 0;
        h12 = 0;
        h21 = 0;
        h22 = 0;
        gX = h00 - h20 + 2.0f * h01 - 2.0f * h21 + h02 - h22;
        gY = h00 + 2.0f * h10 + h20 - h02 - 2.0f * h12 - h22;
        gZ = 0.5f * Mathf.Sqrt(1.0f - gX * gX - gY * gY);
        temp = Vector3.Normalize(new Vector3(gX, gY, gZ));
        Normal[TextureResolution * (TextureResolution - 1) + TextureResolution-1] = new Color(temp.x, temp.y, temp.z);

        // Sides
        // Top
        h00 = 0;
        h10 = 0;
        h20 = 0;
        for (int x = 1; x < TextureResolution-1; x++)
        {
            h01 = (DisplacementMap[x-1].y + Height)/ Height;  
            h21 = (DisplacementMap[x+1].y + Height)/ Height;
            h02 = (DisplacementMap[x + TextureResolution - 1].y + Height)/ Height;
            h12 = (DisplacementMap[x + TextureResolution].y + Height)/ Height;
            h22 = (DisplacementMap[x + TextureResolution + 1].y + Height) / Height;
            gX = h00 - h20 + 2.0f * h01 - 2.0f * h21 + h02 - h22;
            gY = h00 + 2.0f * h10 + h20 - h02 - 2.0f * h12 - h22;
            gZ = 0.5f * Mathf.Sqrt(1.0f - gX * gX - gY * gY);
            temp = Vector3.Normalize(new Vector3(gX, gY, gZ));
            Normal[x] = new Color(temp.x, temp.y, temp.z);
        }

        // Bottom
        h02 = 0;
        h12 = 0;
        h22 = 0;
        for (int x = 1; x < TextureResolution - 1; x++)
        {
            h00 = (DisplacementMap[TextureResolution * (TextureResolution - 2) + x - 1].y + Height) / Height;
            h10 = (DisplacementMap[TextureResolution*(TextureResolution-2)+x].y + Height)/ Height;
            h20 = (DisplacementMap[TextureResolution*(TextureResolution-2)+x+1].y + Height)/ Height;
            h01 = (DisplacementMap[TextureResolution*(TextureResolution-1)+x-1].y + Height)/ Height;
            h21 = (DisplacementMap[TextureResolution*(TextureResolution-1)+x+1].y + Height)/ Height;
            gX = h00 - h20 + 2.0f * h01 - 2.0f * h21 + h02 - h22;
            gY = h00 + 2.0f * h10 + h20 - h02 - 2.0f * h12 - h22;
            gZ = 0.5f * Mathf.Sqrt(1.0f - gX * gX - gY * gY);
            temp = Vector3.Normalize(new Vector3(gX, gY, gZ));
            Normal[TextureResolution*(TextureResolution-1)+x] = new Color(temp.x, temp.y, temp.z);
        }

        // Left
        h00 = 0;
        h01 = 0;
        h02 = 0;
        for (int y = 1; y < TextureResolution - 1; y++)
        {
            h10 = (DisplacementMap[TextureResolution*y - TextureResolution].y + Height)/ Height;
            h12 = (DisplacementMap[TextureResolution*y + TextureResolution].y + Height)/ Height;
            h20 = (DisplacementMap[TextureResolution*y - TextureResolution + 1].y + Height)/ Height;
            h21 = (DisplacementMap[TextureResolution*y + 1].y + Height)/ Height;
            h22 = (DisplacementMap[TextureResolution * y + TextureResolution + 1].y + Height) / Height;
            gX = h00 - h20 + 2.0f * h01 - 2.0f * h21 + h02 - h22;
            gY = h00 + 2.0f * h10 + h20 - h02 - 2.0f * h12 - h22;
            gZ = 0.5f * Mathf.Sqrt(1.0f - gX * gX - gY * gY);
            temp = Vector3.Normalize(new Vector3(gX, gY, gZ));
            Normal[TextureResolution * y] = new Color(temp.x, temp.y, temp.z);
        }

        // Right		Starts at x=textureResolution-1
        h20 = 0;
        h21 = 0;
        h22 = 0;
        for (int y = 1; y < TextureResolution - 1; y++)
        {
            h00 = (DisplacementMap[TextureResolution * y + (TextureResolution - 1) - TextureResolution - 1].y + Height)/ Height;
            h01 = (DisplacementMap[TextureResolution * y + (TextureResolution - 1)].y + Height)/ Height;
            h02 = (DisplacementMap[TextureResolution * y + (TextureResolution - 1) - 1].y + Height)/ Height;
            h10 = (DisplacementMap[TextureResolution * y + (TextureResolution - 1) - TextureResolution].y + Height) / Height;
            h12 = (DisplacementMap[TextureResolution * y + (TextureResolution - 1) + TextureResolution].y + Height) / Height;
            gX = h00 - h20 + 2.0f * h01 - 2.0f * h21 + h02 - h22;
            gY = h00 + 2.0f * h10 + h20 - h02 - 2.0f * h12 - h22;
            gZ = 0.5f * Mathf.Sqrt(1.0f - gX * gX - gY * gY);
            temp = Vector3.Normalize(new Vector3(gX, gY, gZ));
            Normal[TextureResolution * y+TextureResolution-1] = new Color(temp.x, temp.y, temp.z);
        }

        // Center
        for (int x = 1; x < TextureResolution - 1; x++)
        {
            for (int y = 1; y < TextureResolution - 1; y++)
            {
                int index = y * TextureResolution + x;
                h00 = (DisplacementMap[index - TextureResolution - 1].y + Height)/ Height;
                h10 = (DisplacementMap[index - TextureResolution].y + Height)/ Height;
                h20 = (DisplacementMap[index - TextureResolution + 1].y + Height)/ Height;
                h01 = (DisplacementMap[index - 1].y + Height)/ Height;
                h21 = (DisplacementMap[index + 1].y + Height)/ Height;
                h02 = (DisplacementMap[index + TextureResolution - 1].y + Height)/ Height;
                h12 = (DisplacementMap[index + TextureResolution].y + Height)/ Height;
                h22 = (DisplacementMap[index + TextureResolution + 1].y + Height) / Height;
                gX = h00 - h20 + 2.0f * h01 - 2.0f * h21 + h02 - h22;
                gY = h00 + 2.0f * h10 + h20 - h02 - 2.0f * h12 - h22;
                gZ = 0.5f * Mathf.Sqrt(1.0f - gX * gX - gY * gY);
                temp = Vector3.Normalize(new Vector3(gX, gY, gZ));
                Normal[index] = new Color(temp.x, temp.y, temp.z);
            }
        }
        NormalMap.SetPixels(Normal);
        NormalMap.Apply();
        DebugNormal.SetPixels(Normal);
        DebugNormal.Apply();
        material.SetTexture("_BumpMap", NormalMap);
    }

    void GenerateColorFromHeight()
    {
        for (int x = 0; x < TextureResolution; x++)
        {
            for (int y = 0; y < TextureResolution; y++)
            {
                int index = y * TextureResolution + x;
				float rampValue = (DisplacementMap[index].y + Wind.sqrMagnitude/g)/(Wind.sqrMagnitude/g);
                Diffuse[index].r = Mathf.Lerp(DeepWaterColor.r, ShallowWaterColor.r, rampValue);
                Diffuse[index].g = Mathf.Lerp(DeepWaterColor.g, ShallowWaterColor.g, rampValue);
                Diffuse[index].b = Mathf.Lerp(DeepWaterColor.b, ShallowWaterColor.b, rampValue);
            }
        }
        //Debug.Log((DisplacementMap[0].y + Height) / (2 * Height));
        DiffuseMap.SetPixels(Diffuse);
        DiffuseMap.Apply();
        material.SetTexture("_MainTex", DiffuseMap);
    }

    void GenerateTransparencyFromHeight()
    {
        for (int x = 0; x < TextureResolution; x++)
        {
            for (int y = 0; y < TextureResolution; y++)
            {
                int index = y * TextureResolution + x;
                float rampValue = (DisplacementMap[index].y + Height) / (Height)*0.5f;
                Transparency[index].r = Mathf.Lerp(Color.white.r, Color.black.r, rampValue);
                Transparency[index].g = Mathf.Lerp(Color.white.g, Color.black.g, rampValue);
                Transparency[index].b = Mathf.Lerp(Color.white.b, Color.black.b, rampValue);
                Transparency[index].a = Mathf.Lerp(Color.white.r, Color.black.r, rampValue);
            }
        }
        TransparencyMap.SetPixels(Transparency);
        TransparencyMap.Apply();
        DebugTransparency.SetPixels(Transparency);
        DebugTransparency.Apply();
        material.SetTexture("_transMap", TransparencyMap);
    }

    void ApplyHeightMap()
    {
        int increment = (TextureResolution-1) / (LengthWidth-1); // Distance in Texels between verts
        int x = 0, y = 0, index = 0;
        int x1 = 0, y1 = 0, index1 = 0;
        // Corners
        Vertices[index].x = (x + DisplacementMap[index1].x) * PlaneScale;       // lower left
        Vertices[index].y = DisplacementMap[index1].y;
        Vertices[index].z = (y + DisplacementMap[index1].z) * PlaneScale;
        
        x = LengthWidth - 1;                             // right edge of plane
        x1 = TextureResolution - 1;                      // right edge of map
        index = LengthWidth * y + x;                     // set up index
        index1 = TextureResolution * y1 + x1;            // set up index1
        Vertices[index].x = (x + DisplacementMap[index1].x) * PlaneScale;       // lower right
        Vertices[index].y = DisplacementMap[index1].y;
        Vertices[index].z = (y + DisplacementMap[index1].z) * PlaneScale;

        y = LengthWidth - 1;                             // upper edge of plane
        y1 = TextureResolution - 1;                      // upper edge of map
        index = LengthWidth * y + x;                     // set up index
        index1 = TextureResolution * y1 + x1;            // set up index1
        Vertices[index].x = (x + DisplacementMap[index1].x) * PlaneScale;       // Upper Right
        Vertices[index].y = DisplacementMap[index1].y;
        Vertices[index].z = (y + DisplacementMap[index1].z) * PlaneScale;
        
        x = 0;                                           // left edge of plane
        x1 = 0;                                          // left edge of map
        index = LengthWidth * y + x;                     // set up index
        index1 = TextureResolution * y1 + x1;            // set up index1
        Vertices[index].x = (x + DisplacementMap[index1].x) * PlaneScale;       // Upper left
        Vertices[index].y = DisplacementMap[index1].y;
        Vertices[index].z = (y + DisplacementMap[index1].z) * PlaneScale;

        // edges
        x = 0;                                           // working on left edge
        x1 = 0;
        for (y = 1; y < LengthWidth; y++)
        {
            y1 = y*increment;
            index = LengthWidth * y + x;                 // set up index
            index1 = TextureResolution * y1 + x1;        // set up index1
            Vertices[index].x = (x + DisplacementMap[index1].x) * PlaneScale;
            Vertices[index].y = DisplacementMap[index1].y;
            Vertices[index].z = (y + DisplacementMap[index1].z) * PlaneScale;
        }
        x = LengthWidth - 1;                             // working on right edge
        x1 = TextureResolution - 1;
        for (y = 1; y < LengthWidth; y++)
        {
            y1 = y*increment;
            index = LengthWidth * y + x;                 // set up index
            index1 = TextureResolution * y1 + x1;        // set up index1
            Vertices[index].x = (x + DisplacementMap[index1].x) * PlaneScale;
            Vertices[index].y = DisplacementMap[index1].y;
            Vertices[index].z = (y + DisplacementMap[index1].z) * PlaneScale;
        }

        y = 0;                                           // working on bottom edge
        y1 = 0;
        for (x = 1; x < LengthWidth; x++)
        {
            x1 = x*increment;
            index = LengthWidth * y + x;                 // set up index
            index1 = TextureResolution * y1 + x1;        // set up index1
            Vertices[index].x = (x + DisplacementMap[index1].x) * PlaneScale;
            Vertices[index].y = DisplacementMap[index1].y;
            Vertices[index].z = (y + DisplacementMap[index1].z) * PlaneScale;
        }
        y = LengthWidth - 1;                             // working on top edge
        y1 = TextureResolution - 1;
        for (x = 1; x < LengthWidth; x++)
        {
            x1 = x * increment;
            index = LengthWidth * y + x;                 // set up index
            index1 = TextureResolution * y1 + x1;        // set up index1
            Vertices[index].x = (x + DisplacementMap[index1].x) * PlaneScale;
            Vertices[index].y = DisplacementMap[index1].y;
            Vertices[index].z = (y + DisplacementMap[index1].z) * PlaneScale;
        }

        for (x = 1; x < LengthWidth; x++)
        {
            for (y = 1; y < LengthWidth; y++)
            {
                x1 = x * increment;
                y1 = y * increment;
                index = LengthWidth * y + x;                 // set up index
                index1 = TextureResolution * y1 + x1;        // set up index1
                Vertices[index].x = (x + DisplacementMap[index1].x) * PlaneScale;
                Vertices[index].y = DisplacementMap[index1].y;
                Vertices[index].z = (y + DisplacementMap[index1].z) * PlaneScale;
            }
        }

		for(int i = 0; i < TextureResolution; i++)
		{
            Vector3 v = DisplacementMap[i].normalized;
            Displacement[i] = new Color(v.x, v.y, v.z);
		}

        DebugDisplacement.SetPixels(Displacement);
        DebugDisplacement.Apply();

        mesh.vertices = Vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        CalculateTangents();
        meshC.enabled = false;
        meshC.enabled = true;
    }

    float GaussianRnd()
    {
        //Debug.Log("GaussianRnd Called");
        float x1 = Random.value;
        float x2 = Random.value;
        
        if (x1 <= 0.01f)
            x1 = 0.01f;
        float res = (float)(System.Math.Sqrt(-2.0f * System.Math.Log(x1)) * Mathf.Cos(2.0f * Mathf.PI * x2));
		return res;
    }

    float Dispersion(int n_prime, int m_prime)
    {
        //Debug.Log("Dispersion Called");
        float w_0 = 2.0f * Mathf.PI / 200.0f;		// Dispersion is 2pi/t where t is the loop time.
        float kx = Mathf.PI * (2 * n_prime - TextureResolution) / OceanSize;
        float kz = Mathf.PI * (2 * m_prime - TextureResolution) / OceanSize;
        float res = Mathf.Floor(Mathf.Sqrt(g * Mathf.Sqrt(kx * kx + kz * kz)) / w_0) * w_0;
		return res;
    }

    float Phillips(int n_prime, int m_prime)
    {
        
        //Debug.Log("Phillips Called");
		float kx = Mathf.PI * (2 * n_prime - TextureResolution) / OceanSize;
		float kz = Mathf.PI * (2 * m_prime - TextureResolution) / OceanSize;
        Vector2 k = new Vector2(kx, kz);
		//if (n_prime == 0 && m_prime == 0)
			//Debug.Log(n_prime+" "+m_prime+" "+k);
        //float k_length = k.magnitude;
        //if (k_length < 0.0001f) return 0;
        //float k_length2 = k_length * k_length;
        //float k_length4 = k_length2 * k_length2;
        //if (k_length2 < 0.0001f || k_length4 < 0.0001f) return 0;
		//!!!!//or2 _Wind = new Vector2(Wind.x, 0);
        //float k_dot_w = Vector2.Dot(k.normalized, Wind.normalized);
        //float k_dot_w2 = k_dot_w * k_dot_w;
        //float w_length = Wind.magnitude;
        float L = Wind.sqrMagnitude / g;
        //float L2 = L * L;
        //float damping = 0.001f;
        //float l2 = L2 * damping *damping;
        //float res = (float)(Height * System.Math.Exp(-1.0f / (k_length2 * L2)) / k_length4 * k_dot_w2 * System.Math.Exp(-k_length2 * l2));

		if (k.magnitude < 0.0001f)
						return 0;
		if (k.sqrMagnitude < 0.0001f)
						return 0;
		float l2 = Mathf.Pow (L, 2) * .0000001f;
		double dampval = System.Math.Exp (-1 * k.sqrMagnitude * l2);
		float res = Height * (float)(System.Math.Exp (-1.0f / System.Math.Pow ((k.magnitude * L), 2)) / System.Math.Pow (k.magnitude, 4) * System.Math.Pow((Vector2.Dot (k.normalized, Wind.normalized)), 2)*dampval);

		//if (n_prime == 0 && m_prime == 0)
			//Debug.Log ("Result of Phillips Spectrum is: "+res);
		//if (res < 0.0001f)
		//	return 0;
		//else
		return res;
    }

    ComplexF hTilde_0(int n_prime, int m_prime)
    {
        //Debug.Log("hTilde_0 Called");
        ComplexF r = new ComplexF(GaussianRnd(), GaussianRnd());
        r *= (float)System.Math.Sqrt(Phillips(n_prime, m_prime) / 2.0f);
        //Debug.Log("htilde0(" + n_prime +", " +m_prime+") ="+ r);
		return r; //new ComplexF(1, 1);//r;
    }




    ComplexF hTilde(int n_prime, int m_prime)
    {
        //Debug.Log("hTilde Called");
        int index_negative = (TextureResolution-m_prime)%TextureResolution * TextureResolution + (TextureResolution-n_prime)%TextureResolution;
		int index = m_prime * TextureResolution + n_prime;

        float t = Time.time*Speed;

        //Debug.Log(ComplexDisplacementMap[index]);
        
		ComplexF hTilde0 = htilde0_initial[index];           //hTilde_0(n_prime, m_prime);
		ComplexF hTilde0conj = htilde0_initial[index_negative].GetConjugate();   //hTilde_0(-n_prime, -m_prime).GetConjugate();

        //ComplexF htilde0 = new ComplexF(ComplexDisplacementMap[index]);
        //ComplexF htilde0conj = new ComplexF(ComplexDisplacementMapConj[index].Re, ComplexDisplacementMapConj[index].Im);

        float omegat = Dispersion(n_prime, m_prime)*t;
        float sin_ = Mathf.Sin(omegat);
        float cos_ = Mathf.Cos(omegat);

        ComplexF c0 = new ComplexF(cos_, sin_);
        ComplexF c1 = new ComplexF(cos_, -sin_);

		ComplexF res = hTilde0 * c0 + hTilde0conj * c1;

        //Debug.Log(res);
        //Debug.Log("c0:" + c0 + " c1:" + c1 );
        //Debug.Log("" + res.Re + " + " + res.Im + "i");
		return res;
    }

    void EvaluateWaves()
    {
        //Debug.Log("Evaluating Waves at " + Time.time);
		float kx, kz, len;// lambda = -1.0f;
        int index;
        for (int m_prime = 0; m_prime < TextureResolution; m_prime++)
        {
            kz = Mathf.PI * (2 * m_prime - TextureResolution) / OceanSize;
            for (int n_prime = 0; n_prime < TextureResolution; n_prime++)
            {
                kx = Mathf.PI * (2 * n_prime - TextureResolution) / OceanSize;
                len = Mathf.Sqrt(kx * kx + kz * kz);
                index = m_prime * TextureResolution + n_prime;

                ComplexDisplacementMap[index] = hTilde(n_prime, m_prime);
                //Debug.Log(ComplexDisplacementMap[index]);
                SlopeX[index] = ComplexDisplacementMap[index] * new ComplexF(0, kx);
                SlopeZ[index] = ComplexDisplacementMap[index] * new ComplexF(0, kz);
                if (len < 0.000001f)
                {
                    DisplaceX[index] = new ComplexF(0, 0);
                    DisplaceZ[index] = new ComplexF(0, 0);
                }
                else
                {
                    DisplaceX[index] = ComplexDisplacementMap[index] * new ComplexF(0, -kx / len);
                    DisplaceZ[index] = ComplexDisplacementMap[index] * new ComplexF(0, -kz / len);
                }
            }
        }
        Debug.Log("Before FFT:" + ComplexDisplacementMap[0]);
        //Debug.Log("Evaluating FFT at " + Time.time);
        Fourier.FFT2(ComplexDisplacementMap, TextureResolution, TextureResolution, FourierDirection.Backward);
        Fourier.FFT2(SlopeX, TextureResolution, TextureResolution, FourierDirection.Backward);
        Fourier.FFT2(SlopeZ, TextureResolution, TextureResolution, FourierDirection.Backward);
        Fourier.FFT2(DisplaceX, TextureResolution, TextureResolution, FourierDirection.Backward);
        Fourier.FFT2(DisplaceZ, TextureResolution, TextureResolution, FourierDirection.Backward);
        //Debug.Log("FFT Complete, now assigning values to displace map at " + Time.time);
//        Debug.Log("After FFT:"+ComplexDisplacementMap[0]);
        ApplyComplexMapToHeightMap();
    }

    void ApplyComplexMapToHeightMap()
    {
        //Debug.Log(ComplexDisplacementMap[0]);
        int sign, index;
        float lambda = -1.0f;
        float[] signs = { 1.0f, -1.0f};
        for (int m_prime = 0; m_prime < TextureResolution; m_prime++)
        {
            for (int n_prime = 0; n_prime < TextureResolution; n_prime++)
            {
                index = m_prime * TextureResolution + n_prime;

                sign = (int)signs[(n_prime + m_prime) & 1];

                ComplexDisplacementMap[index] *= sign;
                DisplaceX[index] *= sign;
                DisplaceZ[index] *= sign;
                SlopeX[index] *= sign;
                SlopeZ[index] *= sign;
                float xoffset = n_prime % ((TextureResolution - 1) / (LengthWidth - 1));
                float zoffset = m_prime % ((TextureResolution - 1) / (LengthWidth - 1));
                DisplacementMap[index].y = ComplexDisplacementMap[index].Re *Height;
                DisplacementMap[index].x = xoffset * PlaneScale + DisplaceX[index].Re *lambda*Choppiness;
				DisplacementMap[index].z = zoffset * PlaneScale + DisplaceZ[index].Re *lambda*Choppiness;

                Vector3 n = new Vector3(0.0f - SlopeX[index].Re, 0.0f, 0.0f - SlopeZ[index].Re).normalized;
                Normal[index] = new Color(n.x, n.y, n.z);


                if (n_prime == 0 && m_prime == 0)
                {
                    float tempOffsetX = (TextureResolution-1) % ((TextureResolution - 1) / (LengthWidth - 1));
                    float tempOffsetZ = (TextureResolution-1) % ((TextureResolution - 1) / (LengthWidth - 1));
					DisplacementMap[index + TextureResolution-1 + (TextureResolution-1) * TextureResolution].y = ComplexDisplacementMap[index].Re*Height;
					DisplacementMap[index + TextureResolution - 1 + (TextureResolution - 1) * TextureResolution].x = tempOffsetX * PlaneScale + DisplaceX[index].Re * lambda*Choppiness;
					DisplacementMap[index + TextureResolution - 1 + (TextureResolution - 1) * TextureResolution].z = tempOffsetZ * PlaneScale + DisplaceZ[index].Re * lambda*Choppiness;

                    Normal[index + TextureResolution - 1 + (TextureResolution - 1) * TextureResolution] = new Color(n.x, n.y, n.z);
                }
                if (n_prime == 0)
                {
                    float tempOffsetX = (TextureResolution - 1) % ((TextureResolution - 1) / (LengthWidth - 1));
                    float tempOffsetZ = m_prime % ((TextureResolution - 1) / (LengthWidth - 1));
					DisplacementMap[index + TextureResolution - 1].y = ComplexDisplacementMap[index].Re*Height;
					DisplacementMap[index + TextureResolution - 1].x = tempOffsetX * PlaneScale + DisplaceX[index].Re*Choppiness;
					DisplacementMap[index + TextureResolution - 1].z = tempOffsetZ * PlaneScale + DisplaceZ[index].Re*Choppiness;

                    Normal[index + TextureResolution - 1] = new Color(n.x, n.y, n.z);

                }
                if (m_prime == 0)
                {
                    float tempOffsetX = n_prime % ((TextureResolution - 1) / (LengthWidth - 1));
                    float tempOffsetZ = (TextureResolution - 1) % ((TextureResolution - 1) / (LengthWidth - 1));
					DisplacementMap[index + (TextureResolution - 1) * TextureResolution].y = ComplexDisplacementMap[index].Re*Height;
					DisplacementMap[index + (TextureResolution - 1) * TextureResolution].x = tempOffsetX * PlaneScale + DisplaceX[index].Re*Choppiness;
					DisplacementMap[index + (TextureResolution - 1) * TextureResolution].z = tempOffsetZ * PlaneScale + DisplaceZ[index].Re*Choppiness;
                    Normal[index + (TextureResolution-1)*TextureResolution] = new Color(n.x, n.y, n.z);
                }
            }
        }

        //for (int x = 0; x < TextureResolution; x++)
        //{
        //    for (int z = 0; z < TextureResolution; z++)
        //    {
        //        index = z * TextureResolution + x;
        //        DisplacementMap[index].x = x;
		//		DisplacementMap[index].y = 0; // ComplexDisplacementMap[index].Re;
        //        DisplacementMap[index].z = z;
        //    }
        //}

        //Debug.Log("Complex Displacement Map applied to Displacement Map at " + Time.time);
        NormalMap.SetPixels(Normal);
        NormalMap.Apply();
        DebugNormal.SetPixels(Normal);
        DebugNormal.Apply();
        material.SetTexture("_BumpMap", NormalMap);
        GenerateColorFromHeight();
        GenerateTransparencyFromHeight();
        ApplyHeightMap();
        //GenerateNormalMap();
    }
}
