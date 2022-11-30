using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Voxelization.Utils;

namespace Voxelization.Tools
{
    public class VoxelizerEditorWindow : EditorWindow
    {
        private static readonly string appName = "Voxelizer tool";

        private readonly string[] staticTypesLabels = new string[] 
        {
            "Voxelize",
            "Voxelize to single object",
            "Voxelize to primitives"
        };
        private readonly string[] animatedTypesLabels = new string[]
        {
            "Voxelize"
        };
        private readonly string[] unitsLabels = new string[]
        {
            "Voxel size",
            "Subdivision level"
        };
        private readonly string[] procLabels = new string[]
        {
            "CPU single-thread",
            "CPU multi-thread",
            // "GPU" Not implemented yet (future updates)
        };

        private readonly string ERROR_NO_GAMEOBJECT = "Select gameObject to voxelize";
        private readonly string ERROR_NO_MESHES = "No meshes found. Try to select other Mesh type";

        private readonly string WARNING_LONG_TIME = "Processing time may be long";
        private readonly string WARNING_SMALL_VOXEL_SIZE = "Small voxel size values (voxelSize < 0.2) are unsafe - processing time may be EXTREMELY long";

        private readonly string INFO_VERTS_NUMBER = "Total mesh vertices: {0}";
        private readonly string INFO_VOXELIZED_VERTS_NUMBER = "Result mesh [voxels: {0}], [vertices: {1}]";

        // Selectable 
        private int selectedTypeIndex = 0;
        private VoxelizationType voxelizationType;

        private int selectedUnitsIndex = 0;
        private VoxelizeUnits meshUnits = VoxelizeUnits.Subdivision_level;

        private int selectedProcIndex = 0;
        private ProcessingType processingType;

        private GameObject targetGameObject;
        private MeshType rendererType;

        private float voxelSize = 1f;
        private int subDivisionLevel = 1;

        private bool removeNeighbouringFaces;
        private bool simplify;   // Not implemented yet (future updates)
        private bool fillInside; // Not implemented yet (future updates)

        private float voxelScaleMult = 1f;
        private Mesh voxelCustomMesh;

        // private 
        private bool isAnyError = false;

        private Voxelizer voxelizer = null;
        private GameObject voxelizedObject = null;

        private int voxelizedVerticesNum = 0;
        private int voxelsNum = 0;
        private bool isLogoVisible = false;
        private bool isModifyOpen = false;
        private Vector2 scrollPos;


        [MenuItem("Window/SimpleMeshVoxelizer")]
        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow(typeof(VoxelizerEditorWindow), true, appName);

            window.maxSize = new Vector2(400f, 700f);
            window.minSize = new Vector2(320f, 580f);
        }

        void OnGUI()
        {
            isAnyError = false;

            HeaderPart();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            (Vector3 max, Vector3 min, int vertsNum) = SelectMeshPart();

            GUIStyle activeControls = VoxelizerEditorStyles.GetActiveControlsBackground();

            SetVoxelSizePart(activeControls, max, min, vertsNum);

            ConfigurationPart();
            VoxelizerEditorUtils.HorizontalLine(Color.gray);

            ModifyVoxelsPart(activeControls);
            VoxelizerEditorUtils.HorizontalLine(Color.gray);

            ProcessingPart();
            GUILayout.Space(10f);

            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            VoxelizeButtonPart(max, min);
        }

        private void HeaderPart()
        {
            GUIStyle tinyButtonStyle = VoxelizerEditorStyles.GetTinyButtonStyle(10, 2);
            Rect outter = new Rect(Vector3.zero, new Vector2(position.width, 12));
            VoxelizerEditorUtils.DrawUIBox(Color.black, Color.white, outter, 1);

            if (GUILayout.Button("Toggle Logo Visibility", tinyButtonStyle, GUILayout.Height(10f)))
            {
                isLogoVisible = !isLogoVisible;
            }

            if (isLogoVisible)
            {
                PlaceLogo();
            }
            else
            {
                GUILayout.Space(15f);
            }
        }

