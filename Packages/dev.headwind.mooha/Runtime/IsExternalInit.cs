using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    // Records with positional parameters compile to init-only setters, which the compiler only
    // emits when this marker type is visible. Unity's target framework does not carry it, and it is
    // internal, so every assembly that declares a record needs its own copy.
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit { }
}
