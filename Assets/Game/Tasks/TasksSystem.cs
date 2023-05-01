using System;
using System.Collections.Generic;
using Infrastructure;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

namespace Game.Tasks
{
    public class TasksSystem : MonoBehaviour
    {
        [ShowInInspector] private int _currentTask;
        [ShowInInspector] private int _currentTaskList;
        private List<TaskSO> _activeTasks = new List<TaskSO>();
        public List<TaskSO> ActiveTasks => _activeTasks;
        
        [SerializeField] private TaskListSO[] _listOfTasks;

        [Inject] private World _world;

        public event Action<int> OnTaskCompleted;
        public event Action<TaskSO> OnTaskSetUp;
        
        public event Action<List<TaskSO>> OnTasksSetUp;

        private void Awake()
        {
            _currentTaskList = _world.CurrentTaskListID.Value;
            _currentTask = _world.CurrentTaskID.Value;
            SetUpTasksList();
        }

        private void SetUpTasksList()
        {
            SetActiveTasksFromCurrentTasksList();
            OnTasksSetUp?.Invoke(_activeTasks);
        }

        private void SetActiveTasksFromCurrentTasksList()
        {
            _activeTasks.Clear();
            var currentList = GetAllTasksInList();
            for (int i = 0; i < currentList.Length; i++)
            {
                if (_currentTask >= i)
                {
                    _activeTasks.Add(currentList[i]);
                    Debug.Log("taask added");
                }
            }
        }

        private void CompleteTask(int taskID)
        {
            OnTaskCompleted?.Invoke(taskID);
            _currentTask += 1;
            CheckLeftoverTasks();
        }

        private void CheckLeftoverTasks()
        {
            if (_currentTask >= GetAllTasksInList().Length)
            {
                _currentTaskList += 1;
                _currentTask = 0;
            }
        }

        public void CompleteTasksList()
        {
            
        }
        private TaskSO GetCurrentTask()
        {
            return _listOfTasks[_currentTaskList].Tasks[_currentTask];
        }

        private TaskSO[] GetAllTasksInList()
        {
            return _listOfTasks[_currentTaskList].Tasks;
        }

        private void OnDestroy()
        {
            _world.CurrentTaskID.Value = _currentTask;
        }


        #region Chill Task

        private float _timer;
        public void EngageChillTask()
        {
            _timer += Time.deltaTime;

            if (_timer >= ReturnChillTaskTimer())
            {
                CompleteChillTask();
            }
        }

        private float ReturnChillTaskTimer()
        {
            foreach (var task in _activeTasks)
            {
                if (task.TaskType == TaskType.Chill)
                {
                    Debug.Log(_timer);
                    return task.Time;
                }
            }

            Debug.Log("5");
            return 5;
        }
        
        private void CompleteChillTask()
        {
            _timer = 0;
            var id = 0;
            foreach (var task in _activeTasks)
            {
                if (task.TaskType == TaskType.Chill)
                {
                    Debug.Log("task chill complete");
                    CompleteTask(id);
                }
                id += 1;
            }
        }

        #endregion

        public void CompleteFindObjectTask()
        {
            
        }
    }
}