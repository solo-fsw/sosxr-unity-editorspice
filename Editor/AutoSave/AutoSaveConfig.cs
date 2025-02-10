using UnityEngine;


namespace Tarodev
{
    [CreateAssetMenu(fileName = "AutoSaveConfig", menuName = "SOSXR/AutoSaveConfig")]
    public class AutoSaveConfig : ScriptableObject
    {
        private void Awake()
        {
            Debug.Log("Only one of these is needed");
        }

        [Tooltip("Enable auto save functionality")]
        public bool Enabled;

        [Tooltip("The frequency in minutes auto save will activate")] [Min(1)]
        public int Frequency = 1;

        [Tooltip("Log a message every time the scene is auto saved")]
        public bool Logging;
    }
}