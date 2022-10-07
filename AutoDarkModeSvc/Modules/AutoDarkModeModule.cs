namespace AutoDarkModeSvc.Modules
{
    public abstract class AutoDarkModeModule : IAutoDarkModeModule
    {
        public string Name { get; }
        public abstract string TimerAffinity { get; }
        public abstract void Fire();
        public int Priority { get; set; }
        public bool FireOnRegistration { get; }
        /// <summary>
        /// Do not call logic in the constructor, as it is called whenever a name check is performed on a module
        /// </summary>
        /// <param name="name"></param>
        /// <param name="fireOnRegistration"></param>
        public AutoDarkModeModule(string name, bool fireOnRegistration)
        {
            Name = name;
            Priority = 0;
            FireOnRegistration = fireOnRegistration;
        }

        public bool Equals(IAutoDarkModeModule other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other is null)
                return false;

            if (other.Name == Name)
            {
                return true;
            }
            return false;
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as IAutoDarkModeModule);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        public int CompareTo(int other)
        {
            return Priority.CompareTo(other);
        }

        public int CompareTo(IAutoDarkModeModule other)
        {
            if (other is null)
            {
                return 0;
            }
            return other.Priority.CompareTo(Priority);
        }

        public virtual void DisableHook()
        {

        }

        public virtual void EnableHook()
        {

        }
    }
}