        private (Vector3, Vector3, int) SelectMeshPart()
        {
            targetGameObject = (GameObject)EditorGUILayout.ObjectField("GameObject to voxelize: ", targetGameObject, typeof(GameObject), true);
            if (targetGameObject == null)
            {
                isAnyError = VoxelizerEditorUtils.PlaceErrorBox(ERROR_NO_GAMEOBJECT, isAnyError);
            }

            rendererType = (MeshType)EditorGUILayout.EnumPopup("Mesh type: ", rendererType);

            Vector3 min;
            Vector3 max;
            int vertsNum = GetVertsNum(targetGameObject, rendererType, out min, out max);
            if (vertsNum == 0)
            {
                isAnyError = VoxelizerEditorUtils.PlaceErrorBox(ERROR_NO_MESHES, isAnyError);
            }
            else
            {
                VoxelizerEditorUtils.PlaceHelpBox(string.Format(INFO_VERTS_NUMBER, vertsNum), MessageType.Info);
            }
            return (max, min, vertsNum);
        }

        private void SetVoxelSizePart(GUIStyle style, Vector3 max, Vector3 min, int vertsNum)
        {
            GUILayout.BeginVertical(style);

            selectedUnitsIndex = EditorGUILayout.Popup("Units: ", selectedUnitsIndex, unitsLabels);
            meshUnits = Enum.GetValues(typeof(VoxelizeUnits)).Cast<VoxelizeUnits>().ToList()[selectedUnitsIndex];

            UpdateVoxelSize(max, min, vertsNum);

            string[] labelsList;
            if (rendererType == MeshType.Static)
            {
                labelsList = staticTypesLabels;
            }
            else
            {
                labelsList = animatedTypesLabels;
            }

            GUILayout.EndHorizontal();


            selectedTypeIndex = EditorGUILayout.Popup("Voxelization type: ", selectedTypeIndex, labelsList);
            voxelizationType = Enum.GetValues(typeof(VoxelizationType)).Cast<VoxelizationType>().ToList()[selectedTypeIndex];

            selectedTypeIndex %= labelsList.Length;
        }

        private void ConfigurationPart()
        {
            if (voxelizationType == VoxelizationType.Create_Voxelized_Mesh_With_Primitives)
            {
                if (voxelSize < 0.5f)
                {
                    VoxelizerEditorUtils.PlaceHelpBox(WARNING_SMALL_VOXEL_SIZE, MessageType.Warning);
                }
            }
            else
            {
                if (!isModifyOpen)
                {
                    removeNeighbouringFaces = EditorGUILayout.Toggle(new GUIContent("Remove neigbouring faces: ", "Reduce the size of the final Mesh (by removing unseen voxel faces)"), removeNeighbouringFaces);
                }
            }

            // Not implemented yet (future updates)
            GUI.enabled = false;
            //if (!isModifyOpen)
            //{
            //    simplify = EditorGUILayout.Toggle("SimplifyResult: ", simplify);
            //}
            //fillInside = EditorGUILayout.Toggle("Fill voxels inside model: ", fillInside);
            GUI.enabled = true;
        }

        private void ModifyVoxelsPart(GUIStyle style)
        {
            GUIStyle fontStyle = GUI.skin.toggle;
            if (isModifyOpen)
            {
                fontStyle.fontStyle = FontStyle.Bold;
            }
            else
            {
                fontStyle.fontStyle = FontStyle.Normal;
            }

            isModifyOpen = GUILayout.Toggle(isModifyOpen, "Modify voxels", fontStyle);

            if (isModifyOpen)
            {
                GUILayout.BeginVertical(style);

                voxelScaleMult = EditorGUILayout.Slider("Voxel scale: ", voxelScaleMult, 0.001f, 1);
                voxelCustomMesh = (Mesh)EditorGUILayout.ObjectField("Voxel: ", voxelCustomMesh, typeof(Mesh));

                GUILayout.EndHorizontal();
            }
        }

        private void ProcessingPart()
        {
            selectedProcIndex = EditorGUILayout.Popup("Processing type: ", selectedProcIndex, procLabels);
            processingType = Enum.GetValues(typeof(ProcessingType)).Cast<ProcessingType>().ToList()[selectedProcIndex];

            if (processingType == ProcessingType.Multi_thread)
            {
                VoxelizerEditorUtils.PlaceHelpBox(string.Format("ETA speed up: {0}x", SystemInfo.processorCount - 1), MessageType.Info);
            }
        }

