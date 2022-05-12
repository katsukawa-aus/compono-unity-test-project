using UnityEngine;
using UnityEngine.AddressableAssets;
using System.IO;
using System.Collections;

public class ExportRoutine : MonoBehaviour
{
    [SerializeField] private SpawnItemList m_itemList = null;

    private AssetReferenceGameObject m_assetLoadedAsset;
    private GameObject m_instanceObject = null;

    //output folder path
    private string outputFolderPath;

    //schreenshot parameters
    private Camera mainCamera;
    private float orthographicSizeModifier = 1f;
    private float screenshotGapRatio = 1.1f;
    private int screenshotSize = 512;
    private float rotationAngle = 22.5f;

    //status
    private bool isLoadingAsset = false;
    private bool isTakingScreenshot = false;
    private bool hasFinished = false;

    //reference of the model
    private UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> objReference;

    private void Start()
    {
        if (m_itemList == null || m_itemList.AssetReferenceCount == 0)
        {
            Debug.LogError("Spawn list not setup correctly");
        }

        //prepare the camera
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        orthographicSizeModifier = Screen.height / screenshotSize;

        //set a transparent background
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);

        //create output folder
        outputFolderPath = Application.dataPath + "/../Output";
        if (!Directory.Exists(outputFolderPath))
        {
            Directory.CreateDirectory(outputFolderPath);
        }

        //take screenshots of all models
        StartCoroutine(TakeAllScreenshot());
    }

    private void Update()
    {
        //exit playmode
        if (hasFinished)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }
    }

    IEnumerator TakeAllScreenshot()
    {
        for (int i = 0; i < m_itemList.AssetReferenceCount; i++)
        {
            //load the model
            isLoadingAsset = true;
            LoadItemAtIndex(m_itemList, i);
            yield return new WaitUntil(() => !isLoadingAsset);

            //calculate the size of the model to adjust camera parameters
            Bounds maxBounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (Renderer item in GetComponentsInChildren<Renderer>())
            {
                Bounds bounds = item.bounds;
                maxBounds.Encapsulate(bounds);
            }
            float maxBoundsLengthOn_XY_Plane = new Vector2(maxBounds.extents.x, maxBounds.extents.z).magnitude;
            float maxBoundsLengthOn_Y_Axis = maxBounds.extents.y;
            float maxBoundsLength = Mathf.Max(maxBoundsLengthOn_XY_Plane, maxBoundsLengthOn_Y_Axis);

            //adjust camera parameters
            mainCamera.transform.position = new Vector3(0f, maxBounds.center.y, mainCamera.transform.position.z);
            mainCamera.orthographicSize = maxBoundsLength * screenshotGapRatio * orthographicSizeModifier;

            //take screenshots
            isTakingScreenshot = true;
            StartCoroutine(TakeScreenshotSizeFixed());
            yield return new WaitUntil(() => !isTakingScreenshot);
        }

        hasFinished = true;
    }

    private void LoadItemAtIndex(SpawnItemList itemList, int index)
    {
        if (m_instanceObject != null)
        {
            Destroy(m_instanceObject);
        }

        m_assetLoadedAsset = itemList.GetAssetReferenceAtIndex(index);

        var spawnPosition = new Vector3();
        var spawnRotation = Quaternion.identity;
        var parentTransform = this.transform;

        var loadRoutine = m_assetLoadedAsset.LoadAssetAsync();
        loadRoutine.Completed += LoadRoutine_Completed;

        void LoadRoutine_Completed(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> obj)
        {
            m_instanceObject = Instantiate(obj.Result, spawnPosition, spawnRotation, parentTransform);

            //keep the reference of the model
            objReference = obj;

            isLoadingAsset = false;
        }
    }

    private void UnloadModelFromMemory()
    {
        //unload the model from memory
        Addressables.Release(objReference);
    }

    private IEnumerator TakeScreenshotSizeFixed()
    {
        //create the folder to save screenshots
        string folderPath = outputFolderPath + "/" + m_instanceObject.name.Substring(0, m_instanceObject.name.IndexOf("("));
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        //take 16 screenshots
        for (int i = 0; i < 16; i++)
        {
            int screenshotWidth = screenshotSize;
            int screenshotHeight = screenshotSize;

            if (i > 0)
            {
                m_instanceObject.transform.Rotate(0f, rotationAngle, 0f);
            }

            var texture = new Texture2D(screenshotWidth, screenshotHeight, TextureFormat.ARGB32, false);

            yield return new WaitForEndOfFrame();

            texture.ReadPixels(new Rect(Screen.width / 2 - screenshotWidth / 2, Screen.height / 2 - screenshotHeight / 2, screenshotWidth, screenshotHeight), 0, 0);
            texture.Apply();

            var bytes = texture.EncodeToPNG();
            Destroy(texture);

            string fileName = "frame" + i.ToString("D4") + ".png";
            File.WriteAllBytes(folderPath + "/" + fileName, bytes);

            yield return new WaitUntil(() => File.Exists(folderPath + "/" + fileName));
        }

        //release memory
        UnloadModelFromMemory();

        isTakingScreenshot = false;
    }
}
