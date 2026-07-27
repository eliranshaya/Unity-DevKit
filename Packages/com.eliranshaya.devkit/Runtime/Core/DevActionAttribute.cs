using System;

namespace DevKit
{
    /// <summary>
    /// Marks a method as a developer action. The method shows up in the DevKit panel and is
    /// invoked when the user taps its row.
    /// <para>
    /// The path is a <c>/</c> separated string. Everything before the last separator becomes the
    /// category shown in the left rail, the last segment becomes the button label.
    /// </para>
    /// <para>
    /// Static methods are invoked directly. Instance methods must live on a <see cref="UnityEngine.Component"/>
    /// and are resolved against the first live instance in the loaded scenes at invoke time.
    /// </para>
    /// <para>
    /// Parameters become input fields in the panel. Supported types are
    /// <c>int</c>, <c>float</c>, <c>string</c>, <c>bool</c> and any <c>enum</c>.
    /// A method taking anything else is skipped with a warning naming the method and the type.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This attribute is always compiled, so annotated game code keeps compiling when
    /// <c>DEVKIT_ENABLED</c> is undefined. Only the scan that reads it is stripped.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class DevActionAttribute : Attribute
    {
        /// <summary>Full <c>Category/Label</c> path of the action.</summary>
        public string Path { get; private set; }

        /// <summary>When true a yes/no prompt is shown before the method runs.</summary>
        public bool Confirm { get; private set; }

        /// <summary>Sort weight inside the category. Lower values come first, ties keep alphabetical order.</summary>
        public int Order { get; private set; }

        /// <param name="path">Slash separated path, for example <c>"Economy/Add 1000$"</c>.</param>
        /// <param name="confirm">Ask for confirmation first. Use for destructive actions.</param>
        /// <param name="order">Sort weight inside the category.</param>
        public DevActionAttribute(string path, bool confirm = false, int order = 0)
        {
            Path = path;
            Confirm = confirm;
            Order = order;
        }
    }
}
