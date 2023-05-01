using UnityEngine;

namespace Game.Tasks
{
    [CreateAssetMenu(fileName = "New task list", menuName = "GBX/Task list", order = 1)]
    public class TaskListSO : ScriptableObject
    {
        public TaskSO[] Tasks;
    }
}
