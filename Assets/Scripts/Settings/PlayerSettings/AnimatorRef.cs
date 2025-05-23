using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using static InventoryManager;

public class AnimatorRef : MonoBehaviour
{

    //---------- Enums
    public enum PlayerAnimatorParams
    {
        T_DEV_RESET,
        B_Always_Active,
        F_Speed_Modifier,
        F_Move_X,
        F_Move_Y,
        I_Arms_State,
        I_Chosen_State,
        B_Crouched,
        B_Grounded,
        T_Jump,
        T_Push,
        T_P_Zom_Attack,
        T_Cy_Attacked,
        B_Cy_InBody,
        T_L_Convert,
        T_Kill_Player,
        B_Is_Dead
    }

    public enum ParamTypes
    {
        Float,
        Int,
        Trigger,
        Bool
    }

    //---------- Fields
    private Animator _animator;
    public Animator Animator
    {
        get
        {
            if (_animator != null)
            {
                return _animator;
            }

            this.gameObject.TryGetComponent<Animator>(out _animator);
            return _animator;
        }
    }

    private Dictionary<PlayerAnimatorParams, ParamTypes> _playerAnimatorParamsTypes = new()
    {
        { PlayerAnimatorParams.T_DEV_RESET, ParamTypes.Trigger },
        { PlayerAnimatorParams.B_Always_Active, ParamTypes.Bool },
        { PlayerAnimatorParams.F_Speed_Modifier, ParamTypes.Float },
        { PlayerAnimatorParams.F_Move_X, ParamTypes.Float },
        { PlayerAnimatorParams.F_Move_Y, ParamTypes.Float },
        { PlayerAnimatorParams.I_Arms_State, ParamTypes.Int },
        { PlayerAnimatorParams.I_Chosen_State, ParamTypes.Int },
        { PlayerAnimatorParams.B_Crouched, ParamTypes.Bool },
        { PlayerAnimatorParams.B_Grounded, ParamTypes.Bool },
        { PlayerAnimatorParams.T_Jump, ParamTypes.Trigger },
        { PlayerAnimatorParams.T_Push, ParamTypes.Trigger },
        { PlayerAnimatorParams.T_P_Zom_Attack, ParamTypes.Trigger },
        { PlayerAnimatorParams.T_Cy_Attacked, ParamTypes.Trigger },
        { PlayerAnimatorParams.B_Cy_InBody, ParamTypes.Bool },
        { PlayerAnimatorParams.T_L_Convert, ParamTypes.Trigger },
        { PlayerAnimatorParams.T_Kill_Player, ParamTypes.Trigger },
        { PlayerAnimatorParams.B_Is_Dead, ParamTypes.Bool }
    };

    //Get all characters following the first instance of the '_' character
    private readonly Regex rX = new("(?<=_).+");

    //---------- Setter
    public bool SetParam(PlayerAnimatorParams param)
    {
        if (_playerAnimatorParamsTypes[param] is ParamTypes.Trigger)
        {
            string cleanParam = CleanString(param.ToString());
            Animator.SetTrigger(cleanParam);
            return true;
        }

        return false;
    }
    public bool SetParam(PlayerAnimatorParams param, bool value)
    {
        if (_playerAnimatorParamsTypes[param] is ParamTypes.Bool)
        {
            string cleanParam = CleanString(param.ToString());
            Animator.SetBool(cleanParam, value);
            return true;
        }

        return false;
    }
    public bool SetParam(PlayerAnimatorParams param, int value)
    {
        if (_playerAnimatorParamsTypes[param] is ParamTypes.Int)
        {
            string cleanParam = CleanString(param.ToString());
            Animator.SetInteger(cleanParam, value);
            return true;
        }

        return false;
    }
    public bool SetParam(PlayerAnimatorParams param, float value)
    {
        if (_playerAnimatorParamsTypes[param] is ParamTypes.Float)
        {
            string cleanParam = CleanString(param.ToString());
            Animator.SetFloat(cleanParam, value);
            return true;
        }

        return false;
    }

    //---------- Getter
    public object GetParam(PlayerAnimatorParams param) 
    {
        string cleanParam = CleanString(param.ToString());
        object result = null;
        switch (_playerAnimatorParamsTypes[param])
        {
            case ParamTypes.Bool:
                result = Convert.ToBoolean(result);
                result = Animator.GetBool(cleanParam.ToString());
                break;
            case ParamTypes.Int:
                result = Convert.ToInt32(result);
                result = Animator.GetInteger(cleanParam.ToString());
                break;
            case ParamTypes.Float:
                result = Convert.ToSingle(result);
                result = Animator.GetFloat(cleanParam.ToString());
                break;
            default:
                break;
        }

        return result;
    }

    //---------- StringCleaner
    private string CleanString(string stringToClean) => rX.Match(stringToClean).Value;
}