        private void VoxelizeButtonPart(Vector3 max, Vector3 min)
        {
            VoxelizerEditorUtils.HorizontalLine(Color.gray);

            var prevColor = GUI.backgroundColor;
            if (!isAnyError)
            {
                GUI.backgroundColor = VoxelizerEditorUtils.GetColorFromHEX("#94eff2");
                var size = (max - min) / voxelSize;
                var sizeStr = string.Format("Voxels [X: {0}] [Y: {1}] [Z: {2}]", Mathf.CeilToInt(size.x), Mathf.CeilToInt(size.y), Mathf.CeilToInt(size.z));
                EditorGUILayout.HelpBox(sizeStr, MessageType.Info);

                if (voxelizationType != VoxelizationType.Create_Voxelized_Mesh_With_Primitives)
                {
                    if (voxelizedObject != null && voxelizer != null && voxelizer.Trees.Count != 0)
                    {
                        voxelizedVerticesNum = GetVertsNum(voxelizedObject, rendererType, out min, out max);
                        voxelsNum = GetVoxelsNum(voxelizer);

                        VoxelizerEditorUtils.PlaceHelpBox(string.Format(INFO_VOXELIZED_VERTS_NUMBER, voxelsNum, voxelizedVerticesNum), MessageType.Info);
                    }
                }
            }

            EditorGUI.BeginDisabledGroup(isAnyError);
            if (GUILayout.Button("Voxelize mesh", GUILayout.Height(25f)))
            {
                voxelizer = Voxelize(targetGameObject, rendererType);
            }
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = prevColor;

            if (voxelizer != null && voxelizer.VoxelMeshes.Count != 0)
            {
                SaveMeshesAsAsset();
            }
        }

        private void SaveMeshesAsAsset()
        {
            if (voxelizedObject == null)
            {
                return;
            }

            if (GUILayout.Button("Save Voxel Meshes as .asset to folder", GUILayout.Height(20f)))
            {
                EditorUtility.DisplayProgressBar(appName, "Saving .asset...", 0f);
                EditorUtility.ClearProgressBar();

                string path = EditorUtility.OpenFolderPanel("Select save folder", "Assets/", "");
                if (string.IsNullOrEmpty(path)) return;

                for (int index = 0; index < voxelizer.VoxelMeshes.Count; index++)
                {
                    var mesh = voxelizer.VoxelMeshes[index];

                    var tmpPath = path + "/" + mesh.name + "_voxels.asset";
                    tmpPath = FileUtil.GetProjectRelativePath(tmpPath);
                    tmpPath = AssetDatabase.GenerateUniqueAssetPath(tmpPath);

                    AssetDatabase.CreateAsset(mesh, tmpPath);
                    AssetDatabase.SaveAssets();

                    var progress = (float)index / voxelizer.VoxelMeshes.Count;
                    var info = string.Format("Saving .asset... [{0}/{1}] {2:0.00}%", (index + 1), voxelizer.MeshesNum, progress * 100);
                    EditorUtility.DisplayProgressBar(appName, info, progress);
                }
                EditorUtility.DisplayProgressBar(appName, "Saving .asset...", 1f);
                EditorUtility.ClearProgressBar(); 
            }
        }

