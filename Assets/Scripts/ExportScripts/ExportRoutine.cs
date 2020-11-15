using UnityEngine;
using UnityEngine.AddressableAssets;

public class ExportRoutine : MonoBehaviour
{   
    [SerializeField] private SpawnItemList m_itemList = null;

    private AssetReferenceGameObject m_assetLoadedAsset;
    private GameObject m_instanceObject = null;

    private void Start()
    {
        if (m_itemList == null || m_itemList.AssetReferenceCount == 0) 
        {
            Debug.LogError("Spawn list not setup correctly");
        }        
        LoadItemAtIndex(m_itemList, 0);
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
        }
    }
}
