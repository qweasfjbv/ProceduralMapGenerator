using UnityEngine;

namespace DungeonGenerator
{
    public class Visualization : MonoBehaviour
    {
        #region Singleton
        static Visualization s_instance;
        public static Visualization Instance { get { return s_instance; } }


        void Awake()
        {
            Init();
        }

        public void Init()
        {
            if (s_instance == null)
            {
                s_instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                if (s_instance != this)
                    Destroy(this.gameObject);
            }

        }
        #endregion

        [SerializeField] private GameObject linePrefab;

        public GameObject SpawnLineRenderer(Vector3 startPos, Vector3 endPos, float width, Color color)
        {
            GameObject go = Instantiate(linePrefab);
            LineRenderer lineRenderer = go.GetComponentInParent<LineRenderer>();

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.sortingOrder = 20;

            return go;
        }

    }

}
