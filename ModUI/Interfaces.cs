using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.ComponentModel;
using ModUI.Internals;
using System.IO;
using System;

namespace ModUI
{
    [EditorBrowsable(EditorBrowsableState.Never)] public class ModRefuseDisable : Attribute { }

    public interface IModDescription
    {
        string Description { get; }
    }
}
