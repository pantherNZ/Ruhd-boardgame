using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class TextNumberAnimatorGroupUI : MonoBehaviour
{
    [SerializeField] int currentValue;
    int? internalValue = null;
    List<TextNumberAnimatorUI> children;

    void Start()
    {
        children = GetComponentsInChildren<TextNumberAnimatorUI>( true ).ToList();
        internalValue = null;
    }

    private void Update()
    {
        if( currentValue != internalValue )
            SetValue( currentValue, internalValue == null );
    }

    public void SetValue( int value, bool skipInterpolation = false )
    {
        if( value == internalValue )
            return;

        if( children == null || children.IsEmpty() )
        {
            currentValue = value;
            return;
        }

        currentValue = value;
        internalValue = value;

        foreach( var (idx, child) in children.Enumerate().Reverse() )
        {
            child.gameObject.SetActive( idx == children.Count - 1 || value > 0 );
            if( child.gameObject.activeSelf )
            {
                var digit = value % 10;
                child.SetValue( digit, skipInterpolation );
            }
            if( value > 0 )
                value /= 10;
        }
    }
}
