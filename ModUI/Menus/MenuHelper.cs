using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ModUI.Internals;

namespace ModUI
{
    public class MenuHelper
    {
        internal static FirstPersonAIO playerController;

        public static void SetInteractMenu(bool enable)
        {
            if (playerController == null)
            {
                var list = Resources.FindObjectsOfTypeAll<FirstPersonAIO>();
                if (list.Length <= 0) return;
                playerController = list[0];
            }

            if (enable)
            {
                playerController.ControllerPause();
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                playerController.ControllerUnPause();
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}