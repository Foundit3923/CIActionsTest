using UnityEngine;
using MenuEnums;
using System;
using JetBrains.Annotations;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;

public class ButtonParams : MonoBehaviour
{
    //Select what enum the button should pass in
    [SerializeField]
    public MenuActions baseMenuAction;

    public T GetAction<T>()
    {
        //When called pass in the Type to get 
        //The corresponding enum will be returned if there is a match between the chosen enum and the chosen type
        T enumMatch = default(T);
        typeof(T).GetType().GetField(Enum.GetName(typeof(MenuActions), baseMenuAction)).GetValue(enumMatch);
        return enumMatch;
    }
}
