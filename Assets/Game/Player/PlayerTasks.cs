using Game.Tasks;
using UnityEngine;
using Zenject;

namespace Game.Player
{
    public class PlayerTasks : MonoBehaviour
    {
        [Inject] private TasksSystem _tasksSystem;

        private void OnTriggerStay2D(Collider2D col)
        {
            var layer = LayerMask.NameToLayer("Chill");
            if (col.gameObject.layer == layer)
            {
                _tasksSystem.EngageChillTask();
            }
        }
    }
}