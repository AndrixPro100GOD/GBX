using UniRx;

namespace Infrastructure
{
    public class World //Save data here
    {
        public ReactiveProperty<int> CurrentTaskID = new ReactiveProperty<int>();
        
        public ReactiveProperty<int> CurrentTaskListID = new ReactiveProperty<int>();
    }
}