        private void PlaceLogo()
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();
            Texture2D Logo1Tex = (Texture2D)Resources.Load("Logo");
            GUILayout.Label(Logo1Tex, GUILayout.MaxWidth(155f), GUILayout.MaxHeight(150f));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            VoxelizerEditorUtils.HorizontalLine(Color.gray);
            GUILayout.Space(10f);
        }

        private void UpdateVoxelSize(Vector3 max, Vector3 min, int vertsNum)
        {
            var size = (max - min) / voxelSize;
            var estimatedVoxels = size.x * size.y * size.z / 8;

            if (meshUnits == VoxelizeUnits.Subdivision_level)
            {
                subDivisionLevel = EditorGUILayout.IntSlider(subDivisionLevel, 1, 1024);
                var len = max - min;
                float maxSide = len.x;
                maxSide = FastMathUtils.Max(len.y, maxSide);
                maxSide = FastMathUtils.Max(len.z, maxSide);
                voxelSize = maxSide / subDivisionLevel;

                GUIStyle boldStyle = GUI.skin.label;
                boldStyle.fontStyle = FontStyle.Bold;
                EditorGUILayout.LabelField(string.Format("Voxel size: {0:0.000}", voxelSize), boldStyle);
            }
            else
            {
                voxelSize = EditorGUILayout.FloatField("Voxel size: ", voxelSize);
            }

            if (targetGameObject != null)
            {
                if ((vertsNum < 100_000f && estimatedVoxels > 2_500_000) ||
                (vertsNum > 100_000f && estimatedVoxels > 1_000_000) ||
                (vertsNum > 250_000f && estimatedVoxels > 250_000))
                {
                    VoxelizerEditorUtils.PlaceHelpBox(WARNING_LONG_TIME, MessageType.Warning);
                }
            }
            
            if (voxelSize <= 0f)
            {
                voxelSize = 0.001f;
            }
        }

        private Voxelizer Voxelize(GameObject target, MeshType rendererType)
        {
            if (target == null)
            {
                this.ShowNotification(new GUIContent("No gameObject found, select one"));
                return null;
            }
            else if (voxelSize <= 0f)
            {
                this.ShowNotification(new GUIContent("Voxel size is zero"));
                return null;
            }
            EditorUtility.DisplayProgressBar(appName, "Voxelizing mesh...", 0f);

            float time = Time.realtimeSinceStartup;

            Voxelizer voxelizer = new Voxelizer(voxelSize, processingType);
            if (isModifyOpen)
            {
                voxelizer = new Voxelizer(voxelSize, processingType, voxelScaleMult, voxelCustomMesh);
            }

            voxelizer.OnFinishedModel += UpdateProgressBar;

            if (voxelizationType == VoxelizationType.Create_Voxelized_Copy)
            {
                voxelizedObject = voxelizer.Voxelize(target, removeNeighbouringFaces, rendererType);
            }
            else if (voxelizationType == VoxelizationType.Create_Voxelized_Mesh)
            {
                voxelizedObject = voxelizer.VoxelizeToSingleObject(target, removeNeighbouringFaces, rendererType);
            }
            else
            {
                voxelizer.VoxelizeToPrimitives(target, rendererType);
            }

            EditorUtility.DisplayProgressBar(appName, "Voxelizing mesh...", 1f);
            EditorUtility.ClearProgressBar();

            this.ShowNotification(new GUIContent(string.Format("GameObject voxelized :) [Elapsed time: {0:0.00}s]", Time.realtimeSinceStartup - time)), 3f);
            return voxelizer;
        }

        private void UpdateProgressBar(int index, Voxelizer voxelizer)
        {
            var progress = (float)(index + 1) / voxelizer.MeshesNum;
            var info = string.Format("Voxelizing mesh... [{0}/{1}] {2:0.00}%", (index + 1), voxelizer.MeshesNum, progress * 100);
            EditorUtility.DisplayProgressBar(appName, info, progress);
        }

        private int GetVertsNum(GameObject target, MeshType rendererType, out Vector3 min, out Vector3 max)
        {
            min = Vector3.positiveInfinity;
            max = Vector3.negativeInfinity;

            if (target == null)
            {
                return 0;
            }

            int vertsNum = 0;
            if (rendererType == MeshType.Static || rendererType == MeshType.All)
            {
                foreach (var filter in target.GetComponentsInChildren<MeshFilter>())
                {
                    if (filter.sharedMesh != null)
                    {
                        vertsNum += filter.sharedMesh.vertexCount;
                        min = FastMathUtils.Min(filter.sharedMesh.bounds.min, min);
                        max = FastMathUtils.Max(filter.sharedMesh.bounds.max, max);
                    }
                }
            }
            if (rendererType == MeshType.Animated || rendererType == MeshType.All)
            {
                foreach (var filter in target.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (filter.sharedMesh != null)
                    {
                        vertsNum += filter.sharedMesh.vertexCount;
                        min = FastMathUtils.Min(filter.sharedMesh.bounds.min, min);
                        max = FastMathUtils.Max(filter.sharedMesh.bounds.max, max);
                    }
                }
            }
            return vertsNum;
        }

        private int GetVoxelsNum(Voxelizer voxelizer)
        {
            if (voxelizer == null)
            {
                return 0;
            }

            int voxels = 0;

            foreach (var tree in voxelizer.Trees)
            {
                voxels += tree.Voxels.Count;
            }
            return voxels;
        }
    }
}
