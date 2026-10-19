using System;

namespace AvaUnion.Controls;

/// <summary>The standard buttons of a <see cref="Dialog"/>. Combine them with the | operator.</summary>
[Flags]
public enum DialogButtons
{
    /// <summary>No buttons.</summary>
    None = 0,

    /// <summary>An OK button. It accepts the dialog.</summary>
    Ok = 1 << 0,

    /// <summary>An Open button. It accepts the dialog.</summary>
    Open = 1 << 1,

    /// <summary>A Save button. It accepts the dialog.</summary>
    Save = 1 << 2,

    /// <summary>A Cancel button. It rejects the dialog.</summary>
    Cancel = 1 << 3,

    /// <summary>A Close button. It rejects the dialog.</summary>
    Close = 1 << 4,

    /// <summary>A Discard button. It rejects the dialog.</summary>
    Discard = 1 << 5,

    /// <summary>An Apply button. It does not close the dialog.</summary>
    Apply = 1 << 6,

    /// <summary>A Reset button. It does not close the dialog.</summary>
    Reset = 1 << 7,

    /// <summary>A Restore Defaults button. It does not close the dialog.</summary>
    RestoreDefaults = 1 << 8,

    /// <summary>A Help button. It does not close the dialog.</summary>
    Help = 1 << 9,

    /// <summary>A Save All button. It accepts the dialog.</summary>
    SaveAll = 1 << 10,

    /// <summary>A Yes button. It accepts the dialog.</summary>
    Yes = 1 << 11,

    /// <summary>A Yes to All button. It accepts the dialog.</summary>
    YesToAll = 1 << 12,

    /// <summary>A No button. It rejects the dialog.</summary>
    No = 1 << 13,

    /// <summary>A No to All button. It rejects the dialog.</summary>
    NoToAll = 1 << 14,

    /// <summary>An Abort button. It rejects the dialog.</summary>
    Abort = 1 << 15,

    /// <summary>A Retry button. It accepts the dialog.</summary>
    Retry = 1 << 16,

    /// <summary>An Ignore button. It accepts the dialog.</summary>
    Ignore = 1 << 17,
}
