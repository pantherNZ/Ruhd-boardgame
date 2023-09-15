using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(VerticalLayoutGroup))]
public class TextNumberAnimatorUI : MonoBehaviour
{
    [SerializeField] int currentValue;
    int? internalValue = null;
    [SerializeField] float interpSpeed;
    [SerializeField] Utility.EasingFunctionTypes easingFunction;
    [SerializeField] Utility.EasingFunctionMethod easingMethod;
    [SerializeField] int powerColumn = 1;

    private TMPro.TextMeshProUGUI baseNumber;
    private bool inBottomSet;
    private List<TMPro.TextMeshProUGUI> numbers = new List<TMPro.TextMeshProUGUI>();

    void Start()
    {
        if( !numbers.IsEmpty() )
            return;

        baseNumber = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        Debug.Assert( baseNumber.GetComponent<TextNumberAnimatorUI>() == null );
        if( baseNumber.GetComponent<TextNumberAnimatorUI>() != null )
            return;

        while( transform.childCount > 1 )
        {
            var child = transform.GetChild( 1 );
            if( child != baseNumber.transform )
            {
                child.parent = null;
                DestroyImmediate( child.gameObject );
            }
        }

        inBottomSet = Utility.RandomBool();

        for( int i = 0; i <= 20; ++i )
        {
            var newNumber = Instantiate( baseNumber, transform );
            newNumber.name = ( i % 10 ).ToString();
            newNumber.text = ( i % 10 ).ToString();
            if( i < 20 )
                numbers.Add( newNumber );
        }

        baseNumber.text = "9";
        internalValue = null;

        LayoutRebuilder.ForceRebuildLayoutImmediate( transform as RectTransform );
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

        if( numbers.IsEmpty() )
        {
            currentValue = value;
            return;
        }

        value %= Mathf.RoundToInt( Mathf.Pow( 10, powerColumn ) );
        var previousValue = currentValue;
        currentValue = value;
        internalValue = value;
        var newPos = GetNumberPosition();
        if( skipInterpolation )
        {
            transform.localPosition = newPos;
        }
        else
        {
            this.InterpolatePosition( newPos, interpSpeed, true, Utility.FetchEasingFunction( easingFunction, easingMethod ) );
        }

        inBottomSet = !inBottomSet;
    }

    public int GetValue() { return currentValue; }

    private Vector3 GetNumberPosition()
    {
        var target = inBottomSet ? numbers[currentValue + 10] : numbers[currentValue];
        var origin = transform.GetChild( 0 ).localPosition;
        return origin - target.transform.localPosition;
    }
}