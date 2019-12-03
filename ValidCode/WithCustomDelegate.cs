namespace ValidCode
{
    using System;

    internal delegate void ValueChangedEventHandler<T>(object sender, ValueChangedEventArgs<T> e);

    internal class ValueChangedEventArgs<T> : EventArgs
    {
        internal ValueChangedEventArgs(T oldValue, T newValue)
        {
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

        internal T OldValue { get; }

        internal T NewValue { get; }
    }
}

