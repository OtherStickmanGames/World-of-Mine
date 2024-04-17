using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BuildGenerator;

public class World : MonoBehaviour
{
	[SerializeField] List<CollideredBlockData> collideredBlocks;
	[SerializeField] TextAsset towerAsset;
	[SerializeField] public Transform towerPos;

	public static World Instance;
	public BoxCollider towerZone;
	public List<Vector3> notMineable = new List<Vector3>();
	public List<Vector3> notAvailable = new List<Vector3>();
	public Dictionary<Vector3, int> countNotAvailables = new Dictionary<Vector3, int>();

	Tower tower;
	BuildedData towerData;
	WorldGenerator worldGenerator;
	BlockItemSpawner blockItemSpawner;
	List<IUpdateble> updatables = new();
	List<ChunckComponent> chuncksBeUpdate = new List<ChunckComponent>();
	bool towerPlaced;

    private void Awake()
    {
		Instance = this;
		
		WorldGenerator.onReady.AddListener(Generator_Ready);
    }

    private void Start()
    {
		updatables.Add(new BlockItemSpawner());
		//updatables.Add(new FindPathSystem());

		StartCoroutine(CheckEmptyNotAvailable());
    }

    private void Update()
    {
        foreach (var item in updatables) item.Update();

		if (Input.GetKeyDown(KeyCode.P))
			PrepareTowerPlace();


		if (Input.GetKeyDown(KeyCode.Q))
        {
			Application.Quit();
        }

		CheckLifetimeNotAvailableList();
	}

	float notAvailableLifetime;
	void CheckLifetimeNotAvailableList()
    {
		notAvailableLifetime += Time.deltaTime;

		if (notAvailableLifetime > 300)
        {
			notAvailableLifetime = 0;
			notAvailable.Clear();
        }
    }

	private void Generator_Ready()
	{
		StartCoroutine(AsyncDelay());

		IEnumerator AsyncDelay()
		{
			yield return null;

			worldGenerator = WorldGenerator.Inst;

			var size = WorldGenerator.size;
			int dist = 3 * size;
			for (int x = -dist; x <= dist; x += size)
			{
				for (int y = size; y <= dist; y += size)
				{
					for (int z = -dist; z <= dist; z += size)
					{
						var checkingPos = new Vector3(x, y, z);
						var chunck = worldGenerator.GetChunk(checkingPos, out var chunckKey);

						if (chunck == null)
						{
							chunckKey *= size;
							worldGenerator.CreateChunck(chunckKey.x, chunckKey.y, chunckKey.z);

							//yield return null;
						}
					}
				}
			}

			PrepareTowerPlace();
			BuildTower();
		}
	}

	private void BuildTower()
    {
		var pos = towerPos.position;
		towerData = BuildGenerator.Build(towerAsset, pos, true);
		notMineable.AddRange(towerData.globalBlockPoses);
		tower = new Tower(towerData.triiger);
	}

	public void AddNotAvailable(Vector3 globalBlockPos)
	{
		notAvailable.Add(globalBlockPos);

		if (countNotAvailables.Any(pair => pair.Key == globalBlockPos))
		{
			countNotAvailables[globalBlockPos]++;
			//print(countNotAvailables[globalBlockPos]);
			if (countNotAvailables[globalBlockPos] > 10)
			{
				WorldGenerator.Inst.SetBlockAndUpdateChunck(globalBlockPos, 0);
				print("--= удалил блок =--");
			}
        }
        else
        {
			countNotAvailables.Add(globalBlockPos, 1);
        }
	}

	private void PrepareTowerPlace()
	{
		if (towerPlaced)
			return;

		if (WorldGenerator.Inst.chuncks.Count > 25)
		{
			towerPlaced = true;

            foreach (var data in collideredBlocks)
            {
				SetBlocksByCollider(data.collider, data.blockID);
			}
		}
	}

    Collider[] results = new Collider[30];

	private void SetBlocksByCollider(Collider collider, byte blockID)
	{
		var blockGlobalPos = Vector3.zero;
		var min = collider.bounds.min;
		var max = collider.bounds.max;
		for (float x = min.x; x < max.x; x++)
		{
			for (float y = min.y; y < max.y; y++)
			{
				for (float z = min.z; z < max.z; z++)
				{
					blockGlobalPos.x = x;
					blockGlobalPos.y = y;
					blockGlobalPos.z = z;

					int countHits = Physics.OverlapSphereNonAlloc(blockGlobalPos, 1, results);
					for (int i = 0; i < countHits; i++)
					{
						var hit = results[i];

						if (hit == collider)
						{
							if(blockID != 0)
                            {
								notMineable.Add(blockGlobalPos.ToGlobalBlockPos());
                            }
							var chunck = WorldGenerator.Inst.SetBlock(blockGlobalPos, blockID);
							if (!chuncksBeUpdate.Contains(chunck))
							{
								chuncksBeUpdate.Add(chunck);
							}
						}
					}
				}
			}
		}

		foreach (var item in chuncksBeUpdate)
		{
			WorldGenerator.Inst.UpdateChunckMesh(item);
		}
		chuncksBeUpdate.Clear();
		Destroy(collider.gameObject);
	}

	[Space(18)] public List<Vector3> keysForRemove = new List<Vector3>();
	public List<Vector3> convertedKeys = new List<Vector3>();
	IEnumerator CheckEmptyNotAvailable()
	{
		while (true)
		{
			convertedKeys.AddRange(countNotAvailables.Select(p => p.Key).ToList());
			foreach (var key in convertedKeys)
			{
				yield return null;

				var blockID = WorldGenerator.Inst.GetBlockID(key);

				if (blockID == 0)
				{
					keysForRemove.Add(key);
				}
			}

			convertedKeys.Clear();

			foreach (var key in keysForRemove)
			{
				yield return null;

				countNotAvailables.Remove(key);
			}

			yield return null;

			keysForRemove.Clear();
		}
	}

	public static readonly Vector3[] faceChecks = new Vector3[6]
	{
		new Vector3( 0.0f, 0.0f,-1.0f),
		new Vector3( 0.0f, 0.0f, 1.0f),
		new Vector3( 0.0f, 1.0f, 0.0f),
		new Vector3( 0.0f,-1.0f, 0.0f),
		new Vector3(-1.0f, 0.0f, 0.0f),
		new Vector3( 1.0f, 0.0f, 0.0f),
	};

	[System.Serializable]
	public class CollideredBlockData
    {
		public Collider collider;
		public byte blockID;
    }
}
