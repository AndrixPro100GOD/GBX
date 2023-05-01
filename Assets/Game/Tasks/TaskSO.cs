using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tasks
{
    [CreateAssetMenu(fileName = "New task", menuName = "GBX/Task", order = 1)]
    public class TaskSO : ScriptableObject
    {
        [SerializeField] private TaskType _taskType;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private string _text;
        [SerializeField, ShowIf("_taskType", TaskType.Chill)]
        private float _time;

        public string Text => _text;
        public Sprite Sprite => _sprite;
        public TaskType TaskType => _taskType;
        
        public float Time => _time;
    }
}
