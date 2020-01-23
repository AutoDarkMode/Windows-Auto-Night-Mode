using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeApp.Handlers
{
    public class SwitchThemeException : Exception
    {
        public override string Message => "Theme switching is unsuccessful.";

        public SwitchThemeException()
        {
            this.Source = "SwitchThemeException";
        }

        public SwitchThemeException(string message, string source) : base(message)
        {
            this.Source = source;
        }
    }

    public class AddAutoStartException : Exception
    {
        public override string Message => "Auto start task could not been set.";

        public AddAutoStartException()
        {
            this.Source = "AutoStartException";
        }

        public AddAutoStartException(string message, string source) : base(message)
        {
            this.Source = source;
        }
    }

    public class RemoveAutoStartException : Exception
    {
        public override string Message => "Auto start task could not been removed.";

        public RemoveAutoStartException()
        {
            this.Source = "RemoveAutoStartException";
        }

        public RemoveAutoStartException(string message, string source) : base(message)
        {
            this.Source = source;
        }
    }
}
