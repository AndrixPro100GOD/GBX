using UnityEngine;
using Zenject;

namespace Game.Tasks
{
    public class TasksSystemInstaller : MonoInstaller
    {
        [SerializeField] private TasksSystem _tasksSystem;

        public override void InstallBindings()
        {
            Container.Bind<TasksSystem>().FromInstance(_tasksSystem).AsSingle();
        }
    }
}