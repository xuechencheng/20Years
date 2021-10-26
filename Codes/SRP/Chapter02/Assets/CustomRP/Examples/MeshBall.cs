﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 生成1023个mesh和小球对象
/// </summary>
public class MeshBall : MonoBehaviour
{
    static int baseColorId = Shader.PropertyToID("_BaseColor");
    [SerializeField]
    Mesh mesh = default;
    [SerializeField]
    Material material = default;
    Matrix4x4[] matrices = new Matrix4x4[1023];
    Vector4[] baseColors = new Vector4[1023];
    MaterialPropertyBlock block;
    void Awake()
    {
        for (int i=0;i<matrices.Length;i++)
        {
            //创建随机转换矩阵和颜色
            matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 10f, Quaternion.Euler(Random.value * 360f, Random.value * 360f, Random.value * 360f), Vector3.one * Random.Range(0.5f, 1.5f));
            baseColors[i] = new Vector4(Random.value, Random.value, Random.value, Random.Range(0.5f, 1f));
        }
    }

    void Update()
    {
        if (block == null)
        {
            //随机属性发送到着色器
            block = new MaterialPropertyBlock();
            block.SetVectorArray( baseColorId, baseColors);
        }    
		 //绘制网格实例
        Graphics.DrawMeshInstanced( mesh, 0, material, matrices, 1023, block);
    }
}
