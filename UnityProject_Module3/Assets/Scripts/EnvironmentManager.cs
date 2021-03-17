using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Linq;

public class EnvironmentManager : MonoBehaviour
{
    
    #region Fields and properties

    VoxelGrid _voxelGrid;
    int _randomSeed = 666;

    bool _showVoids = true;

    Pix2Pix _pix2pix;

    #endregion

    #region Unity Standard Methods
    void Start()
    {
        Vector3Int gridSize = new Vector3Int(64, 10, 64);
        _voxelGrid = new VoxelGrid(gridSize, Vector3.zero, 1, parent: this.transform);

        Random.InitState(_randomSeed);

        _pix2pix = new Pix2Pix();
    }

    void Update()
    {
        DrawVoxels();

        if (Input.GetKeyDown(KeyCode.V))
        {
            _showVoids = !_showVoids;
        }
        
        if(Input.GetMouseButtonDown(0))
        {
            var voxel = SelectVoxel();
            
            if(voxel != null)
            {
                print(voxel.Index);
                _voxelGrid.CreateBlackBlob(voxel.Index, 10, picky: true, flat: true);//Set it to be 2D and irregular representing stone shapes

                PredictAndUpdate();
            }

        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            _voxelGrid.ClearGrid();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //Some code used in the progress of generating database
            //CreateRandomStones(3, 10, 20);//number of stones,minimun radius. max radius
            //populateRandomStonesAndSave(500, 2, 3, 10, 20);

            //var gridImage = _voxelGrid.ImageFromGrid();
            //var resized = ImageReadWrite.Resize256(gridImage, Color.gray);
            //_pix2pix.Predict(resized);

        }
    }

    #endregion

    #region Private Methods
    void PredictAndUpdate(bool allLayers = false)
    {
        _voxelGrid.ClearReds();

        int layerCount = 1;
        if (allLayers) layerCount = _voxelGrid.GridSize.y;

        for (int i = 0; i < layerCount; i++)
        {
            var gridImage = _voxelGrid.ImageFromGrid(layer: i);

            ImageReadWrite.Resize256(gridImage, Color.grey);

            var predicted = _pix2pix.Predict(gridImage);

            TextureScale.Point(predicted, _voxelGrid.GridSize.x, _voxelGrid.GridSize.z);

            _voxelGrid.SetStatesFromImage(predicted, layer: i);
        }
    }
    void populateRandomStonesAndSave(int sampleSize,int minAmt,int maxAmt,int minRadius, int maxRadius)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        string saveFolder = "Output";

        for (int i = 0; i < sampleSize; i++)
        {
            int amt = Random.Range(minAmt, maxAmt);

            _voxelGrid.ClearGrid();

            CreateRandomStones(amt, minRadius, maxRadius, true);

            Texture2D gridImage = _voxelGrid.ImageFromGrid(transparent: true);

            Texture2D resizedImage = ImageReadWrite.Resize256(gridImage, Color.grey);

            ImageReadWrite.SaveImage(resizedImage, $"{saveFolder}/Grid_{i}");
        }
        stopwatch.Stop();
        print($"Took{stopwatch.ElapsedMilliseconds} milliseconds to generate{sampleSize} images");
    }

    void CreateRandomStones(int amt, int minRadius, int maxRadius, bool picky = true )
    {
        for (int i = 0; i < amt; i++)
        {
            bool success = false;
            while(!success)
            {
                float rand = Random.value;

                int x;
                int z;


                //Instead of keep the generated stones/blobs on the side,set it to random coordinates
                //if (rand < 0.5f)
                //{
                //    x = Random.value < 0.5f ? 0 : _voxelGrid.GridSize.x - 1;
                //    z = Random.Range(0, _voxelGrid.GridSize.z);
                //}
                //else
                //{
                //    z = Random.value < 0.5f ? 0 : _voxelGrid.GridSize.z - 1;
                //    x = Random.Range(0, _voxelGrid.GridSize.x);
                //}

                x = Random.Range(0, _voxelGrid.GridSize.x);
                z = Random.Range(0, _voxelGrid.GridSize.z);

                Vector3Int origin = new Vector3Int(x, 0, z);
                int radius = Random.Range(minRadius, maxRadius);

                success = _voxelGrid.CreateBlackBlob(origin, radius, picky);
            }
        }
    }

    Voxel SelectVoxel()
    {
        Voxel selected = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            Transform objectHit = hit.transform;

            if (objectHit.CompareTag("Voxel"))
            {
                string voxelName = objectHit.name;
                var index = voxelName.Split('_').Select(v => int.Parse(v)).ToArray();

                selected = _voxelGrid.Voxels[index[0], index[1], index[2]];
            }
        }
        return selected;
    }

    /// <summary>
    /// Draws the voxels according to it's state and Function Corlor
    /// </summary>
    void DrawVoxels()
    {
        foreach (var voxel in _voxelGrid.Voxels)
        {
            if (voxel.IsActive)
            {
                Vector3 pos = (Vector3)voxel.Index * _voxelGrid.VoxelSize + transform.position;
                if (voxel.FColor    ==   FunctionColor.Black)   Drawing.DrawCube(pos, _voxelGrid.VoxelSize, Color.black);
                else if (voxel.FColor == FunctionColor.Red)     Drawing.DrawCube(pos, _voxelGrid.VoxelSize, Color.red);
                else if (voxel.FColor == FunctionColor.Yellow)  Drawing.DrawCube(pos, _voxelGrid.VoxelSize, Color.yellow);
                else if (voxel.FColor == FunctionColor.Green)   Drawing.DrawCube(pos, _voxelGrid.VoxelSize, Color.green);
                else if (voxel.FColor == FunctionColor.Cyan)    Drawing.DrawCube(pos, _voxelGrid.VoxelSize, Color.cyan);
                else if (voxel.FColor == FunctionColor.Magenta) Drawing.DrawCube(pos, _voxelGrid.VoxelSize, Color.magenta);
                else if (_showVoids && voxel.Index.y == 0)
                    Drawing.DrawTransparentCube(pos, _voxelGrid.VoxelSize);
            }
        }
    }

    #endregion
}
