﻿using System;
using System.Threading;
using Piaget_Core.Lib;

using Piaget_Core.System;

namespace Piaget_Core {

    //class TaskBaseNode : DoubleLinkedNode<TaskBaseNode> {
    //    public TaskBase task;
    //}

    interface ITask {
        string Name { get; }
        long Period { get; } // in ps
        void SetState(Action next_state_procedure);
        void SetSleep(long extra_time);
        void AddParallelTask(string name, WithRegularTask task, long period);
        void AddSerialTask(string name, WithRegularTask task, long period);
        void AddLoopTask(string name, WithLoopTask task, long period);
        void SetHibernated();
        void SetRecovered();
        void SetTerminated();
    }

    class Task : DoubleLinkedNode<Task>, ITask {
        private string name;
        private long sw_period;
        private Clock clock;
        private TaskManager task_manager;
        protected Action current_procedure;
        private long wakeup_sw_time;

        public long WakeupSWTime {
            get { return this.wakeup_sw_time; }
        }

        public long Period { get; private set; }

        public void Init(string name, long period, TaskManager task_manager, Clock clock) {
            this.name = name;
            this.Period = period;
            this.sw_period = Clock.ToPiagetTime(period);
            this.clock = clock;
            this.task_manager = task_manager;
            ResetWakeupTime();
        }
        
        public void ResetWakeupTime() {
            this.wakeup_sw_time = clock.ElapsedSWTime;
        }

        public string Name {
            get {
                return this.name;
            }
        }

        public void SetState(Action next_state_procedure) {
            this.current_procedure = next_state_procedure;
        }

        public void Exec() {
            this.current_procedure();
            this.wakeup_sw_time += sw_period;
        }

        public void SetSleep(long time) {
            this.wakeup_sw_time += time - sw_period;
        }

        public void Sleep() {
            long time_to_sleep = (long)(this.wakeup_sw_time - clock.ElapsedSWTime);
            if (time_to_sleep > Config.SleepTimeIncrement) {
                Thread.Sleep(Clock.ToSleepTime(time_to_sleep));
            }
        }

        public void AddParallelTask (string name, WithRegularTask task, long period) {
            this.task_manager.AddParallelTask(name, task, period);
        }

        public void AddSerialTask(string name, WithRegularTask task, long period) {
            this.task_manager.AddSerialTask(name, task, period, this);
        }

        public void AddLoopTask(string name, WithLoopTask task, long period) {
            this.task_manager.AddSerialTask(name, task, period, this);
        }

        public void SetHibernated() {
            task_manager.Hibernate(this);
        }

        public void SetRecovered() {
            this.wakeup_sw_time = this.clock.ElapsedSWTime;
            task_manager.Recover(this);
        }

        public void SetTerminated() {
            task_manager.RemoveFromPool(this);
        }
    }
}
