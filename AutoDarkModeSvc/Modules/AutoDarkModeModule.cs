#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Modules
{
    public abstract class AutoDarkModeModule : IAutoDarkModeModule
    {
        public string Name { get; }
        public abstract string TimerAffinity { get; }
        public abstract Task Fire(object caller = null);
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
